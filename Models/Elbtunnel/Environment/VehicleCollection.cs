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

namespace Elbtunnel.Environment
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents a collection of vehicles.
	/// </summary>
	/// <remarks>
	///   TODO: Currently, the collection is hardcoded to 3 vehicles. Replaces this by an array once S# supports arrays.
	/// </remarks>
	public class VehicleCollection : Component
	{
		private readonly IVehicle _vehicle1;
		private readonly IVehicle _vehicle2;
		private readonly IVehicle _vehicle3;


	    private bool _monitorVehiclesNotInConflict = true;

        /// <summary>
        ///   Initializes a new instance.
        /// </summary>
        public VehicleCollection(IVehicle vehicle1, IVehicle vehicle2, IVehicle vehicle3)
		{
			_vehicle1 = vehicle1;
			_vehicle2 = vehicle2;
			_vehicle3 = vehicle3;

			Bind(_vehicle1.RequiredPorts.IsTunnelClosed = ProvidedPorts.CheckIsTunnelClosed);
			Bind(_vehicle2.RequiredPorts.IsTunnelClosed = ProvidedPorts.CheckIsTunnelClosed);
			Bind(_vehicle3.RequiredPorts.IsTunnelClosed = ProvidedPorts.CheckIsTunnelClosed);
            
            Bind(_vehicle1.RequiredPorts.CheckIfPositionIsVacant = ProvidedPorts.CheckIfPositionIsVacant);
            Bind(_vehicle2.RequiredPorts.CheckIfPositionIsVacant = ProvidedPorts.CheckIfPositionIsVacant);
            Bind(_vehicle3.RequiredPorts.CheckIfPositionIsVacant = ProvidedPorts.CheckIfPositionIsVacant);
        }
        
        public bool GetMonitorVehiclesNotInConflict()
        {
            if (_monitorVehiclesNotInConflict)
            {
                _monitorVehiclesNotInConflict =
                    !CheckIfPositionsAreOverlapping(_vehicle1.GetPositionMin(), _vehicle1.GetPositionMax(),
                        _vehicle1.GetLane(),
                        _vehicle2.GetPositionMin(), _vehicle2.GetPositionMax(), _vehicle2.GetLane()) &&
                    !CheckIfPositionsAreOverlapping(_vehicle1.GetPositionMin(), _vehicle1.GetPositionMax(),
                        _vehicle1.GetLane(),
                        _vehicle3.GetPositionMin(), _vehicle3.GetPositionMax(), _vehicle3.GetLane()) &&
                    !CheckIfPositionsAreOverlapping(_vehicle2.GetPositionMin(), _vehicle2.GetPositionMax(),
                        _vehicle2.GetLane(),
                        _vehicle3.GetPositionMin(), _vehicle3.GetPositionMax(), _vehicle3.GetLane());
            }
            return _monitorVehiclesNotInConflict;
        }

        // TODO: Remove once S# supportes port forwardings
        private bool CheckIsTunnelClosed()
		{
			return IsTunnelClosed();
		}

		/// <summary>
		///   Informs the vehicle whether the tunnel is closed.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public extern bool IsTunnelClosed();

        /// <summary>
        ///   Gets the minimal position of the vehicle with the given <paramref name="vehicleIndex" />.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
        public int GetVehiclePositionMin(int vehicleIndex)
        {
            switch (vehicleIndex)
            {
                case 0:
                    return _vehicle1.GetPositionMin();
                case 1:
                    return _vehicle2.GetPositionMin();
                default:
                    return _vehicle3.GetPositionMin();
            }
        }

        /// <summary>
        ///   Gets the maximal of the vehicle with the given <paramref name="vehicleIndex" />.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
        public int GetVehiclePositionMax(int vehicleIndex)
        {
            switch (vehicleIndex)
            {
                case 0:
                    return _vehicle1.GetPositionMax();
                case 1:
                    return _vehicle2.GetPositionMax();
                default:
                    return _vehicle3.GetPositionMax();
            }
        }

        private bool CheckIfPositionsAreOverlapping(int position1Min, int position1Max, Lane lane1,
                                                    int position2Min, int position2Max, Lane lane2)
        {
            return (lane1 == lane2) && (position1Max!=0 && position2Max!=0) &&  !(position1Max < position2Min || position2Max < position1Min);
        }


        /// <summary>
        ///   Checks, if the desired next position of a vehicle is currently vacant and undisturbed.
        /// </summary>
        /// <param name="desiredLane">The desired lane a vehicle wants to occupy.</param>
        /// <param name="desiredPosition">The desired position a vehicle wants to occupy.</param>
        public bool CheckIfPositionIsVacant(Lane desiredLane, int desiredPosition)
        {
            return ( !CheckIfPositionsAreOverlapping(desiredPosition, desiredPosition, desiredLane, 
                        _vehicle1.GetPositionMin(), _vehicle1.GetPositionMax(), _vehicle1.GetLane()) &&
                     !CheckIfPositionsAreOverlapping(desiredPosition, desiredPosition, desiredLane,
                        _vehicle2.GetPositionMin(), _vehicle2.GetPositionMax(), _vehicle2.GetLane()) &&
                     !CheckIfPositionsAreOverlapping(desiredPosition, desiredPosition, desiredLane,
                        _vehicle3.GetPositionMin(), _vehicle3.GetPositionMax(), _vehicle3.GetLane()));
        }



        /// <summary>
        ///   Gets the lane of the vehicle with the given <paramref name="vehicleIndex" />.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
        public Lane GetVehicleLane(int vehicleIndex)
		{
			switch (vehicleIndex)
			{
				case 0:
					return _vehicle1.GetLane();
				case 1:
					return _vehicle2.GetLane();
				default:
					return _vehicle3.GetLane();
			}
		}

		/// <summary>
		///   Gets the speed of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		public VehicleKind GetVehicleKind(int vehicleIndex)
		{
			switch (vehicleIndex)
			{
				case 0:
					return _vehicle1.GetKind();
				case 1:
					return _vehicle2.GetKind();
				default:
					return _vehicle3.GetKind();
			}
		}
    }
}