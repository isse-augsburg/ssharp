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
		public static bool IsSatisfiable(this IEnumerable<ICapability> required, IEnumerable<ICapability> available)
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

		private static Dictionary<IngredientType, int> GroupIngredientAmounts(IEnumerable<ICapability> capabilities)
		{
			return capabilities.OfType<Ingredient>()
							   .GroupBy(ingredient => ingredient.Type, ingredient => (int)ingredient.Amount)
							   .ToDictionary(group => group.Key, group => group.Sum());
		}
	}

	/// <summary>
	///   Represents the addition of a specified amount of a certain ingredient to the container.
	/// </summary>
	public class Ingredient : Capability<Ingredient>
	{
		public Ingredient(IngredientType type, uint amount)
		{
			Type = type;
			Amount = amount;
		}

		public override CapabilityType CapabilityType => CapabilityType.Process;

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