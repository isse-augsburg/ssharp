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

namespace SafetySharp.CaseStudies.HeightControl.Modeling.Sensors
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Vehicles;

	/// <summary>
	///   A common base class for sensors that detect vehicles.
	/// </summary>
	public abstract class VehicleDetector : Component
	{
		/// <summary>
		///   Represents a false detection, i.e., a vehicle is detected even though none is present.
		/// </summary>
		public readonly Fault FalseDetection = new TransientFault();

		/// <summary>
		///   Represents a misdetection, i.e., a vehicle does not detect even though it should have been detected.
		/// </summary>
		public readonly Fault Misdetection = new TransientFault();

		/// <summary>
		///   Indicates whether the light barrier detected a vehicle.
		/// </summary>
		public virtual bool IsVehicleDetected => ObserveVehicles(this);

		/// <summary>
		///   Checks whether the <paramref name="detector" /> detects any vehicles.
		/// </summary>
		/// <param name="detector">The detector that should observe the vehicles.</param>
		public extern bool ObserveVehicles(VehicleDetector detector);

		/// <summary>
		///   Gets a value indicating whether the detector detects the <paramref name="vehicle" />.
		/// </summary>
		/// <param name="vehicle">The vehicle that should be checked.</param>
		public abstract bool DetectsVehicle(Vehicle vehicle);

		/// <summary>
		///   Represents a false detection, i.e., a vehicle is detected even though none is present.
		/// </summary>
		[FaultEffect(Fault = nameof(FalseDetection)), Priority(0)]
		public abstract class FalseDetectionEffect : VehicleDetector
		{
			public override bool IsVehicleDetected => true;
		}

		/// <summary>
		///   Represents a misdetection, i.e., a vehicle does not detect even though it should have been detected.
		/// </summary>
		[FaultEffect(Fault = nameof(Misdetection)), Priority(1)]
		public abstract class MisdetectionEffect : VehicleDetector
		{
			public override bool IsVehicleDetected => false;
		}
	}
}