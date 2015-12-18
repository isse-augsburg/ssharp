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
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class ArteriaPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class HeparinPump
		{
			public BloodFlowSource BloodFlow = new BloodFlowSource(() => new Blood());
		}

		class ArtierialChamber
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousChamber
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class VenousSafetyDetector
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousTubingValve
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		public BloodFlowComposite BloodFlow = new BloodFlowComposite();  

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
			BloodFlowConnector.Instance.ConnectInWithIn(BloodFlow,
														new IFlowIn<Blood>[] {_arterialBloodPump.BloodFlow, _arteriaPressureTransducer.BloodFlow});
			BloodFlowConnector.Instance.ConnectOutWithIn(new IFlowOut<Blood>[]{_arterialBloodPump.BloodFlow,_heparinPump.BloodFlow},
														 _arterialChamber.BloodFlow);
			BloodFlowConnector.Instance.ConnectOutWithIn(_arterialChamber.BloodFlow,
														 dialyzer.BloodFlow);
			BloodFlowConnector.Instance.ConnectOutWithIn(dialyzer.BloodFlow,
														 new IFlowIn<Blood>[] {_venousChamber.BloodFlow, _venousPressureTransducer.BloodFlow});
			BloodFlowConnector.Instance.ConnectOutWithIn(_venousChamber.BloodFlow,
														 _venousSafetyDetector.BloodFlow);
			BloodFlowConnector.Instance.ConnectOutWithIn(_venousSafetyDetector.BloodFlow,
														 _venousTubingValve.BloodFlow);
			BloodFlowConnector.Instance.ConnectOutWithOut(_venousTubingValve.BloodFlow,
														  BloodFlow);
		}

	}
}
