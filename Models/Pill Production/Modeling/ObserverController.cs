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

namespace SafetySharp.CaseStudies.PillProduction.Modeling
{
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;

	public abstract class ObserverController : Component
	{
		// recipes scheduled for initial configuration
		private readonly List<Recipe> _scheduledRecipes = new List<Recipe>();

		/// <summary>
		///   The list of all stations controlled by this instance.
		/// </summary>
		[Hidden(HideElements = true)]
		protected readonly Station[] Stations;

		/// <summary>
		///   Create a new instance controlling the given <see cref="Stations" />.
		/// </summary>
		protected ObserverController(params Station[] stations)
		{
			this.Stations = stations;
		}

		/// <summary>
		///   The list of currently functioning stations controlled by this instance.
		/// </summary>
		protected Station[] AvailableStations => Stations.Where(s => s.IsAlive).ToArray();

		/// <summary>
		///   Set to true if a (re-)configuration is not possible.
		/// </summary>
		public bool Unsatisfiable { get; protected set; }

		/// <summary>
		///   Schedules a <paramref name="recipe" /> for initial configuration once simulation
		///   or model checking starts. Typically called during model setup.
		/// </summary>
		public void ScheduleConfiguration(Recipe recipe)
		{
			_scheduledRecipes.Add(recipe);
		}

		public override void Update()
		{
			if (_scheduledRecipes.Count > 0)
			{
				Configure(_scheduledRecipes);
				_scheduledRecipes.Clear();
			}
		}

		/// <summary>
		///   (Re-)Configures a <paramref name="recipe" />.
		/// </summary>
		public abstract void Configure(Recipe recipe);

		/// <summary>
		///   (Re-)Configures a batch of <paramref name="recipes" />.
		///   The default implementation delegates to <see cref="Configure(Recipe)" />,
		///   but subclasses can override this.
		/// </summary>
		/// <param name="recipes"></param>
		public virtual void Configure(IEnumerable<Recipe> recipes)
		{
			foreach (var recipe in recipes)
				Configure(recipe);
		}

		/// <summary>
		///   Removes all configuration for the given <paramref name="recipe" /> from all stations
		///   under this instance's control.
		/// </summary>
		protected void RemoveObsoleteConfiguration(Recipe recipe)
		{
			foreach (var station in AvailableStations)
			{
				var obsoleteRoles = (from role in station.AllocatedRoles where role.Recipe == recipe select role)
					.ToArray();
				foreach (var role in obsoleteRoles)
					station.AllocatedRoles.Remove(role);

				station.BeforeReconfiguration(recipe);
			}
		}

		/// <summary>
		///   Get a fresh role from the <see cref="RolePool" />.
		/// </summary>
		/// <param name="recipe">The recipe the role will belong to.</param>
		/// <param name="input">The station the role declares as input port. May be null.</param>
		/// <param name="previous">The previous condition (postcondition of the previous role). May be null.</param>
		/// <returns>A <see cref="Role" /> instance with the given data.</returns>
		protected Role GetRole(Recipe recipe, Station input, Condition? previous)
		{
			var role = new Role();

			// update precondition
			role.PreCondition.Recipe = recipe;
			role.PreCondition.Port = input;
			role.PreCondition.ResetState();
			if (previous != null)
				role.PreCondition.CopyStateFrom(previous.Value);

			// update postcondition
			role.PostCondition.Recipe = recipe;
			role.PostCondition.Port = null;
			role.PostCondition.ResetState();
			role.PostCondition.CopyStateFrom(role.PreCondition);

			role.ResetCapabilitiesToApply();

			return role;
		}
	}
}