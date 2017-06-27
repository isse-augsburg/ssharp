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
        private readonly Stopwatch _stopwatch;

        public PerformanceMeasurementController(IEnumerable<Agent> agents)
        {
            this._actingController = (IController)Activator.CreateInstance(typeof(T), agents.Cast<BaseAgent>());
            this.Agents = this._actingController.Agents;
            _stopwatch = Stopwatch.StartNew();
            foreach (var agent in Agents)
            {
                CollectedTimeValues.Add(agent.ID, new List<Tuple<TimeSpan, TimeSpan, long>>());
            }
        }

        public Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task)
        {
            _stopwatch.Stop();
            MicrostepScheduler.StartPerformanceMeasurement(_actingController);
            var resultingTasks = this._actingController.CalculateConfigurations(context, task);
            var reconfTime = MicrostepScheduler.StopPerformanceMeasurement(_actingController);
            using (var sw = new StreamWriter(@"C:\Users\Eberhardinger\Documents\test.csv", true))
            {
                sw.WriteLine(_stopwatch.ElapsedMilliseconds.ToString() + "; " + reconfTime.ElapsedMilliseconds.ToString());
            }
            foreach (var agent in Agents)
            {
                CollectedTimeValues[agent.ID].Add(new Tuple<TimeSpan, TimeSpan, long>(_stopwatch.Elapsed, reconfTime.Elapsed, DateTime.Now.Ticks));

            }
            _stopwatch.Restart();
            return resultingTasks;
            

        }

        public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated
        {
            add { _actingController.ConfigurationsCalculated += value; }
            remove { _actingController.ConfigurationsCalculated -= value; }
        }
    }
}
