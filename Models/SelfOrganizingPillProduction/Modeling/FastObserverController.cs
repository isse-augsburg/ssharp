using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// An <see cref="ObserverController"/> implementation that is much faster than
    /// the MiniZinc implementation.
    /// </summary>
    class FastObserverController : ObserverController
    {
        public FastObserverController(params Station[] stations) : base(stations) { }

        public override void Configure(Recipe recipe)
        {
            RemoveObsoleteConfiguration(recipe);

            // assign int identifiers to stations
            var stations = AvailableStations;
            var identifiers = new Dictionary<Station, int>();
            for (int i = 0; i < stations.Length; ++i)
                identifiers[stations[i]] = i;

            var data = CalculateConnectionMatrix();
            var connections = data.Item1;
            var costs = data.Item2;

            // find optimal path that satisfies the required capabilities
            var path = FindStationPath(recipe, identifiers, connections, costs);
            if (path == null)
            {
                Unsatisfiable = true;
                return;
            }

            ApplyConfiguration(recipe, path, connections);
        }

        /// <summary>
        /// Calculates the connection matrix for the available stations.
        /// </summary>
        /// <returns>A tuple containing the successor matrix and the path length matrix. -1 indicates no successor / infinite costs.</returns>
        private Tuple<int[,], int[,]> CalculateConnectionMatrix()
        {
            var stations = AvailableStations;

            var paths = new int[stations.Length, stations.Length];
            var costs = new int[stations.Length, stations.Length];

            for (int i = 0; i < stations.Length; ++i)
            {
                for (int j = 0; j < stations.Length; ++j)
                {
                    // neighbours
                    if (stations[i].Outputs.Contains(stations[j]))
                    {
                        paths[i, j] = j;
                        costs[i, j] = 1;
                    }
                    else // default for non-neighbours
                    {
                        paths[i, j] = -1;
                        costs[i, j] = -1; // signifies infinity
                    }
                }

                // reflexive case
                paths[i, i] = i;
                costs[i, i] = 0;
            }

            // Floyd-Warshall algorithm
            for (int link = 0; link < stations.Length; ++link)
            {
                for (int start = 0; start < stations.Length; ++start)
                {
                    for (int end = 0; end < stations.Length; ++end)
                    {
                        if (costs[start, link] > -1 && costs[link, end] > -1 // paths start->link and link->end exist
                            && (costs[start, end] == -1 || costs[start, end] > costs[start, link] + costs[link, end]))
                        {
                            costs[start, end] = costs[start, link] + costs[link, end];
                            paths[start, end] = paths[start, link];
                        }
                    }
                }
            }

            return Tuple.Create(paths, costs);
        }

        /// <summary>
        /// Finds a sequence of connected stations that are able to fulfill the recipe's capabilities.
        /// </summary>
        /// <returns>
        /// An array of station identifiers, one for each capability. This array does not include stations
        /// that only transport a resource from one to the next.
        /// </returns>
        private int[] FindStationPath(Recipe recipe, Dictionary<Station, int> identifiers, int[,] connections, int[,] costs)
        {
            var stations = AvailableStations;

            var paths = from station in stations
                        where recipe.RequiredCapabilities[0].IsSatisfied(station.AvailableCapabilities)
                        select new[] { identifiers[station] };

            if (paths.Count() == 0)
                return null;

            for (int i = 1; i < recipe.RequiredCapabilities.Length; ++i)
            {
                paths = (
                         from path in paths
                         from station in stations
                         let id = identifiers[station]
                         let last = path[path.Length - 1]
                         // if station has the next required capability
                         where recipe.RequiredCapabilities[i].IsSatisfied(station.AvailableCapabilities)
                         // and station is reachable from the previous path
                         where connections[last, id] != -1
                         // append station to the path
                         select path.Concat(new[] { id }).ToArray()
                        ).ToArray();

                if (paths.Count() == 0)
                    return null;
            }

            /*
             * What to optimize?
             *
             * number of roles on path (prefer less busy stations):
            Func<int[], int> cost = (path) => path.Sum(id => stations[id].AllocatedRoles.Count);
             *
             * amount of necessary ingredients available (reconfiguration less likely)
            */

            // optimize path length
            Func<int[], int> cost = (path) => path.Zip(path.Skip(1), (from, to) => costs[from, to]).Sum();

            return paths.OrderBy(cost).First();
        }

        /// <summary>
        /// Configures the <see cref="AvailableStations"/> to produce resource for the <paramref name="recipe"/>.
        /// </summary>
        private void ApplyConfiguration(Recipe recipe, int[] path, int[,] connections)
        {
            var stations = AvailableStations;

            Station lastStation = null;
            Role lastRole = null;

            for (int i = 0; i < path.Length; ++i)
            {
                var station = stations[path[i]];
                var role = lastRole;

                if (station != lastStation)
                {
                    if (lastStation != null) // configure connection between stations
                    {
                        var connection = FindConnection(connections, path[i - 1], path[i]).ToArray();
                        // for each station on connection, except lastStation and station:
                        for (int j = 1; j < connection.Length - 1; ++j)
                        {
                            var link = stations[connection[j]];
                            lastRole.PostCondition.Port = link; // connect to previous

                            var linkRole = GetRole(recipe, lastStation, lastRole.PostCondition.State);
                            link.AllocatedRoles.Add(linkRole); // edd empty (transport) role

                            lastStation = link;
                            lastRole = linkRole;
                        }

                        lastRole.PostCondition.Port = station; // finish connection
                    }

                    // configure station itself
                    role = GetRole(recipe, lastStation, lastRole?.PostCondition.State);
                    station.AllocatedRoles.Add(role);
                }

                var capability = recipe.RequiredCapabilities[i];
                role.CapabilitiesToApply.Add(capability);
                role.PostCondition.State.Add(capability);

                lastStation = station;
                lastRole = role;
            }
        }

        private IEnumerable<int> FindConnection(int[,] connections, int from, int to)
        {
            for (int current = from; current != to; current = connections[current, to])
                yield return current;
            yield return to;
        }
    }
}
