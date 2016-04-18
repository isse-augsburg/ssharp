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

namespace SafetySharp.Modeling
{
	using System;
	using System.Globalization;

	public struct Probability
	{
		public double Value { get; }

		public Probability(double value)
		{
			Value = value;
		}
		public static Probability Zero = new Probability(0);

		public static Probability One = new Probability(1);

		public static Probability operator *(Probability p1, Probability p2)
		{
			return new Probability(p1.Value*p2.Value);
		}

		public static Probability operator +(Probability p1, Probability p2)
		{
			return new Probability(p1.Value + p2.Value);
		}

		public static Probability operator *(Probability p1, int p2)
		{
			return new Probability(p1.Value * p2);
		}
		
		public static Probability operator *(Probability p1, double p2)
		{
			return new Probability(p1.Value * p2);
		}

		public static Probability operator /(Probability p1, Probability p2)
		{
			return new Probability(p1.Value / p2.Value);
		}

		public static Probability operator /(Probability p1, int p2)
		{
			return new Probability(p1.Value / p2);
		}

		public Probability Complement()
		{
			return new Probability(1.0 - Value);
		}

		public bool Between(double minimal, double maximal)
		{
			return (Value >= minimal && Value <= maximal);
		}

		public bool Between(double minimal, double maximal, double tolerance)
		{
			return (Value >= (minimal-tolerance) && Value <= (maximal+tolerance));
		}
		
		public bool Be(double value, double tolerance)
		{
			var minimum = Math.Max(value - tolerance, 0.0);
			var maximum = Math.Min(value + tolerance, 1.0);
			return (Value >= minimum && Value <= maximum);
		}

		/// <summary>
		/// Returns the fully qualified type name of this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> containing a fully qualified type name.
		/// </returns>
		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}
	}

}
