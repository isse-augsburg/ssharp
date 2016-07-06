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

	/// <summary>
	///   Describes how a container should be processed.
	/// </summary>
	public class Recipe
	{
		private readonly List<PillContainer> _activeContainers;

		private uint _producedAmount;

		/// <summary>
		///   Creates a new recipe with the specified sequence of ingredients.
		/// </summary>
		/// <param name="ingredients">The sequence of ingredients to add to the containers.</param>
		/// <param name="amount">The number of containers to produce for this recipe.</param>
		public Recipe(Ingredient[] ingredients, uint amount)
		{
			_activeContainers = new List<PillContainer>((int)amount);
			Amount = amount;

			RequiredCapabilities = new Capability[] { new ProduceCapability() }
				.Concat(ingredients)
				.Concat(new[] { new ConsumeCapability() })
				.ToArray();
		}

		/// <summary>
		///   True if the specified <see cref="Amount" /> of containers was produced
		///   and completely processed for the recipe.
		/// </summary>
		public bool ProcessingComplete => _activeContainers.Count == 0 && _producedAmount == Amount;

		/// <summary>
		///   The sequence of capabilities defining this recipe.
		/// </summary>
		public Capability[] RequiredCapabilities { get; }

		/// <summary>
		///   The total number of containers to be produced for this recipe.
		/// </summary>
		public uint Amount { get; }

		/// <summary>
		///   The number of containers still to be produced.
		/// </summary>
		public uint RemainingAmount => Amount - _producedAmount;

		/// <summary>
		///   Adds a <paramref name="container" /> to the recipe's active containers.
		///   This is called when processing of the container starts.
		/// </summary>
		public void AddContainer(PillContainer container)
		{
			_producedAmount++;
			_activeContainers.Add(container);
		}

		/// <summary>
		///   Notifies the recipe that the given <paramref name="container" /> was
		///   completely processed and has left the production system.
		/// </summary>
		public void RemoveContainer(PillContainer container)
		{
			_activeContainers.Remove(container);
		}

		/// <summary>
		///   Notifies the recipe that production of the given
		///   <paramref name="container" /> failed.
		/// </summary>
		public void DropContainer(PillContainer container)
		{
			_producedAmount--;
			RemoveContainer(container);
		}
	}
}