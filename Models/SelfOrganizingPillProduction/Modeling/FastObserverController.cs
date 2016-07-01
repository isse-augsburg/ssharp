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
        /// Finds a sequence of connected stations that are able to fulfill the <param name="recipe"/>'s capabilities.
        /// </summary>
        /// <returns>
        /// An array of station identifiers, one for each capability. This array does not include stations
        /// that only transport a resource from one to the next.
        /// </returns>
        private int[] FindStationPath(Recipe recipe)
        {
            int[] path = new int[recipe.RequiredCapabilities.Length];

            for (int first = 0; first < availableStations.Length; ++first)
            {
	            if (Capability.IsSatisfiable(new[] { recipe.RequiredCapabilities[0] }, availableStations[first].AvailableCapabilities))
	            {
		            path[0] = first;
		            if (FindStationPath(recipe, path, 1))
			            return path;
	            }
            }

            return null;
        }

        /// <summary>
        /// Recursively checks if there is a valid path with the given prefix for the recipe.
        /// If so, returns true and <param name="path"/> contains the path. Otherwise, returns false.
        /// </summary>
        private bool FindStationPath(Recipe recipe, int[] path, int prefixLength)
        {
            // termination case: the path is already complete
            if (prefixLength == recipe.RequiredCapabilities.Length)
                return true;

            int last = path[prefixLength - 1];

            // special handling: see if the last station can't do the next capability as well
            if (CanSatisfyNext(recipe, path, prefixLength, last))
            {
	            path[prefixLength] = last;
	            if (FindStationPath(recipe, path, prefixLength + 1))
		            return true;
            }
            else // otherwise check connected stations
            {
                for (int next = 0; next < availableStations.Length; ++next) // go through all stations
                {
                    // if connected to last station and can fulfill next capability
                    if (pathMatrix[last, next] != -1 && CanSatisfyNext(recipe, path, prefixLength, next) && next != last)
                    {
                        path[prefixLength] = next; // try a path over next
                        if (FindStationPath(recipe, path, prefixLength + 1)) // if there is such a path, return true
                            return true;
                    }
                }
            }

            return false; // there is no valid path with the given prefix
        }

        /// <summary>
        /// Checks if the given station can satisfy all the demanded capabilities.
        /// </summary>
        /// <param name="recipe">The recipe for which a path is searched.</param>
        /// <param name="path">The current path.</param>
        /// <param name="prefixLength">The length of the path prefix that should be considered valid.</param>
        /// <param name="station">The station which should be next on the path.</param>
        /// <returns>True if choosing station as next path entry would not exceed its capabilities.</returns>
        private bool CanSatisfyNext(Recipe recipe, int[] path, int prefixLength, int station)
        {
            var capabilities = from index in Enumerable.Range(0, prefixLength + 1)
                               where index == prefixLength || path[index] == station
                               select recipe.RequiredCapabilities[index];
            return Capability.IsSatisfiable(capabilities.ToArray(), availableStations[station].AvailableCapabilities);
        }

        /// <summary>
        /// Configures the <see cref="AvailableStations"/> to produce resource for the <paramref name="recipe"/>.
        /// </summary>
        private void ApplyConfiguration(Recipe recipe, int[] path)
        {
            Station lastStation = null;
            Role lastRole = default(Role);

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

                            var linkRole = GetRole(recipe, lastStation, lastRole.PostCondition);
                            link.AllocatedRoles.Add(linkRole); // add empty (transport) role

                            lastStation = link;
                            lastRole = linkRole;
                        }

                        lastRole.PostCondition.Port = station; // finish connection
                    }

                    // configure station itself
                    role = GetRole(recipe, lastStation, lastStation == null ? (Condition?)null : lastRole.PostCondition);
                    station.AllocatedRoles.Add(role);
                }

                var capability = recipe.RequiredCapabilities[i];
                role.AddCapabilityToApply(capability);
                role.PostCondition.AppendToState(capability);

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
