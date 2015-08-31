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
	///   Represents an overheight vehicle that is not allowed to enter the tunnel on the left lane.
	/// </summary>
	public class Vehicle : Component, IVehicle
	{
		/// <summary>
		///   The kind of the vehicle.
		/// </summary>
		private readonly VehicleKind _kind;

		private readonly Choice _choice = new Choice();

		/// <summary>
		///   The current lane of the vehicle.
		/// </summary>
		private Lane _lane;

		/// <summary>
		///   The maximal position of the vehicle in the current step.
		/// </summary>
		[Range(-1, 30, OverflowBehavior.Clamp)]
		private int _positionMax;

		/// <summary>
		///   The minimal position of the vehicle in the current step.
		/// </summary>
		[Range(-1, 30, OverflowBehavior.Clamp)]
		private int _positionMin;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="kind">The kind of the vehicle.</param>
		public Vehicle(VehicleKind kind)
		{
			_kind = kind;

			SetInitialValues(_lane, Lane.Left, Lane.Right);
		}

		/// <summary>
		///   Gets the minimal position of the vehicle in the current step.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public int GetPositionMin()
		{
			return _positionMin;
		}

		/// <summary>
		///   Gets the maximal position of the vehicle in the current step.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public int GetPositionMax()
		{
			return _positionMax;
		}

		/// <summary>
		///   Checks, if the desired next position of a vehicle is currently vacant and undisturbed.
		/// </summary>
		/// <param name="desiredLane">The desired lane a vehicle wants to occupy.</param>
		/// <param name="desiredPosition">The desired position a vehicle wants to occupy.</param>
		public extern bool CheckIfPositionIsVacant(Lane desiredLane, int desiredPosition);

		/// <summary>
		///   Gets the current lane of the vehicle.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public Lane GetLane()
		{
			return _lane;
		}

		/// <summary>
		///   Gets the kind the vehicle.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public VehicleKind GetKind()
		{
			return _kind;
		}

		/// <summary>
		///   Informs the vehicle whether the tunnel is closed.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		public extern bool IsTunnelClosed();

		/// <summary>
		///   Moves the vehicle.
		/// </summary>
		public override void Update()
		{
			if (IsTunnelClosed())
			{
				return;
			}

			// 1 is the assumed minimal speed and 3 the assumed maximal speed a vehicle might have
			if (_positionMax <= 14)
			{
				// road layout forbids changing the lane after position 14.
				_lane = _choice.Choose(Lane.Left, Lane.Right);
			}
			int speed;
			if (_positionMax == 0)
			{
				speed = _choice.Choose(0, 1, 3);
			}
			else
			{
				speed = _choice.Choose(1, 4);
			}

			if (speed > 0)
			{
				_positionMin = _positionMax + 1;
				_positionMax = _positionMax + speed;
			}
			else
			{
				_positionMin = _positionMax;
			}

			if (_positionMax > 20 || _positionMin > 20)
			{
				_positionMin = 20;
				_positionMax = 20;
			}
		}

		/*
        /// <summary>
        ///   Represents a traffic jam
        /// </summary>
	    [Transient]
	    public class TrafficJam : Fault<Vehicle>
	    {
	        public void Update()
	        {
	            Component._speed = 0;
                Component._positionMin=Component._positionMax;
	        }
	    }
        */
	}
}