namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;
	
	public class Patient : Component
	{
		// Patient is the source and the sink of blood
		public readonly BloodFlowSource ArteryFlow = new BloodFlowSource();
		public readonly BloodFlowSink VeinFlow = new BloodFlowSink();

		[Range(0, 100, OverflowBehavior.Error)]
		public int Water = 50;
		[Range(0, 16, OverflowBehavior.Error)]
		public int SmallWasteProducts = 10;
		[Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts = 3; //Only removable by ultrafiltration

		public bool IsConnected = true;

		[Provided]
		public void CreateBlood(Blood outgoingBlood)
		{
			var incomingSuction = ArteryFlow.Outgoing.BackwardFromSuccessor;
			var hasSuction = incomingSuction.SuctionType == SuctionType.CustomSuction && incomingSuction.CustomSuctionValue > 0;
			if (hasSuction && IsConnected)
			{
				var totalUnitsToDeliver = ArteryFlow.Outgoing.BackwardFromSuccessor.CustomSuctionValue;
				var bigWasteUnitsToDeliver = totalUnitsToDeliver / 2;
				if (BigWasteProducts >= bigWasteUnitsToDeliver)
				{
					outgoingBlood.BigWasteProducts = bigWasteUnitsToDeliver;
					var waterUnitsToDeliver = totalUnitsToDeliver - bigWasteUnitsToDeliver;
					outgoingBlood.Water = waterUnitsToDeliver;
				} 
				else
				{
					outgoingBlood.BigWasteProducts = BigWasteProducts; // Deliver rest of unfiltrated blood or none
					var waterUnitsToDeliver = totalUnitsToDeliver - outgoingBlood.BigWasteProducts;
					outgoingBlood.Water = waterUnitsToDeliver;
				}
				if (SmallWasteProducts >= outgoingBlood.Water)
				{
					outgoingBlood.SmallWasteProducts = outgoingBlood.Water;
				}
				else
				{
					outgoingBlood.SmallWasteProducts = SmallWasteProducts; // Deliver rest of unfiltrated blood or none
				}
				Water -= outgoingBlood.Water;
				SmallWasteProducts -= outgoingBlood.SmallWasteProducts;
				BigWasteProducts -= outgoingBlood.BigWasteProducts;
				outgoingBlood.HasHeparin = true;
				outgoingBlood.ChemicalCompositionOk = true;
				outgoingBlood.GasFree = true;
				outgoingBlood.Pressure = QualitativePressure.GoodPressure;
				outgoingBlood.Temperature = QualitativeTemperature.BodyHeat;
			}
			else
			{
				outgoingBlood.Water = 0;
				outgoingBlood.BigWasteProducts = 0;
				outgoingBlood.SmallWasteProducts = 0;
				outgoingBlood.HasHeparin = true;
				outgoingBlood.ChemicalCompositionOk = true;
				outgoingBlood.GasFree = true;
				outgoingBlood.Pressure = QualitativePressure.NoPressure;
				outgoingBlood.Temperature = QualitativeTemperature.BodyHeat;
			}
		}

		[Provided]
		public void CreateBloodSuction(Suction outgoingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
			outgoingSuction.CustomSuctionValue = 0;
		}

		[Provided]
		public void BloodReceived(Blood incomingBlood)
		{
			Water += incomingBlood.Water;
			SmallWasteProducts += incomingBlood.SmallWasteProducts;
			BigWasteProducts += incomingBlood.BigWasteProducts;
		}

		[Provided]
		public void DoNothing(Suction incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ArteryFlow.SetOutgoingForward), nameof(CreateBlood));
			Bind(nameof(ArteryFlow.BackwardFromSuccessorWasUpdated), nameof(DoNothing));
			Bind(nameof(VeinFlow.SetOutgoingBackward), nameof(CreateBloodSuction));
			Bind(nameof(VeinFlow.ForwardFromPredecessorWasUpdated), nameof(BloodReceived));
		}

		public void PrintBloodValues(string pointOfTime)
		{
			System.Console.Out.WriteLine("\t" + "Patient (" + pointOfTime + ")");
			System.Console.Out.WriteLine("\t\tWater: " + Water);
			System.Console.Out.WriteLine("\t\tSmallWasteProducts: " + SmallWasteProducts);
			System.Console.Out.WriteLine("\t\tBigWasteProducts: " + BigWasteProducts);
		}

		public string ValuesAsText()
		{
			return
			"Water: " + Water +
			"\nSmallWasteProducts: " + SmallWasteProducts +
			"\nBigWasteProducts: " + BigWasteProducts;
		}
	}
}
