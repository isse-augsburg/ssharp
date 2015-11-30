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

namespace Elbtunnel.Vehicles
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents an overheight vehicle that is not allowed to enter the tunnel on the left lane.
	/// </summary>
	public class Vehicle : Component, IInitializable
	{
		[Hidden]
		private readonly VehicleKind _kind;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="kind">The kind of the vehicle.</param>
		public Vehicle(VehicleKind kind)
		{
			_kind = kind;
		}

		/// <summary>
		///   Gets the current lane of the vehicle.
		/// </summary>
		public Lane Lane { get; private set; }

		/// <summary>
		///   Gets the kind the vehicle.
		/// </summary>
		public VehicleKind Kind => _kind;

		/// <summary>
		///   Gets the current vehicle's position.
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		///   Gets the current vehicle's speed.
		/// </summary>
		public int Speed { get; private set; }

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
		}

		/// <summary>
		///   Moves the vehicle.
		/// </summary>
		public override void Update()
		{
			if (IsTunnelClosed)
				return;

			// The road layout makes lane changes impossible after position 14
			if (Position <= 14)
				Lane = Choose(Lane.Left, Lane.Right);

			// Vehicles are only allowed to stop at the initial position
			if (Position == 0)
				Speed = Choose(0, 1, 3);
			else
				Speed = Choose(1, 3);

			Position += Speed;

			if (Position > 20)
				Position = 20;
		}
	}
}