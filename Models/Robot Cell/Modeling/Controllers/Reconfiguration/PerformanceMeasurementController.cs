using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
    using System.Diagnostics;
    using System.IO;
    using Odp;
    using Odp.Reconfiguration;
    using SafetySharp.Modeling;

    internal interface IPerformanceMeasurementController
    {
        Dictionary<uint, List<Tuple<TimeSpan, TimeSpan, long>>> CollectedTimeValues { get; }
    }

    class PerformanceMeasurementController<T> : IController, IPerformanceMeasurementController
        where T : IController
    {
        [NonDiscoverable, Hidden(HideElements = true)]
        public BaseAgent[] Agents { get; }
        [NonDiscoverable, Hidden]
        private readonly IController _actingController;
        [NonDiscoverable, Hidden(HideElements = true)]
        public Dictionary<uint, List<Tuple<TimeSpan,TimeSpan, long>>> CollectedTimeValues { get; } = new Dictionary<uint, List<Tuple<TimeSpan, TimeSpan, long>>>();

        [NonDiscoverable, Hidden]
        private readonly Dictionary<BaseAgent, Stopwatch> _stopwatchs = new Dictionary<BaseAgent, Stopwatch>();
        
        public PerformanceMeasurementController(IEnumerable<Agent> agents)
        {
            this._actingController = (IController)Activator.CreateInstance(typeof(T), agents.Cast<BaseAgent>());
            this.Agents = this._actingController.Agents;
            foreach (var agent in Agents)
            {
                CollectedTimeValues.Add(agent.ID, new List<Tuple<TimeSpan, TimeSpan, long>>());
                _stopwatchs.Add(agent, Stopwatch.StartNew());
            }
        }

        public async Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task)
        {
            MicrostepScheduler.StartPerformanceMeasurement(_actingController);
            var resultingTasks = await this._actingController.CalculateConfigurations(context, task);
            var reconfTime = MicrostepScheduler.StopPerformanceMeasurement(_actingController);
            foreach (var agent in resultingTasks.InvolvedAgents)
            {
                _stopwatchs[agent].Stop();
                CollectedTimeValues[agent.ID].Add(new Tuple<TimeSpan, TimeSpan, long>(_stopwatchs[agent].Elapsed-reconfTime.Elapsed, reconfTime.Elapsed, DateTime.Now.Ticks));
                _stopwatchs[agent].Restart();
            }
            return resultingTasks;
            

        }

        public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated
        {
            add { _actingController.ConfigurationsCalculated += value; }
            remove { _actingController.ConfigurationsCalculated -= value; }
        }
    }
}
