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
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;

	using Condition = Odp.Condition<Station, Recipe>;
	using Role = Odp.Role<Station, Recipe, PillContainer>;

	/// <summary>
	///   An <see cref="ObserverController" /> implementation that is much faster than
	///   the MiniZinc implementation.
	/// </summary>
	internal class FastController : Odp.AbstractController<Station, Recipe, PillContainer>
	{
		[Hidden]
		private Station[] _availableStations;

		[Hidden]
		private int[,] _costMatrix;

		[Hidden]
		private int[,] _pathMatrix;

		public FastController(params Station[] stations) : base(stations) { }

		public override Dictionary<Station, IEnumerable<Role>> CalculateConfigurations(params Recipe[] tasks)
		{
			_availableStations = Agents.Where(station => station.IsAlive).ToArray();
			var configuration = new Dictionary<Station, IEnumerable<Role>>();

			CalculateShortestPaths();

			foreach (var recipe in tasks)
			{
				// find optimal path that satisfies the required capabilities
				var path = FindStationPath(recipe);
				if (path == null)
				{
					ReconfigurationFailure = true;
					continue; // TODO: or break ?
				}

				ExtractConfigurations(configuration, recipe, path);
			}

			return configuration;
		}

		/// <summary>
		///   Calculates the connection matrix for the available stations.
		/// </summary>
		/// <returns>A tuple containing the successor matrix and the path length matrix. -1 indicates no successor / infinite costs.</returns>
		private void CalculateShortestPaths()
		{
			_pathMatrix = new int[_availableStations.Length, _availableStations.Length];
			_costMatrix = new int[_availableStations.Length, _availableStations.Length];

			for (var i = 0; i < _availableStations.Length; ++i)
			{
				for (var j = 0; j < _availableStations.Length; ++j)
				{
					// neighbours
					if (_availableStations[i].Outputs.Contains(_availableStations[j]))
					{
						_pathMatrix[i, j] = j;
						_costMatrix[i, j] = 1;
					}
					else // default for non-neighbours
					{
						_pathMatrix[i, j] = -1; // signifies no path
						_costMatrix[i, j] = -1; // signifies infinity
					}
				}

				// reflexive case
				_pathMatrix[i, i] = i;
				_costMatrix[i, i] = 0;
			}

			// Floyd-Warshall algorithm
			for (var link = 0; link < _availableStations.Length; ++link)
			{
				for (var start = 0; start < _availableStations.Length; ++start)
				{
					for (var end = 0; end < _availableStations.Length; ++end)
					{
						if (_costMatrix[start, link] > -1 && _costMatrix[link, end] > -1 // paths start->link and link->end exist
							&& (_costMatrix[start, end] == -1 || _costMatrix[start, end] > _costMatrix[start, link] + _costMatrix[link, end]))
						{
							_costMatrix[start, end] = _costMatrix[start, link] + _costMatrix[link, end];
							_pathMatrix[start, end] = _pathMatrix[start, link];
						}
					}
				}
			}
		}

		/// <summary>
		///   Finds a sequence of connected stations that are able to fulfill the
		///   <param name="recipe" />
		///   's capabilities.
		/// </summary>
		/// <returns>
		///   An array of station identifiers, one for each capability. This array does not include stations
		///   that only transport a resource from one to the next.
		/// </returns>
		private int[] FindStationPath(Recipe recipe)
		{
			var path = new int[recipe.RequiredCapabilities.Length];

			for (var first = 0; first < _availableStations.Length; ++first)
			{
				var requiredCapabilities = new[] { recipe.RequiredCapabilities[0] };
				if (requiredCapabilities.IsSatisfiable(_availableStations[first].AvailableCapabilities))
				{
					path[0] = first;
					if (FindStationPath(recipe, path, 1))
						return path;
				}
			}

			return null;
		}

		/// <summary>
		///   Recursively checks if there is a valid path with the given prefix for the recipe.
		///   If so, returns true and
		///   <param name="path" />
		///   contains the path. Otherwise, returns false.
		/// </summary>
		private bool FindStationPath(Recipe recipe, int[] path, int prefixLength)
		{
			// termination case: the path is already complete
			if (prefixLength == recipe.RequiredCapabilities.Length)
				return true;

			var last = path[prefixLength - 1];

			// special handling: see if the last station can't do the next capability as well
			if (CanSatisfyNext(recipe, path, prefixLength, last))
			{
				path[prefixLength] = last;
				if (FindStationPath(recipe, path, prefixLength + 1))
					return true;
			}
			else // otherwise check connected stations
			{
				for (int next = 0; next < _availableStations.Length; ++next) // go through all stations
				{
					// if connected to last station and can fulfill next capability
					if (_pathMatrix[last, next] != -1 && CanSatisfyNext(recipe, path, prefixLength, next) && next != last)
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
		///   Checks if the given station can satisfy all the demanded capabilities.
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
			return capabilities.ToArray().IsSatisfiable(_availableStations[station].AvailableCapabilities);
		}

		private void ExtractConfigurations(Dictionary<Station, IEnumerable<Role>> configuration, Recipe recipe, int[] path)
		{
			Station lastStation = null;
			Role lastRole = default(Role);

			Action<Station, Role> addRole = (s, r) => {
				if (!configuration.ContainsKey(s))
					configuration.Add(s, new HashSet<Role>());
				(configuration[s] as HashSet<Role>).Add(r);
			};

			for (var i = 0; i < path.Length; ++i)
			{
				var station = _availableStations[path[i]];
				var role = lastRole;

				if (station != lastStation)
				{
					if (lastStation != null) // configure connection between stations
					{
						var connection = GetShortestPath(path[i - 1], path[i]).ToArray();
						// for each station on connection, except lastStation and station:
						for (var j = 1; j < connection.Length - 1; ++j)
						{
							var link = _availableStations[connection[j]];
							lastRole.PostCondition.Port = link; // connect to previous

							var linkRole = GetRole(recipe, lastStation, lastRole.PostCondition);
							addRole(link, linkRole); // add empty (transport) role

							lastStation = link;
							lastRole = linkRole;
						}

						lastRole.PostCondition.Port = station; // finish connection
					}

					// configuration for station itself
					role = GetRole(recipe, lastStation, lastStation == null ? (Condition?)null : lastRole.PostCondition);
					addRole(station, role);
				}

				var capability = recipe.RequiredCapabilities[i];
				role.AddCapability(capability);
				role.PostCondition.AppendToState(capability);

				lastStation = station;
				lastRole = role;
			}
		}

		private IEnumerable<int> GetShortestPath(int from, int to)
		{
			for (int current = from; current != to; current = _pathMatrix[current, to])
				yield return current;
			yield return to;
		}
	}
}