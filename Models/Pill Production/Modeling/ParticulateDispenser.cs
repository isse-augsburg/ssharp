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
	using System;
	using SafetySharp.Modeling;
	using Odp;

	/// <summary>
	///   A production station that adds ingredients to the containers.
	/// </summary>
	public partial class ParticulateDispenser : Station
	{
		public readonly Fault DispenserDefect = new PermanentFault();

		private readonly IngredientTank[] _ingredientTanks;

		public ParticulateDispenser()
		{
			_ingredientTanks = Array.ConvertAll(
				(IngredientType[])Enum.GetValues(typeof(IngredientType)),
				type => new IngredientTank(Name, type)
				);

			CompleteStationFailure.Subsumes(DispenserDefect);
			DispenserDefect.Subsumes(BlueTankDepleted, RedTankDepleted, YellowTankDepleted);
		}

		public override ICapability[] AvailableCapabilities
			=> Array.ConvertAll(_ingredientTanks, tank => tank.Capability);

		// for convenience
		public Fault BlueTankDepleted => _ingredientTanks[(int)IngredientType.BlueParticulate].TankDepleted;
		public Fault RedTankDepleted => _ingredientTanks[(int)IngredientType.RedParticulate].TankDepleted;
		public Fault YellowTankDepleted => _ingredientTanks[(int)IngredientType.YellowParticulate].TankDepleted;

		public void SetStoredAmount(IngredientType ingredientType, uint amount)
		{
			_ingredientTanks[(int)ingredientType].Amount = amount;
		}

		protected override void ExecuteRole(Role role)
		{
			foreach (var capability in role.CapabilitiesToApply)
			{
				var ingredient = capability as Ingredient;
				if (ingredient == null)
					throw new InvalidOperationException($"Invalid capability in ParticulateDispenser: {capability}");

				_ingredientTanks[(int)ingredient.Type].Dispense(Container, ingredient);
			}
		}

		[FaultEffect(Fault = nameof(DispenserDefect))]
		public class DispenserDefectEffect : ParticulateDispenser
		{
			public override ICapability[] AvailableCapabilities => new ICapability[0];
		}

		[FaultEffect(Fault = nameof(CompleteStationFailure))]
		public class CompleteStationFailureEffect : ParticulateDispenser
		{
			public override bool IsAlive => false;

			public override void Update()
			{
			}
		}
	}
}