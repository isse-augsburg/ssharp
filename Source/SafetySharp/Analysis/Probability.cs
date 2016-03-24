using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Analysis
{
	using System.Globalization;

	public struct Probability
	{
		public double Value;

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

		public bool Between(double minimal, double maximal)
		{
			return (Value >= minimal && Value <= maximal);
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
