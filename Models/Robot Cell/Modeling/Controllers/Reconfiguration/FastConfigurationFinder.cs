// The MIT License (MIT)
//
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;
	using Odp;

	/// <summary>
	///   Modifies <see cref="Odp.Reconfiguration.FastConfigurationFinder"/> to avoid reusing carts where possible.
	/// </summary>
	internal class FastConfigurationFinder : Odp.Reconfiguration.FastConfigurationFinder
	{
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly HashSet<CartAgent> _usedCarts = new HashSet<CartAgent>();

		public FastConfigurationFinder() : base(preferCapabilityAccumulation: false) { }

		protected override IEnumerable<int> FindResourceFlow(int[] distribution, BaseAgent[] availableAgents)
		{
			_usedCarts.Clear();
			return base.FindResourceFlow(distribution, availableAgents);
		}

		protected override IEnumerable<int> GetShortestPath(int from, int to, BaseAgent[] availableAgents)
		{
			var previous = -1;
			for (var current = from; current != to; current = _pathMatrix[current, to])
			{
				var agent = (availableAgents[current] is CartAgent) ? GetPreferredCart(current, previous, to, availableAgents) : current;
				yield return agent;
				previous = agent;
			}
			yield return to;
		}

		private int GetPreferredCart(int suggestion, int previous, int destination, BaseAgent[] availableAgents)
		{
			var nextRobot = (RobotAgent)availableAgents[_pathMatrix[suggestion, destination]];

			var unusedCart = availableAgents[previous].Outputs
				.OfType<CartAgent>()
				.FirstOrDefault(candidate => !_usedCarts.Contains(candidate)
					&& candidate.Inputs.Contains(availableAgents[previous])
					&& candidate.Outputs.Contains(nextRobot) && nextRobot.Inputs.Contains(candidate));

			if (unusedCart == null)
				return suggestion;

			_usedCarts.Add(unusedCart);
			return Array.IndexOf(availableAgents, unusedCart);
		}
	}
}