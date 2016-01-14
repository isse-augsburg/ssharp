using System;

namespace HemodialysisMachine.Model
{
	using System.ComponentModel;
	using Utilities.BidirectionalFlow;

	class Blood : IElement<Blood>
	{
		public int UnfiltratedBloodUnits = 0;
		public int FiltratedBloodUnits = 0;
		public bool HasHeparin = false;
		public bool ChemicalCompositionOk = true;
		public bool GasFree = false;
		public QualitativePressure Pressure = QualitativePressure.NoPressure;
		public QualitativeTemperature Temperature = QualitativeTemperature.TooCold;

		public void CopyValuesFrom(Blood from)
		{
			UnfiltratedBloodUnits = from.UnfiltratedBloodUnits;
			FiltratedBloodUnits = from.FiltratedBloodUnits;
			HasHeparin = from.HasHeparin;
			ChemicalCompositionOk = from.ChemicalCompositionOk;
			GasFree = from.GasFree;
			Pressure = from.Pressure;
			Temperature = from.Temperature;
		}
	}


	class BloodFlowInToOutSegment : FlowInToOutSegment<Blood, Suction>
	{
	}

	class BloodFlowSource : FlowSource<Blood, Suction>
	{
	}

	class BloodFlowSink : FlowSink<Blood, Suction>
	{
	}

	class BloodFlowComposite : FlowComposite<Blood, Suction>
	{
	}

	class BloodFlowCombinator : FlowCombinator<Blood, Suction>
	{
	}

	class BloodFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<Blood, Suction>
	{
	}

	class BloodFlowUniqueIncomingStub : FlowUniqueIncomingStub<Blood, Suction>
	{
	}
}
