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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
	using System;
	using System.Threading.Tasks;
	using Odp;
	using Odp.Reconfiguration;

	class TolerableAnalysisController : IController
	{
		private readonly IController _controller;

		private bool _hasReconfed;

		public TolerableAnalysisController(IController controller)
		{
			_controller = controller;
		}

		public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated
		{
			add { _controller.ConfigurationsCalculated += value; }
			remove { _controller.ConfigurationsCalculated -= value; }
		}

		public BaseAgent[] Agents => _controller.Agents;
		public Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task)
		{
			if (_hasReconfed)
			{
				// This speeds up analyses when checking for reconf failures with DCCA, but is otherwise
				// unacceptable for other kinds of analyses
				return null;
			}

			_hasReconfed = true;
			return _controller.CalculateConfigurations(context, task);
		}
	}
}
