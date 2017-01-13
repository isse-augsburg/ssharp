// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.Runtime
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Runtime.CompilerServices;
	using System.Runtime.Serialization.Formatters.Binary;
	using Analysis;
	using CompilerServices;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	public sealed unsafe class SafetySharpRuntimeModel : ExecutableModel<SafetySharpRuntimeModel>
	{
		/// <summary>
		///   The objects referenced by the model that participate in state serialization.
		/// </summary>
		private readonly ObjectTable _serializedObjects;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="serializedData">The serialized data describing the model.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		internal SafetySharpRuntimeModel(SerializedRuntimeModel serializedData, int stateHeaderBytes = 0) : base(stateHeaderBytes)
		{
			Requires.That(serializedData.Model != null, "Expected a valid model instance.");

			var buffer = serializedData.Buffer;
			var rootComponents = serializedData.Model.Roots;
			var objectTable = serializedData.ObjectTable;
			var formulas = serializedData.Formulas;

			Requires.NotNull(buffer, nameof(buffer));
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");

			Model = serializedData.Model;
			SerializedModel = buffer;
			RootComponents = rootComponents.Cast<Component>().ToArray();
			ExecutableStateFormulas = objectTable.OfType<ExecutableStateFormula>().ToArray();
			Formulas = formulas;
			StateConstraints = Model.Components.Cast<Component>().SelectMany(component => component.StateConstraints).ToArray();

			// Create a local object table just for the objects referenced by the model; only these objects
			// have to be serialized and deserialized. The local object table does not contain, for instance,
			// the closure types of the state formulas.
			Faults = objectTable.OfType<Fault>().Where(fault => fault.IsUsed).ToArray();
			_serializedObjects = new ObjectTable(Model.ReferencedObjects);

			Objects = objectTable;
			StateVectorLayout = SerializationRegistry.Default.GetStateVectorLayout(Model, _serializedObjects, SerializationMode.Optimized);
			UpdateFaultSets();

			_deserialize = StateVectorLayout.CreateDeserializer(_serializedObjects);
			_serialize = StateVectorLayout.CreateSerializer(_serializedObjects);
			_restrictRanges = StateVectorLayout.CreateRangeRestrictor(_serializedObjects);

			PortBinding.BindAll(objectTable);
			
			InitializeConstructionState();
			CheckConsistencyAfterInitialization();
		}


		/// <summary>
		///   Initializes a new instance, which encapsulates ModelBase. It does not work on its own. The resulting
		///   class only provides a working DeriveExecutableModelGenerator. Other methods should not be executed!
		/// </summary>
		private SafetySharpRuntimeModel(ModelBase model)
			: base(0)
		{
			Model = model;
		}

		/// <summary>
		///   Gets a copy of the original model the runtime model was generated from.
		/// </summary>
		internal ModelBase Model { get; }

		/// <summary>
		///   Gets all of the objects referenced by the model, including those that do not take part in state serialization.
		/// </summary>
		internal ObjectTable Objects { get; }

		/// <summary>
		///   Gets the model's <see cref="StateVectorLayout" />.
		/// </summary>
		internal StateVectorLayout StateVectorLayout { get; }

		/// <summary>
		///   Gets the size of the state vector in bytes. The size is always a multiple of 4.
		/// </summary>
		public override int StateVectorSize => StateVectorLayout.SizeInBytes + StateHeaderBytes;

		/// <summary>
		///   Gets the root components of the model.
		/// </summary>
		public Component[] RootComponents { get; }

		/// <summary>
		///   Gets the state formulas of the model.
		/// </summary>
		internal ExecutableStateFormula[] ExecutableStateFormulas { get; }


		/// <summary>
		///   Gets the state formulas of the model.
		/// </summary>
		public override AtomarPropositionFormula[] AtomarPropositionFormulas => ExecutableStateFormulas;

		/// <summary>
		///   Computes an initial state of the model.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void ExecuteInitialStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			foreach (var obj in _serializedObjects.OfType<IInitializable>())
				obj.Initialize();

			_restrictRanges();
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void ExecuteStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			foreach (var component in RootComponents)
				component.Update();

			_restrictRanges();
		}
		

		/// <summary>
		///   Generates the replay information for the <paramref name="trace" />.
		/// </summary>
		/// <param name="choiceResolver">The choice resolver that should be used to resolve nondeterministic choices.</param>
		/// <param name="trace">The trace the replay information should be generated for.</param>
		/// <param name="endsWithException">Indicates whether the trace ends with an exception being thrown.</param>
		private int[][] GenerateReplayInformation(ChoiceResolver choiceResolver, byte[][] trace, bool endsWithException)
		{
			var info = new int[trace.Length - 1][];
			var targetState = stackalloc byte[StateVectorSize];

			// We have to generate the replay info for all transitions
			for (var i = 0; i < trace.Length - 1; ++i)
			{
				choiceResolver.Clear();
				choiceResolver.PrepareNextState();

				// Try all transitions until we find the one that leads to the desired state
				while (true)
				{
					try
					{
						if (!choiceResolver.PrepareNextPath())
							break;

						fixed (byte* sourceState = trace[i])
						Deserialize(sourceState);

						if (i == 0)
							ExecuteInitialStep();
						else
							ExecuteStep();

						if (endsWithException && i == trace.Length - 2)
							continue;
					}
					catch (Exception)
					{
						Requires.That(endsWithException, "Unexpected exception.");
						Requires.That(i == trace.Length - 2, "Unexpected exception.");

						info[i] = choiceResolver.GetChoices().ToArray();
						break;
					}

					NotifyFaultActivations();
					Serialize(targetState);

					// Compare the target states; if they match, we've found the correct transition
					var areEqual = true;
					for (var j = StateHeaderBytes; j < StateVectorSize; ++j)
						areEqual &= targetState[j] == trace[i + 1][j];

					if (!areEqual)
						continue;

					info[i] = choiceResolver.GetChoices().ToArray();
					break;
				}

				Requires.That(info[i] != null, $"Unable to generate replay information for step {i + 1} of {trace.Length}.");
			}

			return info;
		}

		/// <summary>
		///   Notifies all activated faults of their activation. Returns <c>false</c> to indicate that no notifications were necessary.
		/// </summary>
		internal bool NotifyFaultActivations()
		{
			if (ActivationSensitiveFaults.Length == 0)
				return false;

			var notificationsSent = false;
			foreach (var fault in ActivationSensitiveFaults)
			{
				if (fault.IsActivated)
				{
					fault.OnActivated();
					notificationsSent = true;
				}
			}

			return notificationsSent;
		}

		
		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				Objects.OfType<IDisposable>().SafeDisposeAll();
		}

		/// <summary>
		///   Creates a <see cref="SafetySharpRuntimeModel" /> instance from the <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model the runtime model should be created for.</param>
		/// <param name="formulas">The formulas the model should be able to check.</param>
		internal static SafetySharpRuntimeModel Create(ModelBase model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, formulas);
			return serializer.Load();
		}

		/// <summary>
		///   Initializes a new instance, which encapsulates ModelBase. It does not work on its own. The resulting
		///   class only provides a working DeriveExecutableModelGenerator. Other methods should not be executed!
		/// </summary>
		internal static SafetySharpRuntimeModel CreateEncapsulateModelBase(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));

			return new SafetySharpRuntimeModel(model);
		}

		internal override void SetChoiceResolver(ChoiceResolver choiceResolver)
		{
			foreach (var choice in Objects.OfType<Choice>())
				choice.Resolver = choiceResolver;
		}

		public override CounterExampleSerialization<SafetySharpRuntimeModel> CounterExampleSerialization { get; } = new SafetySharpCounterExampleSerialization();
		

		public static Func<SafetySharpRuntimeModel> CreateExecutedModelCreator(ModelBase model, params Formula[] formulas)
		{
			// Note: If a model created by the returned delegate is used, old "stateFormulas" cannot be used anymore, because they
			// might have parts of the "old" model (the one for which formulas have been instantiated in) in their closure.
			// Use "model.Formulas" instead! The order should be preserved.
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			return CreateExecutedModelFromFormulasCreator(model)(formulas);
		}

		public static Func<Formula[], Func<SafetySharpRuntimeModel>> CreateExecutedModelFromFormulasCreator(ModelBase model)
		{
			// Note: If a model created by the returned delegate is used, old "stateFormulas" cannot be used anymore, because they
			// might have parts of the "old" model (the one for which formulas have been instantiated in) in their closure.
			// Use "model.Formulas" instead! The order should be preserved.
			Requires.NotNull(model, nameof(model));

			var serializer = new RuntimeModelSerializer();
			var alreadyCreated = false;

			Func<Formula[], Func<SafetySharpRuntimeModel>> loader = formulas =>
			{
				Requires.NotNull(formulas, nameof(formulas));
				Requires.That(!alreadyCreated, $"{nameof(CreateExecutedModelFromFormulasCreator)} may only be called once, because the first run of serializer binds the model");
				alreadyCreated = true;
				serializer.Serialize(model, formulas); //has a side effect: Model binding!
				return serializer.Load;
			};
			return loader;
		}

		public override Expression CreateExecutableExpressionFromAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			var executableStateFormula = formula as ExecutableStateFormula;
			if (executableStateFormula!=null)
			{
				return Expression.Invoke(Expression.Constant(executableStateFormula.Expression));
			}
			throw new InvalidOperationException("AtomarPropositionFormula cannot be evaluated. Use ExecutableStateFormula instead.");
		}
	}
}