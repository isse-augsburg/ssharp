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
	using System.Collections.Generic;
	using System.Linq;

	using Odp;

	public static class CapabilityHelper
	{
		public static bool IsSatisfiable(this ICapability[] required, ICapability[] available)
		{
			if (required.OfType<ProduceCapability>().Any() && !available.OfType<ProduceCapability>().Any())
				return false;
			if (required.OfType<ConsumeCapability>().Any() && !available.OfType<ConsumeCapability>().Any())
				return false;

			var requiredAmounts = GroupIngredientAmounts(required);
			var availableAmounts = GroupIngredientAmounts(available);

			foreach (var pair in requiredAmounts)
			{
				int value;
				if (!availableAmounts.TryGetValue(pair.Key, out value) || value < pair.Value)
					return false;
			}

			return true;
		}

		private static Dictionary<IngredientType, int> GroupIngredientAmounts(ICapability[] capabilities)
		{
			return capabilities.OfType<Ingredient>()
							   .GroupBy(ingredient => ingredient.Type, ingredient => (int)ingredient.Amount)
							   .ToDictionary(group => group.Key, group => group.Sum());
		}
	}

	/// <summary>
	///   Represents the loading of empty pill containers on the conveyor belt.
	/// </summary>
	public sealed class ProduceCapability : ICapability
	{
		public override bool Equals(object obj)
		{
			return obj is ProduceCapability;
		}

		public override int GetHashCode()
		{
			return 17;
		}
	}

	/// <summary>
	///   Represents the removal of pill containers from the conveyor belt, labeling and palletization.
	/// </summary>
	public sealed class ConsumeCapability : ICapability
	{
		public override bool Equals(object obj)
		{
			return obj is ConsumeCapability;
		}

		public override int GetHashCode()
		{
			return 31;
		}
	}

	/// <summary>
	///   Represents the addition of a specified amount of a certain ingredient to the container.
	/// </summary>
	public class Ingredient : ICapability
	{
		public Ingredient(IngredientType type, uint amount)
		{
			Type = type;
			Amount = amount;
		}

		public IngredientType Type { get; }

		public uint Amount { get; }

		public override bool Equals(object obj)
		{
			var other = obj as Ingredient;
			if (other != null)
				return other.Type == Type && other.Amount == Amount;
			return false;
		}

		public override int GetHashCode()
		{
			return (int)Type + 57 * (int)Amount;
		}
	}
}