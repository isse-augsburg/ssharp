using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Plants
{
    using System.Diagnostics;
    using System.IO;

    [AttributeUsage(AttributeTargets.Field)]
    class ReliabilityAttribute : Attribute
    {
        /// <summary>
        /// Mean time to failure in simulation steps
        /// </summary>
        public double MTTF { get; set; }

        /// <summary>
        /// Mean time to repair in simulation steps
        /// </summary>
        public double MTTR { get; set; }

        private int expDistributionXForRepair = 1;
        private int expDistributionXForFail = 1;
        private double previousPropabilityToSucced;
        private double previousPropabilityToNotRepair;


        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ReliabilityAttribute(double mttf, double mttr)
        {
            if (mttf <= 0 || mttr <= 0)
                throw new ArgumentOutOfRangeException();
            this.MTTF = mttf;
            this.MTTR = mttr;
            previousPropabilityToSucced = Math.Exp(-1 * (0.0 / MTTF));
            previousPropabilityToNotRepair = Math.Exp(-1 * (0.0 / MTTR));
        }

        public double DistributionValueToFail()
        {
            var currentPropabilityToSucced = Math.Exp(-1 * (expDistributionXForFail++ / MTTF)) / previousPropabilityToSucced;
            if (currentPropabilityToSucced > 1.0)
                currentPropabilityToSucced = 1.0;
            previousPropabilityToSucced = currentPropabilityToSucced;
            return 1.0- currentPropabilityToSucced;
        }

        public void ResetDistributionToFail()
        {
            expDistributionXForFail = 1;
        }

        public double DistributionValueToRepair()
        {
            var currentPropabilityToNotRepair = Math.Exp((-1 * (expDistributionXForRepair++ / MTTR))) / previousPropabilityToNotRepair;
            if (currentPropabilityToNotRepair > 1.0)
                currentPropabilityToNotRepair = 1.0;
            previousPropabilityToNotRepair = currentPropabilityToNotRepair;
            return 1.0-currentPropabilityToNotRepair;
        }

        public void ResetDistributionToRepair()
        {
            expDistributionXForRepair = 1;
        }

    }
}
