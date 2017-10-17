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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Plants
{
	using System;

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
