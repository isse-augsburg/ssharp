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
	using System;
	using System.Collections.Generic;
	using SafetySharp.Modeling;
	using Odp;
	using Odp.Reconfiguration;

    internal abstract class Agent : BaseAgent
	{
		public readonly Fault ConfigurationUpdateFailed = new TransientFault();

	    protected Agent()
	    {
	        ConfigurationUpdateFailed.Name = $"{Name}.{nameof(ConfigurationUpdateFailed)}";
	    }

	    internal Queue<Task> TaskQueue;

		public abstract string Name { get; }

	    public abstract System.Threading.Tasks.Task RestoreRobot(Fault fault);


        public bool HasResource => Resource != null;

		protected override async System.Threading.Tasks.Task UpdateAsync()
		{
			CheckAllocatedCapabilities();
			if (TaskQueue?.Count > 0)
				await PerformReconfiguration(new[] {
					Tuple.Create(TaskQueue.Dequeue() as ITask, new State(this))
				});
			await base.UpdateAsync();
		}

		protected override void DropResource()
		{
			(Resource.Task as Task).IsResourceInProduction = false;
			base.DropResource();
		}

		public void CheckAllocatedCapabilities()
		{
			foreach (var input in Inputs.ToArray())
			{
				if (!CheckInput((Agent)input))
					input.Disconnect(this);
			}

			foreach (var output in Outputs.ToArray())
			{
				if (!CheckOutput((Agent)output))
					Disconnect(output);
			}
		}

		protected virtual bool CheckAllocatedCapability(ICapability capability)
		{
			return true;
		}

		protected virtual bool CheckInput(Agent agent)
		{
			return true;
		}

		protected virtual bool CheckOutput(Agent agent)
		{
			return true;
		}

		[FaultEffect(Fault = nameof(ConfigurationUpdateFailed))]
		public abstract class ConfigurationUpdateFailedEffect : Agent
		{
			public override void AllocateRoles(IEnumerable<Role> roles) { }
			public override void RemoveAllocatedRoles(IEnumerable<Role> roles) { }
		}
	}
}