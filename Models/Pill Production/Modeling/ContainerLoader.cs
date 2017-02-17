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
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	/// <summary>
	///   A production station that loads containers on the conveyor belt.
	/// </summary>
	public class ContainerLoader : Station
	{
		private static readonly Capability[] _produceCapabilities = new[] { new ProduceCapability() };
		private static readonly Capability[] _emptyCapabilities = new Capability[0];

		private readonly ObjectPool<PillContainer> _containerStorage = new ObjectPool<PillContainer>(Model.ContainerStorageSize);
		public readonly Fault NoContainersLeft = new PermanentFault();

		private int _containerCount = Model.ContainerStorageSize;

		public ContainerLoader()
		{
			CompleteStationFailure.Subsumes(NoContainersLeft);
		}

		public override Capability[] AvailableCapabilities =>
			_containerCount > 0 ? _produceCapabilities : _emptyCapabilities;

		protected override void ExecuteRole(Role role)
		{
			// This is only called if the current Container comes from another station.
			// The only valid role is thus an empty one (no CapabilitiesToApply) and represents
			// a simple forwarding to the next station.
			if (role.HasCapabilitiesToApply())
				throw new InvalidOperationException("Unsupported capability configuration in ContainerLoader");
		}

		public override void Update()
		{
			// Handle resource requests if any. This is required to allow forwarding
			// of containers to the next station.
			base.Update();

			// No accepted resource requests and no previous resource,
			// so produce resources instead.
			var role = ChooseProductionRole();
			if (Container == null && role != null)
			{
				var recipe = role?.Recipe;

				// role.capabilitiesToApply will always be { ProduceCapability }
				Container = _containerStorage.Allocate();
				_containerCount--;
				Container.OnLoaded(recipe);
				recipe.AddContainer(Container);

				// assume role.PostCondition.Port != null
				role?.PostCondition.Port.ResourceReady(source: this, condition: role.Value.PostCondition);
			}
		}

		private Role? ChooseProductionRole()
		{
			foreach (var role in AllocatedRoles)
				if (role.PreCondition.Port == null && role.Recipe.RemainingAmount > 0 && role.HasCapabilitiesToApply())
					return role;
			return null;
		}

		[FaultEffect(Fault = nameof(NoContainersLeft))]
		public class NoContainersLeftEffect : ContainerLoader
		{
			public override Capability[] AvailableCapabilities => _emptyCapabilities;
		}

		[FaultEffect(Fault = nameof(CompleteStationFailure))]
		public class CompleteStationFailureEffect : ContainerLoader
		{
			public override bool IsAlive => false;

			public override void Update()
			{
			}
		}
	}
}