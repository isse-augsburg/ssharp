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

namespace SafetySharp.CaseStudies.RailroadCrossing.Modeling.Plants
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents the actual train that is controlled by the train controller.
	/// </summary>
	public class Train : Component
	{
		[Range(0, Model.EndPosition, OverflowBehavior.Clamp)]
		private int _position;

		[Range(0, Model.MaxSpeed, OverflowBehavior.Clamp)]
		private int _speed = Model.MaxSpeed;

		/// <summary>
		///   Gets the train's current position.
		/// </summary>
		public int Position => _position;

		/// <summary>
		///   Gets the train's current speed that affects its position.
		/// </summary>
		public int Speed => _speed;

		/// <summary>
		///   Gets the train's current acceleration that affects its speed.
		/// </summary>
		public extern int Acceleration { get; }

		/// <summary>
		///   Updates the train's speed and position in accordance with its current acceleration.
		/// </summary>
		public override void Update()
		{
			_position += _speed;
			_speed += Acceleration;
		}
	}
}