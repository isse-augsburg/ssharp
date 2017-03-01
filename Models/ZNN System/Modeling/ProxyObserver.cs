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

using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	public class ProxyObserver : Component
	{
		/// <summary>
		/// The Proxy
		/// </summary>
		public ProxyT Proxy { get; }

		/// <summary>
		/// Sets if a server adjustment is possible
		/// </summary>
		public ReconfStates ReconfigurationState = ReconfStates.NotSet;

		/// <summary>
		/// Constraints for reconfiguration possible
		/// </summary>
		[Hidden(HideElements = true)]
		public List<Func<bool>> ReconfPossibleConstraints { get; set; }

		/// <summary>
		/// Constraints for server adjustment needed
		/// </summary>
		[Hidden(HideElements = true)]
		public List<Func<bool>> AdjustmentNeededConstraints { get; set; }

		/// <summary>
		/// Creates a new proxy observer for the given <see cref="ProxyT"/>
		/// </summary>
		/// <param name="proxy">The proxy observed</param>
		public ProxyObserver(ProxyT proxy)
		{
			Proxy = proxy;

			GenerateConstraints();
		}

		/// <summary>
		/// Generate reconfiguration constraints
		/// </summary>
		private void GenerateConstraints()
		{
			ReconfPossibleConstraints = new List<Func<bool>>
			{
				// Reconfiguration Possible
				() => Proxy.ConnectedServers.Count > 0,
				() => Model.MaxBudget > 0,
				() => Proxy.ConnectedServers.Count(s => s.IsServerDead) < Proxy.ConnectedServers.Count,
			};


			AdjustmentNeededConstraints = new List<Func<bool>>
			{
				// Adjustment needed
				() => Proxy.AvgResponseTime > Model.HighResponseTimeValue ||
					  Proxy.AvgResponseTime < Model.LowResponseTimeValue ||
					  //Proxy.TotalServerCosts > Model.MaxBudget ||
					  Proxy.TotalServerCosts > Model.MaxBudget * 0.75
			};
		}

		/// <summary>
		/// Checks the given constraints
		/// </summary>
		/// <param name="constraints">The contraints to checked</param>
		/// <returns>False if any constraint is false</returns>
		public bool CheckConstraints(List<Func<bool>> constraints)
		{
			var constr = constraints.Select(constraint => constraint());
			if(constr.Any(constraint => !constraint))
			{
				return false;
			}
			return true;
		}

		public override void Update()
		{
			var canReconf = CheckConstraints(ReconfPossibleConstraints);
			if(!canReconf)
			{
				ReconfigurationState = ReconfStates.Failed;
				throw new Exception("No Reconfiguration Possible");
			}

			var needReconf = CheckConstraints(AdjustmentNeededConstraints);
			if(needReconf)
			{
				var oldActiveServerCount = Proxy.ActiveServerCount;
				var oldServerFidelity = Proxy.CurrentServerFidelity;

				Proxy.AdjustServers();
				ReconfigurationState=ReconfStates.Succedded;

				if(oldActiveServerCount == Proxy.ActiveServerCount && oldServerFidelity == Proxy.CurrentServerFidelity && Proxy.AvgResponseTime > Model.LowResponseTimeValue)
				{
					ReconfigurationState = ReconfStates.Failed;
					throw new Exception("Not reconfigured although it was possible");
				}
			}
		}
	}
}