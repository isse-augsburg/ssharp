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

namespace Elbtunnel
{
	using Actuators;
	using Controllers;
	using Environment;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Sensors;

	[TestFixture]
	public class Tests
	{
		[SetUp]
		public void Initialize()
		{
			var lightBarrier1 = new LightBarrier(position: 5);
			var lightBarrier2 = new LightBarrier(position: 10);

			var detectorLeft = new OverheadDetector(Lane.Left, position: 10);
			var detectorRight = new OverheadDetector(Lane.Right, position: 10);
			var detectorFinal = new OverheadDetector(Lane.Left, position: 15);

			var trafficLights = new TrafficLights();

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(lightBarrier1);
			var mainControl = new MainControlOriginal(lightBarrier2, detectorLeft, detectorRight, timeout: 6);
            var endControl = new EndControlOriginal(detectorFinal, timeout: 6);

			var vehicle1 = new Vehicle(VehicleKind.OverheightTruck);
			var vehicle2 = new Vehicle(VehicleKind.Truck);
			var vehicle3 = new Vehicle(VehicleKind.OverheightTruck);

			var heightControl = new HeightControl(preControl, mainControl, endControl, trafficLights);
			var vehicles = new VehicleCollection(vehicle1, vehicle2, vehicle3);

			_model = new Model();
			_model.AddRootComponents(heightControl, vehicles);

			Bind(vehicles, lightBarrier1);
			Bind(vehicles, lightBarrier2);
			Bind(vehicles, detectorLeft);
			Bind(vehicles, detectorRight);
			Bind(vehicles, detectorFinal);


			_model.Bind(vehicles.RequiredPorts.IsTunnelClosed = trafficLights.ProvidedPorts.IsRed);
		    var validState = true;//vehicles.GetMonitorVehiclesNotInConflict();
			var vehicleAboutToCollide = VehicleCollided(vehicle1) | VehicleCollided(vehicle2) | VehicleCollided(vehicle3);
			_hazard = validState & vehicleAboutToCollide & !trafficLights.IsRed();
		}

		private LtlFormula _hazard;
		private Model _model;

		private void Bind(VehicleCollection vehicles, IVehicleDetector detector)
		{
			_model.Bind(detector.RequiredPorts.GetVehicleKind = vehicles.ProvidedPorts.GetVehicleKind);
            _model.Bind(detector.RequiredPorts.GetVehiclePositionMin = vehicles.ProvidedPorts.GetVehiclePositionMin);
            _model.Bind(detector.RequiredPorts.GetVehiclePositionMax = vehicles.ProvidedPorts.GetVehiclePositionMax);
			_model.Bind(detector.RequiredPorts.GetVehicleLane = vehicles.ProvidedPorts.GetVehicleLane);
		}

		private static void Main()
		{
		}

		private LtlFormula VehicleCollided(Vehicle vehicle)
		{
			return vehicle.GetKind() == VehicleKind.OverheightTruck && vehicle.GetPositionMax() > 18 && vehicle.GetLane() == Lane.Left;
		}

		[Test]
		public void DccaSpin()
		{
			var spin = new Spin(_model);
			spin.ComputeMinimalCriticalSets(_hazard);
        }

        [Test]
        public void DccaNuSMV()
        {
            var nuSMV = new NuSMV(_model);
            nuSMV.ComputeMinimalCriticalSets(_hazard);
        }

        [Test]
        public void ProbabilityOfHazard()
        {
            var prism = new Prism(_model);
            prism.ComputeProbability(_hazard);
        }
    }


    [TestFixture]
    public class DesignExploration
    {
        // Vehicles in the Environment

        private Vehicle _vehicle1;
        private Vehicle _vehicle2;
        private Vehicle _vehicle3;
        private VehicleCollection _vehicles;

        private TrafficLights _trafficLights;


        // Sensors
        private IVehicleDetector _preLightBarrier;
        private IVehicleDetector _preDetectorLeft;
        private IVehicleDetector _preDetectorRight;

        private IVehicleDetector _mainLightBarrier;
        private IVehicleDetector _mainDetectorLeft;
        private IVehicleDetector _mainDetectorRight;

        private IVehicleDetector _endDetectorLeft;
        private IVehicleDetector _endLightBarrierRight;

        private Model _model;
        private LtlFormula _hazard;
        
        private LtlFormula VehicleCollided(Vehicle vehicle)
        {
            return vehicle.GetKind() == VehicleKind.OverheightTruck && vehicle.GetPositionMax() > 18 && vehicle.GetLane() == Lane.Left;
        }

        private void BindVehicle(VehicleCollection vehicles, IVehicleDetector detector)
        {
            _model.Bind(detector.RequiredPorts.GetVehicleKind = vehicles.ProvidedPorts.GetVehicleKind);
            _model.Bind(detector.RequiredPorts.GetVehiclePositionMin = vehicles.ProvidedPorts.GetVehiclePositionMin);
            _model.Bind(detector.RequiredPorts.GetVehiclePositionMax = vehicles.ProvidedPorts.GetVehiclePositionMax);
            _model.Bind(detector.RequiredPorts.GetVehicleLane = vehicles.ProvidedPorts.GetVehicleLane);
        }

        [SetUp]
        public void InitializeModelRepository()
        {
            _vehicle1= new Vehicle(VehicleKind.OverheightTruck);
            _vehicle2= new Vehicle(VehicleKind.Truck);
            _vehicle3= new Vehicle(VehicleKind.OverheightTruck);
            _vehicles = new VehicleCollection(_vehicle1, _vehicle2, _vehicle3);


            _trafficLights = new TrafficLights();

            _preLightBarrier = new LightBarrier(position: 5);
            _preDetectorLeft = new OverheadDetector(Lane.Left, position: 5);
            _preDetectorRight = new OverheadDetector(Lane.Right, position: 5);

            _mainLightBarrier = new LightBarrier(position: 10);
            _mainDetectorLeft = new OverheadDetector(Lane.Left, position: 10);
            _mainDetectorRight = new OverheadDetector(Lane.Right, position: 10);

            _endDetectorLeft = new OverheadDetector(Lane.Left, position: 15);
            _endLightBarrierRight = new SmallLightBarrier(Lane.Right, position: 15);

            _model = new Model();

            /*
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _preDetectorLeft);
            BindVehicle(_vehicles, _preDetectorRight);

            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);

            BindVehicle(_vehicles, _endDetectorLeft);
            BindVehicle(_vehicles, _endLightBarrierRight);
            */

            _model.Bind(_vehicles.RequiredPorts.IsTunnelClosed = _trafficLights.ProvidedPorts.IsRed);

            var validState = true; //_vehicles.GetMonitorVehiclesNotInConflict();
            var vehicleAboutToCollide = VehicleCollided(_vehicle1) | VehicleCollided(_vehicle2) | VehicleCollided(_vehicle3);
            _hazard = validState & vehicleAboutToCollide & !_trafficLights.IsRed();
        }


        [Test]
        public void DccaCollision_Design1Original()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(_preLightBarrier);
            var mainControl = new MainControlOriginal(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlOriginal(_endDetectorLeft, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }


        [Test]
        public void DccaCollision_Design2PreImprovedDetection()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _preDetectorLeft);
            BindVehicle(_vehicles, _preDetectorRight);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlImprovedDetection(_preLightBarrier, _preDetectorLeft, _preDetectorRight);
            var mainControl = new MainControlOriginal(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlOriginal(_endDetectorLeft, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }


        [Test]
        public void DccaCollision_Design3MainRemovedCounter()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(_preLightBarrier);
            var mainControl = new MainControlRemovedCounter(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlOriginal(_endDetectorLeft, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }

        [Test]
        public void DccaCollision_Design4PostAddLbAtHighTube()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);
            BindVehicle(_vehicles, _endLightBarrierRight);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(_preLightBarrier);
            var mainControl = new MainControlOriginal(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlAdditionalLightBarrier(_endDetectorLeft, _endLightBarrierRight, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }

        [Test]
        public void DccaCollision_Design5MainTolerant()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(_preLightBarrier);
            var mainControl = new MainControlTolerant(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlOriginal(_endDetectorLeft, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }

        [Test]
        public void DccaCollision_Design6MainRemovedCounterTolerant()
        {
            BindVehicle(_vehicles, _preLightBarrier);
            BindVehicle(_vehicles, _mainLightBarrier);
            BindVehicle(_vehicles, _mainDetectorLeft);
            BindVehicle(_vehicles, _mainDetectorRight);
            BindVehicle(_vehicles, _endDetectorLeft);

            // We set timeout to 6, because the slowest possible time from preControl to mainControl (or mainControl to endControl)
            // is 5. And we add a safety-margin of 1.
            var preControl = new PreControlOriginal(_preLightBarrier);
            var mainControl = new MainControlRemovedCounterTolerant(_mainLightBarrier, _mainDetectorLeft, _mainDetectorRight, timeout: 6);
            var endControl = new EndControlOriginal(_endDetectorLeft, timeout: 6);
            var heightControl = new HeightControl(preControl, mainControl, endControl, _trafficLights);


            _model.AddRootComponents(heightControl, _vehicles);

            var spin = new Spin(_model);
            spin.ComputeMinimalCriticalSets(_hazard);
        }
    }
}