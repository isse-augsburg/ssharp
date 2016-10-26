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
	using Odp;

	/// <summary>
	///   A pill container which is filled with different ingredients.
	/// </summary>
	public class PillContainer : Resource
	{
		public Recipe Recipe => (Recipe)Task;

		/// <summary>
		///   Tells the container it was loaded on the conveyor belt.
		/// </summary>
		/// <param name="recipe">The recipe according to which it will henceforth be processed.</param>
		public void OnLoaded(Recipe recipe)
		{
			if (Task != null)
				throw new InvalidOperationException("Container already belongs to a recipe");
			Task = recipe;
			OnCapabilityApplied(Task.RequiredCapabilities[0]); // first capability will always be ProduceCapability
		}

		/// <summary>
		///   Adds an ingredient to the container.
		/// </summary>
		/// <param name="ingredient"></param>
		public void AddIngredient(Ingredient ingredient)
		{
			OnCapabilityApplied(ingredient);
		}
	}
}