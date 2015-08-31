// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Elbtunnel.Dcca
{
	using Actuators;
	using Controllers;
	using Environment;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Sensors;

	public class LtsMinModel : Model
	{
		public IVehicleDetector EndDetectorLeft;
		public IVehicleDetector EndLightBarrierRight;
		public IVehicleDetector MainDetectorLeft;
		public IVehicleDetector MainDetectorRight;
		public IVehicleDetector MainLightBarrier;
		public IVehicleDetector PreDetectorLeft;
		public IVehicleDetector PreDetectorRight;

		public IVehicleDetector PreLightBarrier;
		public TrafficLights TrafficLights;
		public Vehicle Vehicle1;
		public Vehicle Vehicle2;
		public Vehicle Vehicle3;
		public VehicleCollection Vehicles;

		public LtsMinModel()
		{
			Vehicle1 = new Vehicle(VehicleKind.OverheightTruck);
			Vehicle2 = new Vehicle(VehicleKind.Truck);
			Vehicle3 = new Vehicle(VehicleKind.OverheightTruck);
			Vehicles = new VehicleCollection(Vehicle1, Vehicle2, Vehicle3);

			TrafficLights = new TrafficLights();

			PreLightBarrier = new LightBarrier(position: 5);
			PreDetectorLeft = new OverheadDetector(Lane.Left, position: 5);
			PreDetectorRight = new OverheadDetector(Lane.Right, position: 5);

			MainLightBarrier = new LightBarrier(position: 10);
			MainDetectorLeft = new OverheadDetector(Lane.Left, position: 10);
			MainDetectorRight = new OverheadDetector(Lane.Right, position: 10);

			EndDetectorLeft = new OverheadDetector(Lane.Left, position: 15);
			EndLightBarrierRight = new SmallLightBarrier(Lane.Right, position: 15);

			Bind(Vehicles.RequiredPorts.IsTunnelClosed = TrafficLights.ProvidedPorts.IsRed);
		}

		

		protected void BindVehicle(VehicleCollection vehicles, IVehicleDetector detector)
		{
			Bind(detector.RequiredPorts.GetVehicleKind = vehicles.ProvidedPorts.GetVehicleKind);
			Bind(detector.RequiredPorts.GetVehiclePositionMin = vehicles.ProvidedPorts.GetVehiclePositionMin);
			Bind(detector.RequiredPorts.GetVehiclePositionMax = vehicles.ProvidedPorts.GetVehiclePositionMax);
			Bind(detector.RequiredPorts.GetVehicleLane = vehicles.ProvidedPorts.GetVehicleLane);
		}
	}

	internal class Design1Original : LtsMinModel
	{
		public PreControlOriginal PreControl;
		public MainControlOriginal MainControl;
		public EndControlOriginal EndControl;
		public HeightControl HeightControl;

		public Design1Original()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);

			PreControl = new PreControlOriginal(PreLightBarrier);
			MainControl = new MainControlOriginal(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			EndControl = new EndControlOriginal(EndDetectorLeft, timeout: 10);
			HeightControl = new HeightControl(PreControl, MainControl, EndControl, TrafficLights);

			AddRootComponents(HeightControl, Vehicles);
		}

		public LtlFormula GetHazard()
		{
            return !TrafficLights.IsRed() &&
				   ((Vehicle1.GetKind() == VehicleKind.OverheightTruck && Vehicle1.GetPositionMax() > 18 && Vehicle1.GetLane() == Lane.Left) ||
					(Vehicle2.GetKind() == VehicleKind.OverheightTruck && Vehicle2.GetPositionMax() > 18 && Vehicle2.GetLane() == Lane.Left) ||
					(Vehicle3.GetKind() == VehicleKind.OverheightTruck && Vehicle3.GetPositionMax() > 18 && Vehicle3.GetLane() == Lane.Left));
		}
	}

	internal class Design2PreImprovedDetection : LtsMinModel
	{
		public Design2PreImprovedDetection()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, PreDetectorLeft);
			BindVehicle(Vehicles, PreDetectorRight);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);

			var preControl = new PreControlImprovedDetection(PreLightBarrier, PreDetectorLeft, PreDetectorRight);
			var mainControl = new MainControlOriginal(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			var endControl = new EndControlOriginal(EndDetectorLeft, timeout: 10);
			var heightControl = new HeightControl(preControl, mainControl, endControl, TrafficLights);

			AddRootComponents(heightControl, Vehicles);
		}
	}

	internal class Design3MainRemovedCounter : LtsMinModel
	{
		public Design3MainRemovedCounter()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);

			var preControl = new PreControlOriginal(PreLightBarrier);
			var mainControl = new MainControlRemovedCounter(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			var endControl = new EndControlOriginal(EndDetectorLeft, timeout: 10);
			var heightControl = new HeightControl(preControl, mainControl, endControl, TrafficLights);

			AddRootComponents(heightControl, Vehicles);
		}
	}

	internal class Design4PostAddLbAtHighTube : LtsMinModel
	{
		public Design4PostAddLbAtHighTube()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);
			BindVehicle(Vehicles, EndLightBarrierRight);

			var preControl = new PreControlOriginal(PreLightBarrier);
			var mainControl = new MainControlOriginal(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			var endControl = new EndControlAdditionalLightBarrier(EndDetectorLeft, EndLightBarrierRight, timeout: 10);
			var heightControl = new HeightControl(preControl, mainControl, endControl, TrafficLights);

			AddRootComponents(heightControl, Vehicles);
		}
	}

	internal class Design5MainTolerant : LtsMinModel
	{
		public Design5MainTolerant()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);

			var preControl = new PreControlOriginal(PreLightBarrier);
			var mainControl = new MainControlTolerant(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			var endControl = new EndControlOriginal(EndDetectorLeft, timeout: 10);
			var heightControl = new HeightControl(preControl, mainControl, endControl, TrafficLights);

			AddRootComponents(heightControl, Vehicles);
		}
	}

	internal class Design6MainRemovedCounterTolerant : LtsMinModel
	{
		public Design6MainRemovedCounterTolerant()
		{
			BindVehicle(Vehicles, PreLightBarrier);
			BindVehicle(Vehicles, MainLightBarrier);
			BindVehicle(Vehicles, MainDetectorLeft);
			BindVehicle(Vehicles, MainDetectorRight);
			BindVehicle(Vehicles, EndDetectorLeft);

			var preControl = new PreControlOriginal(PreLightBarrier);
			var mainControl = new MainControlRemovedCounterTolerant(MainLightBarrier, MainDetectorLeft, MainDetectorRight, timeout: 10);
			var endControl = new EndControlOriginal(EndDetectorLeft, timeout: 10);
			var heightControl = new HeightControl(preControl, mainControl, endControl, TrafficLights);

			AddRootComponents(heightControl, Vehicles);
		}
	}
}