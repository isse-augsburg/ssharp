using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using Utilities;

	class ExtracorporealBloodCircuit : IBloodFlowIn, IBloodFlowOut
	{
		class ArterialBloodPump : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		class ArteriaPressureTransducer : IBloodFlowIn
		{
			public Func<BloodUnit> FlowUnitBefore { get; set; }

			void Update()
			{
				var incomingValue = FlowUnitBefore();
				// if (incomingValue.A >= 2) {alarm=true;}
			}
		}

		class HeparinPump : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		class ArtierialChamber : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		class VenousChamber : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		class VenousPressureTransducer : IBloodFlowIn
		{
			public Func<BloodUnit> FlowUnitBefore { get; set; }

			void Update()
			{
				var incomingValue = FlowUnitBefore();
				// if (incomingValue.A >= 2) {alarm=true;}
			}
		}

		class VenousSafetyDetector : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		class VenousTubingValve : DirectBloodFlow, IBloodFlowIn, IBloodFlowOut
		{
		}

		public Func<BloodUnit> FlowUnitBefore { get; set; }
		public Func<BloodUnit> FlowUnitAfterwards { get; set; }

		private readonly ArterialBloodPump _arterialBloodPump = new ArterialBloodPump();
		private readonly ArteriaPressureTransducer _arteriaPressureTransducer = new ArteriaPressureTransducer();
		private readonly HeparinPump _heparinPump = new HeparinPump();
		private readonly ArtierialChamber _arterialChamber  = new ArtierialChamber();
		private readonly VenousChamber _venousChamber = new VenousChamber();
		private readonly VenousPressureTransducer _venousPressureTransducer = new VenousPressureTransducer();
		private readonly VenousSafetyDetector _venousSafetyDetector = new VenousSafetyDetector();
		private readonly VenousTubingValve _venousTubingValve = new VenousTubingValve();

		public ExtracorporealBloodCircuit(Dialyzer dialyzer)
		{
			// The order of the connections matter
			BloodFlowConnection.ConnectInWithIn(this, _arterialBloodPump);
			BloodFlowConnection.ConnectOutWithIn(_arterialBloodPump, _heparinPump);
			BloodFlowConnection.ConnectOutWithIn(this, _arteriaPressureTransducer);
			BloodFlowConnection.ConnectOutWithIn(_heparinPump, _arterialChamber);
			BloodFlowConnection.ConnectOutWithIn(_arterialChamber, dialyzer);
			BloodFlowConnection.ConnectOutWithIn(dialyzer, _venousChamber);
			BloodFlowConnection.ConnectOutWithIn(dialyzer, _venousPressureTransducer);
			BloodFlowConnection.ConnectOutWithIn(_venousChamber, _venousSafetyDetector);
			BloodFlowConnection.ConnectOutWithIn(_venousSafetyDetector, _venousTubingValve);
			BloodFlowConnection.ConnectOutWithOut(_venousTubingValve, this);
		}

	}
}
