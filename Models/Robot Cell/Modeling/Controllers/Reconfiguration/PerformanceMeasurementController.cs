using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
    using System.Diagnostics;
    using Odp;
    using Odp.Reconfiguration;
    using SafetySharp.Modeling;

    class PerformanceMeasurementController<T> : IController
        where T : IController
    {
        [NonDiscoverable, Hidden(HideElements = true)]
        public BaseAgent[] Agents { get; }
        [NonDiscoverable, Hidden]
        private readonly IController _actingController;
        [NonDiscoverable, Hidden]
        private readonly Stopwatch _stopwatchReconf = new Stopwatch();
        [NonDiscoverable, Hidden(HideElements = true)]
        public List<Tuple<TimeSpan,TimeSpan>> CollectedTimeValues { get; } = new List<Tuple<TimeSpan, TimeSpan>>();
        [NonDiscoverable, Hidden]
        private readonly  Stopwatch _stopwatchLastReconf = new Stopwatch();

        public PerformanceMeasurementController(IEnumerable<Agent> agents)
        {
            this._actingController = (IController)Activator.CreateInstance(typeof(T), agents);
            this.Agents = this._actingController.Agents;
            _stopwatchLastReconf.Start();
        }

        public Task<ConfigurationUpdate> CalculateConfigurations(object context, params ITask[] tasks)
        {
            _stopwatchLastReconf.Stop();
            _stopwatchReconf.Start();
            var resultingTasks = this._actingController.CalculateConfigurations(context, tasks);
            _stopwatchReconf.Stop(); 
            CollectedTimeValues.Add(new Tuple<TimeSpan, TimeSpan>(_stopwatchReconf.Elapsed, _stopwatchLastReconf.Elapsed));
            _stopwatchLastReconf.Start();
            return resultingTasks;
        }

        public bool ReconfigurationFailure => _actingController.ReconfigurationFailure;
        public event Action<BaseAgent[]> ConfigurationsCalculated
        {
            add { _actingController.ConfigurationsCalculated += value; }
            remove { _actingController.ConfigurationsCalculated -= value; }
        }
    }
}
