// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System.Reflection;
	using System.Reflection.Emit;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Dynamically generates the step execution method.
	/// </summary>
	public sealed class StepGenerator
	{
		/// <summary>
		///   The IL generator of the serialization method.
		/// </summary>
		private readonly ILGenerator _il;

		/// <summary>
		///   The method that is being generated.
		/// </summary>
		private readonly DynamicMethod _method;

		/// <summary>
		///   The model the method is generated for.
		/// </summary>
		private readonly Model _model;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the method should be generated for.</param>
		internal StepGenerator(Model model)
		{
			Requires.NotNull(model, nameof(model));

			_method = new DynamicMethod(
				name: "ExecuteStep",
				returnType: typeof(void),
				parameterTypes: new[] { typeof(object[]) },
				m: typeof(object).Assembly.ManifestModule,
				skipVisibility: true);

			_il = _method.GetILGenerator();
			_model = model;
		}

		/// <summary>
		///   Compiles the dynamic method, returning a delegate that can be used to invoke it.
		/// </summary>
		internal Action<object[]> Compile()
		{
			// The method must end with a ret instruction, even though no value is returned
			//GenerateUpdates();
			_il.Emit(OpCodes.Ret);

			return (Action<object[]>)_method.CreateDelegate(typeof(Action<object[]>));
		}

//		/// <summary>
//		///   Generates the IL code that invokes the object updates.
//		/// </summary>
//		private void GenerateUpdates()
//		{
//			_model.Metadata.RootComponent.VisitPostOrder(component =>
//			{
//				foreach (var fault in component.Faults)
//					UpdateFault(fault.Fault);
//
//				UpdateComponent(component.Component);
//			});
//		}
//
//		/// <summary>
//		///   Generates the code to update the <paramref name="component" />, if necessary.
//		/// </summary>
//		/// <param name="component">The component that should be updated.</param>
//		private void UpdateComponent(Component component)
//		{
//			if (component == null)
//				return;
//
//			var metadata = component.Metadata;
//
//			// If the component does not override the update function and has no state machine,
//			// there's no need to update it
//			if (metadata.UpdateMethods.Length != 1 || metadata.StateMachine != null)
//				InvokeDelegate(_model.ObjectTable[component], metadata.StepMethod.BackingField);
//		}
//
//		/// <summary>
//		///   Generates the code to update the <paramref name="fault" />, if necessary.
//		/// </summary>
//		/// <param name="fault">The fault that should be updated.</param>
//		private void UpdateFault(Fault fault)
//		{
//			if (fault == null)
//				return;
//
//			// If the fault does not override the update function or it is ignored, there's no need to update it
//			if (fault.Metadata.UpdateMethods.Length != 1 && !fault.IsIgnored)
//				CallMethod(_model.ObjectTable[fault], fault.Metadata.StepMethod.MethodInfo);
//
//			UpdateOccurrencePattern(fault.OccurrencePattern);
//		}
//
//		/// <summary>
//		///   Generates the code to update the <paramref name="occurrencePattern" />, if necessary.
//		/// </summary>
//		/// <param name="occurrencePattern">The occurrence pattern that should be updated.</param>
//		private void UpdateOccurrencePattern(OccurrencePattern occurrencePattern)
//		{
//			if (occurrencePattern == null)
//				return;
//
//			// If the associated fault is ignored, there is no need to update the occurrence pattern
//			if (!occurrencePattern.Metadata.DeclaringFault.Fault.IsIgnored)
//				CallMethod(_model.ObjectTable[occurrencePattern], occurrencePattern.Metadata.StepMethod.MethodInfo);
//		}

		/// <summary>
		///   Invokes the delegate for the object identified by <paramref name="objectIdentifier" /> stored in the object's
		///   <paramref name="delegateField" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object the delegate should be invoked for.</param>
		/// <param name="delegateField">The field storing the delegate that should be invoked.</param>
		private void InvokeDelegate(int objectIdentifier, FieldInfo delegateField)
		{
			LoadObject(objectIdentifier);

			// o._updateDelegate()
			_il.Emit(OpCodes.Ldfld, delegateField);
			_il.Emit(OpCodes.Call, delegateField.FieldType.GetMethod("Invoke"));
		}

		/// <summary>
		///   Invokes the <paramref name="method" /> for the object identified by <paramref name="objectIdentifier" />.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object the delegate should be invoked for.</param>
		/// <param name="method">The method that should be invoked.</param>
		private void CallMethod(int objectIdentifier, MethodInfo method)
		{
			LoadObject(objectIdentifier);

			// o.method()
			_il.Emit(OpCodes.Call, method);
		}

		/// <summary>
		///   Loads the object identified by <paramref name="objectIdentifier" /> onto the stack.
		/// </summary>
		/// <param name="objectIdentifier">The identifier of the object that should be loaded onto the stack.</param>
		private void LoadObject(int objectIdentifier)
		{
			// var o = objects[identifier]
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Ldc_I4, objectIdentifier);
			_il.Emit(OpCodes.Ldelem_Ref);
		}
	}
}