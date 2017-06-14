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

namespace SafetySharp.Odp.Reconfiguration
{
	using System.Collections.Generic;

	public class ReconfigurationMonitor
	{
		private readonly List<ITask> _failedTasks;

		public ReconfigurationMonitor(int maxTaskCount)
		{
			_failedTasks = new List<ITask>(maxTaskCount);
		}

		private IController _controller;

		public IController Controller
		{
			get { return _controller; }
			set
			{
				if (_controller != null)
					_controller.ConfigurationsCalculated -= OnReconfiguration;
				_controller = value;
				if (_controller != null)
					_controller.ConfigurationsCalculated += OnReconfiguration;
			}
		}

		public bool ReconfigurationFailure { get; private set; }

		private void OnReconfiguration(ITask task, ConfigurationUpdate config)
		{
			ReconfigurationFailure |= config.Failed;

			if (config.Failed && !_failedTasks.Contains(task))
				_failedTasks.Add(task);
		}
	}
}
