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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using SafetySharp.Modeling;
	using Odp;
	using System.Collections.Generic;

	class CentralRobotReconfiguration : CentralReconfiguration<Agent, Task, Resource>
	{
		public readonly Fault ReconfigurationFailure = new TransientFault();

		public bool UnsuccessfulReconfiguration { get; private set; }
		public const int MaxSteps = 350;

		[Range(0, MaxSteps, OverflowBehavior.Clamp)]
		private int _stepCount;
		private bool _hasReconfed;
		[Hidden]
		public AnalysisMode Mode = AnalysisMode.AllFaults;

		public CentralRobotReconfiguration(Controller controller) : base(controller) { }

		public override void Update()
		{
			++_stepCount;
			base.Update();
		}

		public override void Reconfigure(IEnumerable<Task> deficientTasks)
		{
			if (Mode == AnalysisMode.IntolerableFaults && _stepCount >= MaxSteps)
				return;

			if (UnsuccessfulReconfiguration)
				return;

			if (Mode == AnalysisMode.TolerableFaults && _hasReconfed)
			{
				// This speeds up analyses when checking for reconf failures with DCCA, but is otherwise
				// unacceptable for other kinds of analyses
				return;
			}

			base.Reconfigure(deficientTasks);
			UnsuccessfulReconfiguration = _controller.ReconfigurationFailure;
			_hasReconfed = true;
		}

		[FaultEffect(Fault = nameof(ReconfigurationFailure))]
		public abstract class ReconfigurationFailureEffect : CentralRobotReconfiguration
		{
			public ReconfigurationFailureEffect(Controller controller) : base(controller) { }

			public override void Reconfigure(IEnumerable<Task> deficientTasks)
			{
				UnsuccessfulReconfiguration = true;
			}
		}
	}
}
