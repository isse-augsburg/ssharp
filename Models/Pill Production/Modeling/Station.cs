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
	using Odp;

	using IReconfigurationStrategy = Odp.IReconfigurationStrategy<Station, Recipe, PillContainer>;

	/// <summary>
	///   A production station that modifies containers.
	/// </summary>
	public abstract class Station : BaseAgent<Station, Recipe, PillContainer>
	{
		public readonly Fault CompleteStationFailure = new PermanentFault();

		private static int _instanceCounter;
		protected readonly string Name;

		protected Station() : base()
		{
			Name = $"Station#{++_instanceCounter}";
			FaultHelper.PrefixFaultNames(this, Name);
		}

		[Hidden]
		internal Queue<Recipe> RecipeQueue { get; set; }

		public override void Update()
		{
			if (RecipeQueue.Count > 0)
				PerformReconfiguration(new[] {
					Tuple.Create(RecipeQueue.Dequeue(), new ReconfigurationReason<Station, Recipe, PillContainer>(null, null))
				});

			base.Update();
		}

		/// <summary>
		///   The resource currently located at the station.
		/// </summary>
		public PillContainer Container { // TODO: remove?
			get { return Resource; }
			protected set { Resource = value; }
		}

		protected override void DropResource()
		{
			Container.Recipe.DropContainer(Container);
			base.DropResource();
		}

		protected override InvariantPredicate[] MonitoringPredicates => new[] {
			Invariant.IOConsistency,
			Invariant.NeighborsAliveGuarantee,
			// Invariant.ResourceConsistency,

			// custom version of capability consistency due to ingredient amounts
			Invariant.RoleInvariant(role => role.CapabilitiesToApply.ToArray().IsSatisfiable(AvailableCapabilities))
		};

		/// <summary>
		///   Removes all configuration related to a recipe and propagates
		///   this change to neighbouring stations.
		/// </summary>
		/// <param name="recipe"></param>
		protected void RemoveRecipeConfigurations(Recipe recipe)
		{
			var obsoleteRoles = (from role in AllocatedRoles where role.Task == recipe select role)
				.ToArray(); // collect roles before underlying collection is modified
			var affectedNeighbours = (from role in obsoleteRoles select role.PreCondition.Port)
				.Concat(from role in obsoleteRoles select role.PostCondition.Port)
				.Distinct()
				.Where(neighbour => neighbour != null);

			foreach (var role in obsoleteRoles)
				AllocatedRoles.Remove(role);

			foreach (var neighbour in affectedNeighbours)
				neighbour.RemoveRecipeConfigurations(recipe);
		}

		/*[FaultEffect(Fault = nameof(CompleteStationFailure))]
		public abstract class CompleteStationFailureEffect : Station
		{
			public override bool IsAlive => false;

			public override void Update() { } // do not act
		}*/
		// S# seems not to (entirely) support abstract fault effects,
		// thus this is duplicated in each concrete subclass.
	}
}