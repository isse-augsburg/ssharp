using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ScalableServer.Modeling
{
	public enum QualitativeAmount
	{
		None,
		VeryFew,
		Few,
		Some,
		Many,
	}

	static class QualitativeAmountExtensions
	{
		public static QualitativeAmount Less(this QualitativeAmount request)
		{
			switch (request)
			{
				case QualitativeAmount.None:
					return QualitativeAmount.None;
				case QualitativeAmount.VeryFew:
					return QualitativeAmount.None;
				case QualitativeAmount.Few:
					return QualitativeAmount.VeryFew;
				case QualitativeAmount.Some:
					return QualitativeAmount.Few;
				case QualitativeAmount.Many:
					return QualitativeAmount.Some;
				default:
					throw new ArgumentOutOfRangeException(nameof(request), request, null);
			}
		}
		public static QualitativeAmount More(this QualitativeAmount request)
		{
			switch (request)
			{
				case QualitativeAmount.None:
					return QualitativeAmount.VeryFew;
				case QualitativeAmount.VeryFew:
					return QualitativeAmount.Few;
				case QualitativeAmount.Few:
					return QualitativeAmount.Some;
				case QualitativeAmount.Some:
					return QualitativeAmount.Many;
				case QualitativeAmount.Many:
					return QualitativeAmount.Many;
				default:
					throw new ArgumentOutOfRangeException(nameof(request), request, null);
			}
		}
		public static int Value(this QualitativeAmount request)
		{
			switch (request)
			{
				case QualitativeAmount.None:
					return 0;
				case QualitativeAmount.VeryFew:
					return 1;
				case QualitativeAmount.Few:
					return 5;
				case QualitativeAmount.Some:
					return 20;
				case QualitativeAmount.Many:
					return 100;
				default:
					throw new ArgumentOutOfRangeException(nameof(request), request, null);
			}
		}
	}
}
