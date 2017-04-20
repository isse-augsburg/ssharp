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
        [NonDiscoverable, Hidden(HideElements = true)]
        public Dictionary<uint, List<Tuple<TimeSpan,TimeSpan>>> CollectedTimeValues { get; } = new Dictionary<uint, List<Tuple<TimeSpan, TimeSpan>>>();

        [NonDiscoverable, Hidden]
        private readonly Stopwatch _stopwatch;

        public PerformanceMeasurementController(IEnumerable<Agent> agents)
        {
            this._actingController = (IController)Activator.CreateInstance(typeof(T), agents.Cast<BaseAgent>());
            this.Agents = this._actingController.Agents;
            _stopwatch = Stopwatch.StartNew();
            foreach (var agent in Agents)
            {
                CollectedTimeValues.Add(agent.ID, new List<Tuple<TimeSpan, TimeSpan>>());
            }
        }

        public Task<ConfigurationUpdate> CalculateConfigurations(object context, params ITask[] tasks)
        {
            _stopwatch.Stop();
            Console.WriteLine(_stopwatch.ElapsedMilliseconds);
            MicrostepScheduler.StartPerformanceMeasurement(_actingController);
            var resultingTasks = this._actingController.CalculateConfigurations(context, tasks);
            var reconfTime = MicrostepScheduler.StopPerformanceMeasurement(_actingController).Elapsed;
            _stopwatch.Restart();
            
            foreach (var agent in Agents)
            {
                CollectedTimeValues[agent.ID].Add(new Tuple<TimeSpan, TimeSpan>(_stopwatch.Elapsed, reconfTime));
                
            }
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
