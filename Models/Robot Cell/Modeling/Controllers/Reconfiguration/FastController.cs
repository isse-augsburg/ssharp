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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using SafetySharp.Modeling;
	using Odp;
	using Odp.Reconfiguration;

	/// <summary>
	///   An <see cref="IController" /> implementation that is much faster than
	///   the MiniZinc implementation.
	/// </summary>
	internal class FastController : Odp.Reconfiguration.FastController
	{
		protected override bool PreferCapabilityAccumulation => false;

		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly HashSet<CartAgent> _usedCarts = new HashSet<CartAgent>();

		public FastController(Agent[] agents) : base(agents) { }

		public override Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task)
		{
			_usedCarts.Clear();
			return base.CalculateConfigurationsAsync(context, task);
		}

		protected override IEnumerable<int> GetShortestPath(int from, int to)
		{
			var previous = -1;
			for (var current = from; current != to; current = _pathMatrix[current, to])
			{
				var agent = (_availableAgents[current] is CartAgent) ? GetPreferredCart(current, previous, to) : current;
				yield return agent;
				previous = agent;
			}
			yield return to;
		}

		private int GetPreferredCart(int suggestion, int previous, int destination)
		{
			var nextRobot = (RobotAgent)_availableAgents[_pathMatrix[suggestion, destination]];

			var unusedCart = _availableAgents[previous].Outputs
				.OfType<CartAgent>()
				.FirstOrDefault(candidate => !_usedCarts.Contains(candidate) && candidate.Outputs.Contains(nextRobot));

			if (unusedCart != null)
			{
				_usedCarts.Add(unusedCart);
				var cartID = Array.IndexOf(_availableAgents, unusedCart);
				return cartID;
			}
			return suggestion;
		}
	}
}