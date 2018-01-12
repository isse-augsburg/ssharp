// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

using System;
using System.Collections.Generic;
using System.Linq;
using ISSE.SafetyChecking.Modeling;

namespace SafetyLustre
{
	public class LustreModelBase
	{
		public LustreModelBase(string ocFileName, IEnumerable<Fault> faults)
		{
            program = new Program(ocFileName, null);
			var numberOfInputQueuesNeeded =
				program.signals.Count(signal => signal.IsFaultInput || signal.IsRealInput);
		    program.input = new Queue<Object>[numberOfInputQueuesNeeded];
			for (var i = 0; i < numberOfInputQueuesNeeded; i++)
			{
				program.input[i] = new Queue<Object>();
			}

            StateVectorSize =
		        program.countVariables(0) * sizeof(bool) +
		        program.countVariables(1) * sizeof(int) +
		        program.countVariables(2) * sizeof(char) +
		        program.countVariables(3) * sizeof(float) +
		        program.countVariables(4) * sizeof(double) +
				sizeof(int) + // variable containing the current state
				sizeof(long); // variable containing the permanent faults

		    output = 0;

			this.faults = faults.ToDictionary( fault => fault.Name, fault => fault);
		}

        public Program program;

		public Dictionary<string, Fault> faults;

		public int StateVectorSize { get; }

		public Object output;

		public Choice Choice { get; set; } = new Choice();
		
		public virtual void SetInitialState()
		{
			output = 0;
		}

		public void Update()
		{
			for (var i = 0; i < program.signals.Count; i++)
			{
				var signal = program.signals[i];
				if (signal.IsRealInput)
				{
					// TODO: Different types
					var value=Choice.Choose(true, false);
					program.input[i].Enqueue(value);
				}
				if (signal.IsFaultInput)
				{
					var activated = ISSE.SafetyChecking.ExecutedModel.FaultHelper.Activate(faults[signal.getName()]);
					program.input[i].Enqueue(activated);
				}
			}
			
            program.executeProcedure();
		    output = program.output[0];
        }
	}
}
