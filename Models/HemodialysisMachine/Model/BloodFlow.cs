using System;

namespace HemodialysisMachine.Model
{
	using System.ComponentModel;
	using Utilities.BidirectionalFlow;

	class Blood : IElement<Blood>
	{
		public int Water = 0;
		public int SmallWasteProducts = 0;
		public int BigWasteProducts = 0;
		public bool HasHeparin = false;
		public bool ChemicalCompositionOk = true;
		public bool GasFree = false;
		public QualitativePressure Pressure = QualitativePressure.NoPressure;
		public QualitativeTemperature Temperature = QualitativeTemperature.TooCold;

		public void CopyValuesFrom(Blood from)
		{
			Water = from.Water;
			SmallWasteProducts = from.SmallWasteProducts;
			BigWasteProducts = from.BigWasteProducts;
			HasHeparin = from.HasHeparin;
			ChemicalCompositionOk = from.ChemicalCompositionOk;
			GasFree = from.GasFree;
			Pressure = from.Pressure;
			Temperature = from.Temperature;
		}

		public void PrintBloodValues()
		{
			System.Console.Out.WriteLine("\tWater: " + Water);
			System.Console.Out.WriteLine("\tSmallWasteProducts: " + SmallWasteProducts);
			System.Console.Out.WriteLine("\tBigWasteProducts: " + BigWasteProducts);
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


	class BloodFlowVirtualSplitter : FlowVirtualSplitter<Blood, Suction>, IIntFlowComponent
	{
		public BloodFlowVirtualSplitter(int number)
			: base(number)
		{
		}

		public override void SplitForwards(Blood source, Blood[] targets)
		{
			StandardBehaviorSplitForwardsEqual(source, targets);
		}

		public override void MergeBackwards(Suction[] sources, Suction target)
		{
			StandardBehaviorMergeBackwardsSelectFirst(sources, target);
		}
	}

	class BloodFlowVirtualMerger : FlowVirtualMerger<Blood, Suction>, IIntFlowComponent
	{
		public BloodFlowVirtualMerger(int number)
			: base(number)
		{
		}

		public override void SplitBackwards(Suction source, Suction[] targets)
		{
			StandardBehaviorSplitBackwardsEqual(source, targets);
		}

		public override void MergeForwards(Blood[] sources, Blood target)
		{
			StandardBehaviorMergeForwardsSelectFirst(sources, target);
		}
	}

	class BloodFlowCombinator : FlowCombinator<Blood, Suction>, IIntFlowComponent
	{
		public override FlowVirtualMerger<Blood, Suction> CreateFlowVirtualMerger(int elementNos)
		{
			return new BloodFlowVirtualMerger(elementNos);
		}

		public override FlowVirtualSplitter<Blood, Suction> CreateFlowVirtualSplitter(int elementNos)
		{
			return new BloodFlowVirtualSplitter(elementNos);
		}
	}

	class BloodFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<Blood, Suction>
	{
	}

	class BloodFlowUniqueIncomingStub : FlowUniqueIncomingStub<Blood, Suction>
	{
	}
}
