using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	// Coarse/rough measurement/quantifiers
	// In German: "unbestimmte Mengenangaben"
	enum RoughAmount
	{
		None=0,
		Few=1,
		Half=2,
		Much=3,
		Complete=4
	}
	// none, half, complete, some, few, plenty, empty, much, full
	
	enum QualitativePressure
	{
		NoPressure,
		LowPressure,
		GoodPressure,
		HighPressure
	}
	// analyzed, evaluated
}

