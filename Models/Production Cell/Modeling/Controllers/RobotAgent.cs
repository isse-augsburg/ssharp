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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling.Controllers
{
	using Plants;

	internal class RobotAgent : Agent
	{
		private Capability _currentCapability;

		public RobotAgent(Capability[] capabilities, Robot robot)
			: base(capabilities)
		{
			Robot = robot;
		}

		public Robot Robot { get; }

		public override void TakeResource(Agent agent)
		{
			// If we fail to transfer the resource, the robot loses all of its capabilities; todo: really?
			if (Robot.TakeResource(((CartAgent)agent).Cart))
				return;

			AvailableCapabilites.Clear();
			ObserverController.ScheduleReconfiguration();
		}

		public override void PlaceResource(Agent agent)
		{
			// If we fail to transfer the resource, the robot loses all of its capabilities; todo: really?
			if (Robot.PlaceResource(((CartAgent)agent).Cart))
				return;

			AvailableCapabilites.Clear();
			ObserverController.ScheduleReconfiguration();
		}

		public override void Produce(ProduceCapability capability)
		{
			if (Resource != null || capability.Resources.Count == 0)
				return;

			Resource = capability.Resources[0];
			capability.Resources.RemoveAt(0);
			Robot.ProduceWorkpiece(Resource.Workpiece);
		}

		public override void Process(ProcessCapability capability)
		{
			if (Resource == null)
				return;

			if (_currentCapability != capability)
			{
				// Switch the capability; if we fail to do so, remove all other capabilities from the available ones and
				// trigger a reconfiguration
				if (Robot.SwitchCapability(capability))
					_currentCapability = capability;
				else
				{
					AvailableCapabilites.RemoveAll(c => c != capability);
					ObserverController.ScheduleReconfiguration();
					return;
				}
			}

			// Apply the capability; if we fail to do so, remove it from the available ones and trigger a reconfiguration
			if (!Robot.ApplyCapability())
			{
				AvailableCapabilites.Remove(capability);
				ObserverController.ScheduleReconfiguration();
			}
		}

		public override void Consume(ConsumeCapability capability)
		{
			if (Resource == null)
				return;

			Robot.ConsumeWorkpiece(Resource.Workpiece);
			Resource = null;
		}
	}
}