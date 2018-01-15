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

namespace SafetySharp.CaseStudies.SmallModels.DeadReckoning
{
    using ISSE.SafetyChecking.Modeling;
    using Modeling;

    public class DeadReckoningComponent : Component
    {
        public DeadReckoningComponent()
        {
            FF = new TransientFault();
            FF.ProbabilityOfOccurrence = new Probability(0.4);

            FC = new TransientFault();
            FC.ProbabilityOfOccurrence = new Probability(0.01);

            FS = new PermanentFault();
            FS.ProbabilityOfOccurrence = new Probability(0.05);
        }

        public Fault FF, FC, FS;

        public int Step;
        public bool CalculationError;
        public bool SensorValueWrong;
        public bool NoDataAvailable;
        public bool Hazard => CalculationError && SensorValueWrong;

        public override void Update()
        {
            if (Step >= 3)
                return;

            if (Step == 0)
                RequestFix();

            if (Step == 2 || NoDataAvailable)
            {
                CheckSensor();
                CalculatePosition();
            }

            Step++;
        }

        public virtual void RequestFix()
        {
            // Get Data from some oracle
        }

        public virtual void CalculatePosition()
        {
            // Calculate new position
            CalculationError = false;
        }

        public virtual void CheckSensor()
        {
            // Measure data from own sensor
        }


        [FaultEffect(Fault = nameof(FF)), Priority(1)]
        public class FFEffect : DeadReckoningComponent
        {
            public override void RequestFix()
            {
                // Fix was flawed or not available
                NoDataAvailable = true;
            }
        }

        [FaultEffect(Fault = nameof(FC)), Priority(0)]
        public class FCEffect : DeadReckoningComponent
        {
            public override void CalculatePosition()
            {
                // Data of the subsystem was flawed or not available.
				// But can be better next time step
                CalculationError = true;
            }
        }

        [FaultEffect(Fault = nameof(FS)), Priority(2)]
        public class FSEffect : DeadReckoningComponent
        {
            public override void CheckSensor()
            {
                // Sensor breaks and is from now on defect
                SensorValueWrong = true;
            }
        }
    }


    public sealed class DeadReckoningModel : ModelBase
    {
        [Root(RootKind.Controller)]
        public DeadReckoningComponent Component { get; } = new DeadReckoningComponent();
    }

}