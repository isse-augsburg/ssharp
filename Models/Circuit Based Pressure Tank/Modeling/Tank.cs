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

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Modeling
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents the pressure tank that is filled by the system.
	/// </summary>
	public class Tank : Component
	{
		/// <summary>
		///   The current pressure level.
		/// </summary>
		[Range(0, Model.PressureLimit, OverflowBehavior.Clamp)]
		private int _pressureLevel;

		/// <summary>
		///   Gets a value indicating whether the pressure tank has ruptured after exceeding its maximum allowed pressure level.
		/// </summary>
		public bool IsRuptured;
		
		/// <summary>
		///   Gets the current pressure level within the tank.
		/// </summary>
		public int PressureLevel => _pressureLevel;

		/// <summary>
		///   Gets a value indicating whether the pressure tank is currently being filled.
		/// </summary>
		public extern bool IsBeingFilled { get; }

		/// <summary>
		///   Updates the pressure tank's internal state.
		/// </summary>
		public override void Update()
		{
			IsRuptured |= _pressureLevel >= Model.PressureLimit;
			if (!IsRuptured && IsBeingFilled)
				_pressureLevel += 1;
			else
				_pressureLevel = 0;
		}
	}
}