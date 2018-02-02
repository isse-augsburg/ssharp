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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration.PerformanceMeasurement
{
    using System;
	using System.Diagnostics;
	using System.Threading.Tasks;

    using Odp;
    using Odp.Reconfiguration;
    using SafetySharp.Modeling;

    public class PerformanceMeasurementController : IController
    {
        private readonly IController _actingController;
        
        public PerformanceMeasurementController(IController actingController)
        {
            _actingController = actingController;
        }

		public event Action<ITask, ConfigurationUpdate, ulong> MeasuredConfigurationCalculation;

        public async Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task)
        {
	        var tuple = await AsyncPerformance.Measure(() => _actingController.CalculateConfigurationsAsync(context, task));
	        var configUpdate = tuple.Item1;
	        var reconfTime = tuple.Item2;

			MeasuredConfigurationCalculation?.Invoke(task, configUpdate, ElapsedNanoseconds(reconfTime));
            return configUpdate;
        }

        public BaseAgent[] Agents => _actingController.Agents;

        public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated
        {
            add { _actingController.ConfigurationsCalculated += value; }
            remove { _actingController.ConfigurationsCalculated -= value; }
        }

		public static ulong ElapsedNanoseconds(Stopwatch watch)
		{
			const ulong nanosecondsPerSecond = 1000000000ul;

			// lossless since non-negative
			var ticks = (ulong)watch.ElapsedTicks;
			var frequency = (ulong)Stopwatch.Frequency;

			return (ticks * nanosecondsPerSecond) / frequency;
		}
    }
}
