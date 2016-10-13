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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;
	using Odp;

	/// <summary>
	///   An <see cref="Odp.IController{Agent, Task}" /> implementation that is much faster than
	///   the MiniZinc implementation.
	/// </summary>
	internal class FastController : FastController<Agent, Task>
	{
		[Hidden(HideElements = true)]
		private readonly HashSet<CartAgent> _usedCarts = new HashSet<CartAgent>();

		public FastController(IEnumerable<Agent> agents) : base(agents.ToArray()) { }

		public override Dictionary<Agent, IEnumerable<Role<Agent, Task>>> CalculateConfigurations(params Task[] tasks)
		{
			_usedCarts.Clear();
			return base.CalculateConfigurations(tasks);
		}

		protected override IEnumerable<int> GetShortestPath(int from, int to)
		{
			int previous = -1;
			for (int current = from; current != to; current = _pathMatrix[current, to])
			{
				int agent = (_availableAgents[current] is CartAgent) ? GetPreferredCart(current, previous, to) : current;
				yield return agent;
				previous = agent;
			}
			yield return to;
		}

		private int GetPreferredCart(int suggestion, int previous, int destination)
		{
			var cart = _availableAgents[suggestion] as CartAgent;
			var nextRobot = (RobotAgent)_availableAgents[_pathMatrix[suggestion, destination]];

			var unusedCart = _availableAgents[previous].Outputs
				.OfType<CartAgent>()
				.Where(candidate => !_usedCarts.Contains(candidate) && candidate.Outputs.Contains(nextRobot))
				.FirstOrDefault();

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