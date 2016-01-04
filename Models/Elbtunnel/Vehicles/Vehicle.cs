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

namespace Elbtunnel.Vehicles
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents an overheight vehicle that is not allowed to enter the tunnel on the left lane.
	/// </summary>
	public class Vehicle : Component, IInitializable
	{
		[Hidden]
		private VehicleKind _kind;

		[Range(0, Specification.TunnelPosition, OverflowBehavior.Clamp)]
		private int _position;

		[Range(0, Specification.MaxSpeed, OverflowBehavior.Error)]
		private int _speed;

		/// <summary>
		///   Gets the current lane of the vehicle.
		/// </summary>
		public Lane Lane { get; protected set; }

		/// <summary>
		///   Gets the kind the vehicle.
		/// </summary>
		public VehicleKind Kind
		{
			get { return _kind; }
			set { _kind = value; }
		}

		/// <summary>
		///   Gets the current vehicle's position.
		/// </summary>
		public int Position
		{
			get { return _position; }
			protected set { _position = value; }
		}

		/// <summary>
		///   Gets the current vehicle's speed.
		/// </summary>
		public int Speed
		{
			get { return _speed; }
			protected set { _speed = value; }
		}

		/// <summary>
		///   Gets a value indicating whether the vehicle has collided with the tunnel.
		/// </summary>
		public bool IsCollided =>
			Kind == VehicleKind.OverheightTruck && Position >= Specification.TunnelPosition && Lane == Lane.Left;

		/// <summary>
		///   Informs the vehicle whether the tunnel is closed.
		/// </summary>
		public extern bool IsTunnelClosed { get; }

		/// <summary>
		///   Performs the nondeterministic initialization.
		/// </summary>
		void IInitializable.Initialize()
		{
			Lane = Choose(Lane.Left, Lane.Right);
			Speed = ChooseFromRange(0, Specification.MaxSpeed);
		}

		/// <summary>
		///   Moves the vehicle.
		/// </summary>
		public override void Update()
		{
			if (IsTunnelClosed)
				return;

			Position += Speed;

			// Vehicles are only allowed to stop at the initial position
			Speed = ChooseFromRange(1, Specification.MaxSpeed);

			// The road layout makes lane changes impossible when the end control has been reached
			if (Position < Specification.EndControlPosition)
				Lane = Choose(Lane.Left, Lane.Right);
		}
	}
}