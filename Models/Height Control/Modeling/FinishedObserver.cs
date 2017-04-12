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


namespace SafetySharp.CaseStudies.HeightControl.Modeling
{
	using SafetySharp.Modeling;
	using Vehicles;

	public abstract class FinishedObserver : Component
	{
		public abstract bool Finished { get; }

	}

	public class FinishedObserverDisabled : FinishedObserver
	{
		public override bool Finished => false;
	}
	public class FinishedObserverVehiclesAtEnd : FinishedObserver
	{

		[Hidden]
		private bool _finished;

		/// <summary>
		///   The vehicles contained in the set.
		/// </summary>
		[Hidden(HideElements = true)]
		private Vehicle[] Vehicles { get; }

		public override bool Finished => _finished;

		public FinishedObserverVehiclesAtEnd(Vehicle[] vehicles)
		{
			Vehicles = vehicles;
		}


		public override void Update()
		{
			var oneVehicleNotAtEnd = false;
			var vehicleId = 0;
			while (!oneVehicleNotAtEnd && vehicleId < Vehicles.Length)
			{
				if (Vehicles[vehicleId].Position < Model.TunnelPosition)
					oneVehicleNotAtEnd = true;
				vehicleId++;
			}
			_finished= !oneVehicleNotAtEnd;
		}
	}
}
