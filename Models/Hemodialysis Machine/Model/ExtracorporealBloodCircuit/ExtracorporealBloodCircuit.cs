namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;
	using Utilities.BidirectionalFlow;

	public class ExtracorporealBloodCircuit : Component
	{
		public readonly BloodFlowComposite BloodFlow = new BloodFlowComposite();
		public readonly BloodFlowDelegate FromDialyzer = new BloodFlowDelegate();
		public readonly BloodFlowDelegate ToDialyzer = new BloodFlowDelegate();

		public readonly ArterialBloodPump ArterialBloodPump = new ArterialBloodPump();
		public readonly ArteriaPressureTransducer ArteriaPressureTransducer = new ArteriaPressureTransducer();
		public readonly HeparinPump HeparinPump = new HeparinPump();
		public readonly ArtierialChamber ArterialChamber  = new ArtierialChamber();
		public readonly VenousChamber VenousChamber = new VenousChamber();
		public readonly VenousPressureTransducer VenousPressureTransducer = new VenousPressureTransducer();
		public readonly VenousSafetyDetector VenousSafetyDetector = new VenousSafetyDetector();
		public readonly VenousTubingValve VenousTubingValve = new VenousTubingValve();

		public void AddFlows(BloodFlowCombinator flowCombinator)
		{
			// The order of the connections matter
			flowCombinator.ConnectInWithIns(BloodFlow,
				new IFlowComponentUniqueIncoming<Blood, Suction>[] { ArterialBloodPump.MainFlow, ArteriaPressureTransducer.SenseFlow });
			flowCombinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Blood, Suction>[] { ArterialBloodPump.MainFlow, HeparinPump.HeparinFlow },
				ArterialChamber.MainFlow);
			flowCombinator.ConnectOutWithIn(ArterialChamber.MainFlow,
				ToDialyzer);
			flowCombinator.ConnectOutWithIns(FromDialyzer,
				new IFlowComponentUniqueIncoming<Blood, Suction>[] { VenousChamber.MainFlow, VenousPressureTransducer.SenseFlow });
			flowCombinator.ConnectOutWithIn(VenousChamber.MainFlow,
				VenousSafetyDetector.MainFlow);
			flowCombinator.ConnectOutWithIn(VenousSafetyDetector.MainFlow,
				VenousTubingValve.MainFlow);
			flowCombinator.ConnectOutWithOut(VenousTubingValve.MainFlow,
				BloodFlow);
		}

	}
}
