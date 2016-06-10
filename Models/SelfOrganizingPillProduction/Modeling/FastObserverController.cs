using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// An <see cref="ObserverController"/> implementation that is much faster than
    /// the MiniZinc implementation.
    /// </summary>
    class FastObserverController : ObserverController
    {
        public FastObserverController(params Station[] stations) : base(stations) { }

        [Hidden]
        private Station[] availableStations;

        [Hidden(HideElements = true)]
        private readonly Dictionary<Station, int> identifiers = new Dictionary<Station, int>();

        [Hidden]
        private int[,] pathMatrix;

        [Hidden]
        private int[,] costMatrix;

        public override void Configure(Recipe recipe)
        {
            Configure(new[] { recipe });
        }

        public override void Configure(IEnumerable<Recipe> recipes)
        {
            availableStations = AvailableStations;

            // assign int identifiers to stations
            identifiers.Clear();
            for (int i = 0; i < availableStations.Length; ++i)
                identifiers[availableStations[i]] = i;

            CalculateShortestPaths();

            foreach (var recipe in recipes)
                ConfigureInternal(recipe);
        }

        private void ConfigureInternal(Recipe recipe)
        {
            RemoveObsoleteConfiguration(recipe);

            // find optimal path that satisfies the required capabilities
            var path = FindStationPath(recipe);
            if (path == null)
                Unsatisfiable = true;
            else
                ApplyConfiguration(recipe, path);
        }

        /// <summary>
        /// Calculates the connection matrix for the available stations.
        /// </summary>
        /// <returns>A tuple containing the successor matrix and the path length matrix. -1 indicates no successor / infinite costs.</returns>
        private void CalculateShortestPaths()
        {
            pathMatrix = new int[availableStations.Length, availableStations.Length];
            costMatrix = new int[availableStations.Length, availableStations.Length];

            for (int i = 0; i < availableStations.Length; ++i)
            {
                for (int j = 0; j < availableStations.Length; ++j)
                {
                    // neighbours
                    if (availableStations[i].Outputs.Contains(availableStations[j]))
                    {
                        pathMatrix[i, j] = j;
                        costMatrix[i, j] = 1;
                    }
                    else // default for non-neighbours
                    {
                        pathMatrix[i, j] = -1; // signifies no path
                        costMatrix[i, j] = -1; // signifies infinity
                    }
                }

                // reflexive case
                pathMatrix[i, i] = i;
                costMatrix[i, i] = 0;
            }

            // Floyd-Warshall algorithm
            for (int link = 0; link < availableStations.Length; ++link)
            {
                for (int start = 0; start < availableStations.Length; ++start)
                {
                    for (int end = 0; end < availableStations.Length; ++end)
                    {
                        if (costMatrix[start, link] > -1 && costMatrix[link, end] > -1 // paths start->link and link->end exist
                            && (costMatrix[start, end] == -1 || costMatrix[start, end] > costMatrix[start, link] + costMatrix[link, end]))
                        {
                            costMatrix[start, end] = costMatrix[start, link] + costMatrix[link, end];
                            pathMatrix[start, end] = pathMatrix[start, link];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds a sequence of connected stations that are able to fulfill the recipe's capabilities.
        /// </summary>
        /// <returns>
        /// An array of station identifiers, one for each capability. This array does not include stations
        /// that only transport a resource from one to the next.
        /// </returns>
        private int[] FindStationPath(Recipe recipe)
        {
            var paths = from station in availableStations
                        where recipe.RequiredCapabilities[0].IsSatisfied(station.AvailableCapabilities)
                        select new[] { identifiers[station] };

            if (paths.Count() == 0)
                return null;

            for (int i = 1; i < recipe.RequiredCapabilities.Length; ++i)
            {
                paths = (
                         from path in paths
                         from station in availableStations
                         let id = identifiers[station]
                         let last = path[path.Length - 1]
                         // if station has the next required capability
                         where recipe.RequiredCapabilities[i].IsSatisfied(station.AvailableCapabilities)
                         // and station is reachable from the previous path
                         where pathMatrix[last, id] != -1
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
            Func<int[], int> cost = (path) => path.Zip(path.Skip(1), (from, to) => costMatrix[from, to]).Sum();

            return paths.OrderBy(cost).First();
        }

        /// <summary>
        /// Configures the <see cref="AvailableStations"/> to produce resource for the <paramref name="recipe"/>.
        /// </summary>
        private void ApplyConfiguration(Recipe recipe, int[] path)
        {
            Station lastStation = null;
            Role lastRole = null;

            for (int i = 0; i < path.Length; ++i)
            {
                var station = availableStations[path[i]];
                var role = lastRole;

                if (station != lastStation)
                {
                    if (lastStation != null) // configure connection between stations
                    {
                        var connection = GetShortestPath(path[i - 1], path[i]).ToArray();
                        // for each station on connection, except lastStation and station:
                        for (int j = 1; j < connection.Length - 1; ++j)
                        {
                            var link = availableStations[connection[j]];
                            lastRole.PostCondition.Port = link; // connect to previous

                            var linkRole = GetRole(recipe, lastStation, lastRole.PostCondition.State);
                            link.AllocatedRoles.Add(linkRole); // add empty (transport) role

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

        private IEnumerable<int> GetShortestPath(int from, int to)
        {
            for (int current = from; current != to; current = pathMatrix[current, to])
                yield return current;
            yield return to;
        }
    }
}
