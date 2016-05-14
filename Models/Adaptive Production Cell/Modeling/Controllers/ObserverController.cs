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
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;

	internal abstract class ObserverController : Component
	{
		[Hidden]
		private bool _reconfigurationRequested = true;

		protected ObserverController(IEnumerable<Agent> agents)
		{
			Agents = agents.ToArray();

			foreach (var agent in Agents)
				Bind(nameof(agent.TriggerReconfiguration), nameof(OnReconfigurationRequested));
		}

		protected ObjectPool<Role> RolePool { get; } = new ObjectPool<Role>(Model.MaxRoleCount);
		protected List<Task> Tasks { get; } = new List<Task>(Model.MaxTaskCount);

		[Hidden(HideElements = true)]
		protected Agent[] Agents { get; }

		public bool ReconfigurationFailed { get; protected set; }

		protected abstract void Reconfigure();

		private void OnReconfigurationRequested()
		{
			_reconfigurationRequested = true;
		}

		public override void Update()
		{
			if (!_reconfigurationRequested)
				return;

			Reconfigure();
			_reconfigurationRequested = false;
		}
	}
}