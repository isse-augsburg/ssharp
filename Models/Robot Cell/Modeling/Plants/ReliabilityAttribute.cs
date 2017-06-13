using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Plants
{
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


        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ReliabilityAttribute(double mttf, double mttr)
        {
            if (mttf <= 0 || mttr <= 0)
                throw new ArgumentOutOfRangeException();
            this.MTTF = mttf;
            this.MTTR = mttr;
        }

        public double DistributionValueToFail()
        {
            return 1 - Math.Exp((-1 * (1 / MTTF) * expDistributionXForFail++));
        }

        public void ResetDistributionToFail()
        {
            expDistributionXForFail = 1;
        }

        public double DistributionValueToRepair()
        {
            return 1 - Math.Exp((-1 * (1 / MTTR) * expDistributionXForRepair++));
        }

        public void ResetDistributionToRepair()
        {
            expDistributionXForRepair = 1;
        }

    }
}
