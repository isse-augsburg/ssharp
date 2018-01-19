﻿// The MIT License (MIT)
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

namespace SafetySharp.CaseStudies.PillProduction.Modeling
{
	using System.Linq;
	using Odp;

	/// <summary>
	///   Modifies <see cref="Odp.Reconfiguration.FastConfigurationFinder"/> to account for ingredient amounts consumed during the processing of a resource.
	/// </summary>
	internal class FastConfigurationFinder : Odp.Reconfiguration.FastConfigurationFinder
	{
		public FastConfigurationFinder() : base(preferCapabilityAccumulation: true) { }

		// override necessary due to ingredient amounts
		protected override bool CanSatisfyNext(TaskFragment taskFragment, BaseAgent[] availableAgents, int[] path, int prefixLength, int station)
		{
			var capabilities = from index in Enumerable.Range(0, prefixLength + 1)
							   where index == prefixLength || path[index] == station
							   select taskFragment[index];
			return capabilities.ToArray().IsSatisfiable(availableAgents[station].AvailableCapabilities);
		}
	}
}