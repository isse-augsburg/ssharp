// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.PillProduction.Modeling
{
	using SafetySharp.Modeling;
	using Odp;

	/// <summary>
	///   A production station that removes containers from the conveyor belt, closes, labels and stores them on pallets.
	/// </summary>
	public class PalletisationStation : Station
	{
		public readonly Fault PalletisationDefect = new PermanentFault();

		public PalletisationStation()
		{
			CompleteStationFailure.Subsumes(PalletisationDefect);
		}

		public override ICapability[] AvailableCapabilities { get; } = { new ConsumeCapability() };

		protected override void ExecuteRole(Role role)
		{
			// unless role is transport only, it will always be { ConsumeCapability }
			if (role.HasCapabilitiesToApply())
			{
				Container.Recipe.RemoveContainer(Container);
				if (Container.Recipe.ProcessingComplete)
				{
					RemoveRecipeConfigurations(Container.Recipe);
				}
				Container = null;
			}
		}

		[FaultEffect(Fault = nameof(PalletisationDefect))]
		public class PalletisationDefectEffect : PalletisationStation
		{
			public override ICapability[] AvailableCapabilities => new ICapability[0];
		}

		[FaultEffect(Fault = nameof(CompleteStationFailure))]
		public class CompleteStationFailureEffect : PalletisationStation
		{
			public override bool IsAlive => false;

			public override void Update()
			{
			}
		}
	}
}