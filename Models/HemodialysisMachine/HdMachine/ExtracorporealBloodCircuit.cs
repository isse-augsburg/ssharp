using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using Utilities;

	class ExtracorporealBloodCircuit
	{
		class ArterialBloodPump
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		class ArteriaPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class HeparinPump
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		class ArtierialChamber
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		class VenousChamber
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		class VenousPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class VenousSafetyDetector
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		class VenousTubingValve
		{
			public DirectBloodFlow BloodFlow = new DirectBloodFlow();
		}

		public CompositeBloodFlow BloodFlow = new CompositeBloodFlow();  

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
			BloodFlowConnector.Connector.ConnectInWithIn(BloodFlow, _arterialBloodPump.BloodFlow, _arteriaPressureTransducer.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(_arterialBloodPump.BloodFlow, _heparinPump.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(_heparinPump.BloodFlow, _arterialChamber.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(_arterialChamber.BloodFlow, dialyzer.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(dialyzer.BloodFlow, _venousChamber.BloodFlow, _venousPressureTransducer.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(_venousChamber.BloodFlow, _venousSafetyDetector.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithIn(_venousSafetyDetector.BloodFlow, _venousTubingValve.BloodFlow);
			BloodFlowConnector.Connector.ConnectOutWithOut(_venousTubingValve.BloodFlow, BloodFlow);
		}

	}
}
