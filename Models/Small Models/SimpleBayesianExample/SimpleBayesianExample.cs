namespace SafetySharp.CaseStudies.SmallModels.SimpleBayesianExample
{
    using ISSE.SafetyChecking.Modeling;
    using Modeling;

    public class SimpleBayesianExampleComponent : Component
    {
        public SimpleBayesianExampleComponent()
        {
            FL = new TransientFault();
            FL.ProbabilityOfOccurrence = new Probability(0.4);

            FV = new TransientFault();
            FV.ProbabilityOfOccurrence = new Probability(0.01);

            FS = new TransientFault();
            FS.ProbabilityOfOccurrence = new Probability(0.05);
        }

        public Fault FL, FV, FS;

        public int Step;
        public bool SubsystemError;
        public bool SensorDefect;
        public bool NoDataAvailable;
        public bool Hazard => SubsystemError && SensorDefect;

        public override void Update()
        {
            if (Step >= 3)
                return;

            if (Step == 0)
                RequestData();
            if (Step == 2 || NoDataAvailable)
            {
                CheckSensor();
                AskSubsystem();
            }

            Step++;
        }

        public virtual void RequestData()
        {
            // Get Data from some oracle
        }

        public virtual void AskSubsystem()
        {
            // Request data from own subsystem
            SubsystemError = false;
        }

        public virtual void CheckSensor()
        {
            // Measure data from own sensor
        }


        [FaultEffect(Fault = nameof(FL)), Priority(1)]
        public class FLEffect : SimpleBayesianExampleComponent
        {
            public override void RequestData()
            {
                // Oracle data was flawed or not available
                NoDataAvailable = true;
            }
        }

        [FaultEffect(Fault = nameof(FV)), Priority(0)]
        public class FVEffect : SimpleBayesianExampleComponent
        {
            public override void AskSubsystem()
            {
                // Data of the subsystem was flawed or not available
                SubsystemError = true;
            }
        }

        [FaultEffect(Fault = nameof(FS)), Priority(2)]
        public class FSEffect : SimpleBayesianExampleComponent
        {
            public override void CheckSensor()
            {
                // Sensor breaks and is from now on defect
                SensorDefect = true;
            }
        }
    }


    public sealed class SimpleBayesianExampleModel : ModelBase
    {
        [Root(RootKind.Controller)]
        public SimpleBayesianExampleComponent Component { get; } = new SimpleBayesianExampleComponent();
    }

}