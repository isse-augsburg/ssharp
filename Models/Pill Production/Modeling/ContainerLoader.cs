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
	using SafetySharp.Modeling;
	using Odp;

	/// <summary>
	///   A production station that loads containers on the conveyor belt.
	/// </summary>
	public class ContainerLoader : Station
	{
		private static readonly ICapability[] _produceCapabilities = new[] { new ProduceCapability() };
		private static readonly ICapability[] _emptyCapabilities = new ICapability[0];

		public override ICapability[] AvailableCapabilities =>
			_containerCount > 0 ? _produceCapabilities : _emptyCapabilities;

		private readonly ObjectPool<PillContainer> _containerStorage = new ObjectPool<PillContainer>(Model.ContainerStorageSize);
		private int _containerCount = Model.ContainerStorageSize;

		public readonly Fault NoContainersLeft = new PermanentFault();

		public ContainerLoader()
		{
			CompleteStationFailure.Subsumes(NoContainersLeft);
		}

		public override void ApplyCapability(ICapability capability)
		{
			if (capability is ProduceCapability)
			{
				Container = _containerStorage.Allocate();
				_containerCount--;

				var recipe = _currentRole.Task;
				Container.OnLoaded(recipe);
				recipe.AddContainer(Container);
			}
			else
				throw new InvalidOperationException();
		}

		[FaultEffect(Fault = nameof(NoContainersLeft))]
		public class NoContainersLeftEffect : ContainerLoader
		{
			public override ICapability[] AvailableCapabilities => _emptyCapabilities;
		}

		[FaultEffect(Fault = nameof(CompleteStationFailure))]
		public class CompleteStationFailureEffect : ContainerLoader
		{
			public override void SayHello(Station agent) { } // do not respond to pings

			public override void Update() { } // do not act
		}
	}
}