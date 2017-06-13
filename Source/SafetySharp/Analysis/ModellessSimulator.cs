using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Analysis
{
    using Modeling;

    public class ModellessSimulator
    {

        /// <summary>
        ///   Gets the root components of the model.
        /// </summary>
        public IComponent[] RootComponents { get; }

        public ModellessSimulator(IComponent[] rootComponents)
        {
            RootComponents = rootComponents;
        }

        public void SimulateStep()
        {
            foreach (var component in RootComponents)
                component.Update();

            MicrostepScheduler.CompleteSchedule();
        }

    }
}
