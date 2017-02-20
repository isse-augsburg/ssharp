// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.ExecutedModel
{
	using System;
	using Utilities;

	public struct ByteSize
	{
		public long Value;

		public ByteSize(long value)
		{
			Value = value;
		}

		public static ByteSize KibiByte = new ByteSize(1024L);
		public static ByteSize MebiByte = new ByteSize(1024L * 1024L);
		public static ByteSize GibiByte = new ByteSize(1024L * 1024L * 1024L);
	}
	
	public enum ModelDensityLimit
	{
		Dense = -1, //Transitions per State is limited to State
		High = 16384, //Average Transitions per State is limited to 16384
		Medium = 4096, //Average Transitions per State is limited to 4096
		Sparse = 64, //Average Transitions per State is limited to 64
		VerySparse = 16 //Average Transitions per State is limited to 16
	}

	public abstract class ModelCapacity
	{
		protected const long MinCapacity = 1024;
		protected const long DefaultStateCapacity = 1 << 24;
		protected const ModelDensityLimit DefaultDensityLimit = ModelDensityLimit.High;

		public abstract ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition);
	}
	
	public class ModelCapacityByModelSize : ModelCapacity
	{
		private long _stateCapacity;

		public ModelDensityLimit DensityLimit { get; set; }

		private ModelCapacityByModelSize()
		{
		}

		public ModelCapacityByModelSize(long stateCapacity, ModelDensityLimit densityLimit = ModelDensityLimit.Dense)
		{
			StateCapacity = stateCapacity;
			DensityLimit = densityLimit;
		}

		public static readonly ModelCapacityByModelSize Default = new ModelCapacityByModelSize
		{
			StateCapacity = DefaultStateCapacity,
			DensityLimit = DefaultDensityLimit
		};

		public static readonly ModelCapacityByModelSize Tiny = new ModelCapacityByModelSize
		{
			StateCapacity = 1024,
			DensityLimit = ModelDensityLimit.Dense
		};

		public static readonly ModelCapacityByModelSize Small = new ModelCapacityByModelSize
		{
			StateCapacity = 1<<20,
			DensityLimit = ModelDensityLimit.Medium
		};

		public static readonly ModelCapacityByModelSize Big = new ModelCapacityByModelSize
		{
			StateCapacity = 1 << 24,
			DensityLimit = ModelDensityLimit.High
		};

		public static readonly ModelCapacityByModelSize Large = new ModelCapacityByModelSize
		{
			StateCapacity = 1 << 28,
			DensityLimit = ModelDensityLimit.High
		};
		
		public long StateCapacity
		{
			get { return Math.Max(_stateCapacity, MinCapacity); }
			private set
			{
				Requires.That(value >= MinCapacity, $"{nameof(StateCapacity)} must be at least {MinCapacity}.");
				_stateCapacity = value;
			}
		}
		
		public override ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition)
		{
			// Equation:
			//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = ByteSize
			//     (2) numberOfTransitions = numberOfStates * density
			//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = ByteSize
			//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
			var densityInNumbers = 0L;
			switch (DensityLimit)
			{
				case ModelDensityLimit.Dense:
					densityInNumbers = StateCapacity;
					break;
				case ModelDensityLimit.Medium:
					densityInNumbers = 4096;
					break;
				case ModelDensityLimit.Sparse:
					densityInNumbers = 50;
					break;
			}
			var numberOfTransitions = StateCapacity * densityInNumbers;

			var availableMemoryValue = numberOfTransitions * sizeOfTransition + StateCapacity * sizeOfState;

			return new ModelByteSize(new ByteSize(availableMemoryValue), sizeOfState, sizeOfTransition, StateCapacity, numberOfTransitions);
		}
	}

	public class ModelCapacityByMemorySize : ModelCapacity
	{
		private static readonly ByteSize _defaultMemoryLimit = new ByteSize(10L * 1024L * 1024L);

		public ByteSize MemoryLimit { get; set; }
		public ModelDensityLimit DensityLimit { get; set; }


		private ModelCapacityByMemorySize()
		{
		}

		public ModelCapacityByMemorySize(ByteSize memoryLimit, ModelDensityLimit densityLimit = ModelDensityLimit.Dense)
		{
			MemoryLimit = memoryLimit;
			DensityLimit = densityLimit;
		}

		public static readonly ModelCapacityByMemorySize Default = new ModelCapacityByMemorySize
		{
			MemoryLimit = _defaultMemoryLimit,
			DensityLimit = DefaultDensityLimit
		};

		public static readonly ModelCapacityByMemorySize Tiny = new ModelCapacityByMemorySize
		{
			MemoryLimit = new ByteSize(1024L * 1024L),
			DensityLimit = ModelDensityLimit.Sparse
		};

		public static readonly ModelCapacityByMemorySize Small = new ModelCapacityByMemorySize
		{
			MemoryLimit = new ByteSize(16L * 1024L * 1024L),
			DensityLimit = ModelDensityLimit.Medium
		};

		public static readonly ModelCapacityByMemorySize Big = new ModelCapacityByMemorySize
		{
			MemoryLimit = new ByteSize(512L * 1024L * 1024L),
			DensityLimit = ModelDensityLimit.High
		};

		public static readonly ModelCapacityByMemorySize Large = new ModelCapacityByMemorySize
		{
			MemoryLimit = new ByteSize(5L * 1024L * 1024L * 1024),
			DensityLimit = ModelDensityLimit.High
		};

		public override ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition)
		{
			const long limit = 5L * 1024L * 1024L * 1024L;
			var availableMemoryValue = MemoryLimit.Value;
			if (availableMemoryValue <= 0 || availableMemoryValue > limit)
				availableMemoryValue = limit;

			// Equation:
			//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = ByteSize
			//     (2) numberOfTransitions = numberOfStates * density
			//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = ByteSize
			//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
			var numberOfStates = 0L;
			var densityInNumbers = 0L;
			switch (DensityLimit)
			{
				case ModelDensityLimit.Dense:
					// (3) density = numberOfStates
					// => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * numberOfStates = ByteSize
					// => sizeOfTransition * numberOfStates^2 + sizeOfState * numberOfStates - ByteSize = 0
					// => (school) quadratic equation
					// exact: (var numberOfStatesEstimate = (- sizeOfState + (Math.Sqrt(4.0*ByteSize + sizeOfState*sizeOfState)) )/ (2.0*sizeOfTransition);)
					// estimate: 
					var numberOfStatesEstimate = Math.Sqrt(availableMemoryValue / ((double)sizeOfTransition));
					numberOfStates = Convert.ToInt64(Math.Floor(numberOfStatesEstimate));
					if (numberOfStates < 4)
						numberOfStates = 4;
					densityInNumbers = numberOfStates;
					break;
				default:
					densityInNumbers = (long)DensityLimit;
					Requires.That(densityInNumbers > 0, "There must be memory for at least one transition per state in average.");
					numberOfStates = availableMemoryValue / (sizeOfState + sizeOfTransition * densityInNumbers);
					break;
			}
			var numberOfTransitions = numberOfStates * densityInNumbers;


			return new ModelByteSize(MemoryLimit, sizeOfState, sizeOfTransition, numberOfStates, numberOfTransitions);
		}

	}

	public class ModelByteSize : ModelCapacity
	{
		public ByteSize ByteSize { get; set; }
		public int SizeOfState { get; set; }
		public int SizeOfTransition { get; set; }
		public long NumberOfStates { get; set; }
		public long NumberOfTransitions { get; set; }
		
		public ModelByteSize(ByteSize byteSize, int sizeOfState, int sizeOfTransition, long numberOfStates, long numberOfTransitions)
		{
			ByteSize = byteSize;
			SizeOfState = sizeOfState;
			SizeOfTransition = sizeOfTransition;
			NumberOfStates = numberOfStates;
			NumberOfTransitions = numberOfTransitions;
		}
		
		public override ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition)
		{
			return this;
		}
	}
}