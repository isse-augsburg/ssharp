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

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
	using System;
    using System.Collections.Generic;
	using System.Linq;
	using Controllers;
	using Plants;

	internal partial class ModelBuilder
	{
		private static class RouteHelper
		{
			public static Tuple<int, int>[] ComputeRoutes(Tuple<int, int>[] routes)
			{
				return TransitiveClosure(routes)
					.Distinct() // no duplicate routes
					.Where(route => route.Item1 != route.Item2) // for efficiency (less faults), remove reflexive routes
					.ToArray();
			}

			private static IEnumerable<Tuple<int, int>> TransitiveClosure(Tuple<int, int>[] routes)
			{
				var robotCount = routes.SelectMany(r => new[] { r.Item1, r.Item2 }).Max() + 1;
			    var connected = new bool[robotCount, robotCount]; // initialized to false

                // Routes are bidirectional, reflexive routes are ignored.
                // To improve efficiency, we can thus only compute routes
                // (r1, r2) where r1 < r2.

                // direct routes
                foreach (var route in routes)
			    {
                    var robot1 = Math.Min(route.Item1, route.Item2);
			        var robot2 = Math.Max(route.Item1, route.Item2);

			        connected[robot1, robot2] = true;
			        yield return Tuple.Create(robot1, robot2);
			    }

                // Floyd-Warshall algorithm
                for (var link = 0; link < robotCount; ++link)
                {
                    for (var start = 0; start < robotCount; ++start)
                    {
                        for (var end = start + 1; end < robotCount; ++end)
                        {
                            if (connected[start, end] || !connected[start, link] || !connected[link, end])
                                continue;
                            connected[start, end] = true;
                            yield return Tuple.Create(start, end);
                        }
                    }
                }
            }

            public static Route[] ToRoutes(Tuple<int, int>[] routeSpecifications, Model model)
			{
				return routeSpecifications.Select(spec => new Route(model.Robots[spec.Item1], model.Robots[spec.Item2]))
					.ToArray();
			}

			public static void Connect(Agent agent, Tuple<int, int>[] routes, Model model)
			{
				foreach (var route in routes)
				{
					model.RobotAgents[route.Item1].BidirectionallyConnect(agent);
					model.RobotAgents[route.Item2].BidirectionallyConnect(agent);
				}
			}
		}
	}
}
