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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using Modeling;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Simulates a S# model for debugging or testing purposes.
	/// </summary>
	public sealed unsafe class SafetySharpSimulator : Simulator
	{
		internal SafetySharpCounterExample SafetySharpCounterExample { get; }
		internal override CounterExample CounterExample => SafetySharpCounterExample;


		internal SafetySharpRuntimeModel SafetySharpRuntimeModel { get; }
		internal override ExecutableModel RuntimeModel => SafetySharpRuntimeModel;

		private ChoiceResolver _choiceResolver;
		internal override ChoiceResolver ChoiceResolver => _choiceResolver;

		private readonly List<byte[]> _states = new List<byte[]>();
		private int _stateIndex;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be simulated.</param>
		/// <param name="formulas">The formulas that can be evaluated on the model.</param>
		public SafetySharpSimulator(ModelBase model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			SafetySharpRuntimeModel = SafetySharpRuntimeModel.Create(model, formulas);
			Reset();
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="counterExample">The counter example that should be simulated.</param>
		public SafetySharpSimulator(SafetySharpCounterExample counterExample)
		{
			Requires.NotNull(counterExample, nameof(counterExample));

			SafetySharpCounterExample = counterExample;
			SafetySharpRuntimeModel = SafetySharpCounterExample.SafetySharpRuntimeModel;

			Reset();
		}

		/// <summary>
		///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
		/// </summary>
		public ModelBase Model => SafetySharpRuntimeModel.Model;


		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		public override void Reset()
		{
			if (ChoiceResolver == null)
			{
				_choiceResolver = new NondeterministicChoiceResolver();
				throw new Exception();
				//foreach (var choice in _runtimeModel.Objects.OfType<Choice>())
				//	choice.Resolver = choiceResolver;
			}



			var state = stackalloc byte[RuntimeModel.StateVectorSize];

			_states.Clear();
			_stateIndex = -1;

			if (CounterExample == null)
				RuntimeModel.Reset();
			else
				CounterExample.Replay(ChoiceResolver, 0);

			RuntimeModel.Serialize(state);
			AddState(state);
		}


		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			CounterExample.SafeDispose();
			ChoiceResolver.SafeDispose();
		}
	}
}