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
		High = 4096, //Average Transitions per State is limited to 4096
		Medium = 16, //Average Transitions per State is limited to 16
		Sparse = 8, //Average Transitions per State is limited to 8
		VerySparse = 1 //Average Transitions per State is limited to 1
	}

	public abstract class ModelCapacity
	{
		protected const long MinCapacity = 1; //1024
		protected const long DefaultStateCapacity = 1 << 24;
		protected const ModelDensityLimit DefaultDensityLimit = ModelDensityLimit.Medium;

		public abstract ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition);
	}
	
	public class ModelCapacityByModelDensity : ModelCapacity
	{
		private long _stateCapacity;

		public int DensityLimit { get; private set; }

		private ModelCapacityByModelDensity()
		{
		}

		public ModelCapacityByModelDensity(long stateCapacity, ModelDensityLimit densityLimit = ModelDensityLimit.Dense)
		{
			StateCapacity = stateCapacity;
			DensityLimit = (int)densityLimit;
		}

		public ModelCapacityByModelDensity(long stateCapacity, int densityLimit)
		{
			Requires.That(densityLimit > 0, "Density must be greater 0");
			StateCapacity = stateCapacity;
			DensityLimit = densityLimit;
		}

		public static readonly ModelCapacityByModelDensity Default = new ModelCapacityByModelDensity
		{
			StateCapacity = DefaultStateCapacity,
			DensityLimit = (int) DefaultDensityLimit
		};

		public static readonly ModelCapacityByModelDensity Tiny = new ModelCapacityByModelDensity
		{
			StateCapacity = 1024,
			DensityLimit = (int) ModelDensityLimit.Dense
		};

		public static readonly ModelCapacityByModelDensity Small = new ModelCapacityByModelDensity
		{
			StateCapacity = 1<<20,
			DensityLimit = (int) ModelDensityLimit.Medium
		};

		public static readonly ModelCapacityByModelDensity Normal = new ModelCapacityByModelDensity
		{
			StateCapacity = 1 << 24,
			DensityLimit = (int)ModelDensityLimit.Medium
		};

		public static readonly ModelCapacityByModelDensity Large = new ModelCapacityByModelDensity
		{
			StateCapacity = 1 << 28,
			DensityLimit = (int) ModelDensityLimit.High
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
			//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = memoryLimit
			//     (2) numberOfTransitions = numberOfStates * density
			//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = memoryLimit
			//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
			long densityInNumbers;
			switch (DensityLimit)
			{
				case (int) ModelDensityLimit.Dense:
					densityInNumbers = StateCapacity;
					break;
				default:
					densityInNumbers = DensityLimit;
					Requires.That(densityInNumbers > 0, "There must be memory for at least one transition per state in average.");
					break;
			}
			var numberOfDistributions = StateCapacity * (long)Math.Ceiling(Math.Sqrt(densityInNumbers));
			var numberOfTransitions = StateCapacity * densityInNumbers;

			return new ModelByteSize(sizeOfState, sizeOfTransition, StateCapacity, numberOfDistributions, numberOfTransitions);
		}
	}

	public class ModelCapacityByModelSize : ModelCapacity
	{
		private long _stateCapacity;

		public long DistributionsCapacity { get; private set; }

		public long TransitionCapacity { get; private set; }

		private ModelCapacityByModelSize()
		{
		}

		public ModelCapacityByModelSize(long stateCapacity, long distributionsCapacity, long transitionCapacity)
		{
			Requires.That(stateCapacity > 0, "must be greater 0");
			Requires.That(distributionsCapacity > 0, "must be greater 0");
			Requires.That(transitionCapacity > 0, "must be greater 0");
			StateCapacity = stateCapacity;
			DistributionsCapacity = distributionsCapacity;
			TransitionCapacity = transitionCapacity;
		}

		public static readonly ModelCapacityByModelSize Default = new ModelCapacityByModelSize
		{
			StateCapacity = DefaultStateCapacity,
			TransitionCapacity = (int)DefaultDensityLimit* DefaultStateCapacity
		};

		public static readonly ModelCapacityByModelSize Tiny = new ModelCapacityByModelSize
		{
			StateCapacity = 1024,
			TransitionCapacity = 1024 * 1024
		};

		public static readonly ModelCapacityByModelSize Small = new ModelCapacityByModelSize
		{
			StateCapacity = 1 << 20,
			TransitionCapacity = 1 << 25
		};

		public static readonly ModelCapacityByModelSize Normal = new ModelCapacityByModelSize
		{
			StateCapacity = 1 << 24,
			TransitionCapacity = 1 << 29
		};

		public static readonly ModelCapacityByModelSize Large = new ModelCapacityByModelSize
		{
			StateCapacity = 1 << 28,
			TransitionCapacity = 1 << 30
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
			return new ModelByteSize(sizeOfState, sizeOfTransition, StateCapacity, DistributionsCapacity, TransitionCapacity);
		}
	}

	public class ModelCapacityByMemorySize : ModelCapacity
	{
		private static readonly ByteSize _defaultMemoryLimit = new ByteSize(10L * 1024L * 1024L);

		public ByteSize TotalMemoryLimit { get; private set; }
		public int DensityLimit { get; private set; }


		private ModelCapacityByMemorySize()
		{
		}

		public ModelCapacityByMemorySize(ByteSize totalMemoryLimit, ModelDensityLimit densityLimit = ModelDensityLimit.Dense)
		{
			TotalMemoryLimit = totalMemoryLimit;
			DensityLimit = (int) densityLimit;
		}

		public ModelCapacityByMemorySize(ByteSize totalMemoryLimit, int densityLimit)
		{
			TotalMemoryLimit = totalMemoryLimit;
			DensityLimit = densityLimit;
		}

		public static readonly ModelCapacityByMemorySize Default = new ModelCapacityByMemorySize
		{
			TotalMemoryLimit = _defaultMemoryLimit,
			DensityLimit = (int) DefaultDensityLimit
		};

		public static readonly ModelCapacityByMemorySize Tiny = new ModelCapacityByMemorySize
		{
			TotalMemoryLimit = new ByteSize(1024L * 1024L),
			DensityLimit = (int) ModelDensityLimit.Sparse
		};

		public static readonly ModelCapacityByMemorySize Small = new ModelCapacityByMemorySize
		{
			TotalMemoryLimit = new ByteSize(16L * 1024L * 1024L),
			DensityLimit = (int) ModelDensityLimit.Sparse
		};

		public static readonly ModelCapacityByMemorySize Normal = new ModelCapacityByMemorySize
		{
			TotalMemoryLimit = new ByteSize(512L * 1024L * 1024L),
			DensityLimit = (int)ModelDensityLimit.High
		};

		public static readonly ModelCapacityByMemorySize Large = new ModelCapacityByMemorySize
		{
			TotalMemoryLimit = new ByteSize(5L * 1024L * 1024L * 1024),
			DensityLimit = (int) ModelDensityLimit.High
		};

		public override ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition)
		{
			const long limit = 5L * 1024L * 1024L * 1024L;
			var availableMemoryValue = TotalMemoryLimit.Value;
			if (availableMemoryValue <= 0 || availableMemoryValue > limit)
				availableMemoryValue = limit;

			if (sizeOfTransition != 0)
			{
				// Equation:
				//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = ByteSize
				//     (2) numberOfTransitions = numberOfStates * density
				//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = ByteSize
				//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
				var numberOfStates = 0L;
				var densityInNumbers = 0L;
				switch (DensityLimit)
				{
					case (int) ModelDensityLimit.Dense:
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
				var numberOfDistributions = numberOfStates * (long)Math.Ceiling(Math.Sqrt(densityInNumbers));
				var numberOfTransitions = numberOfStates * densityInNumbers;

				return new ModelByteSize(sizeOfState, sizeOfTransition, numberOfStates, numberOfDistributions, numberOfTransitions);
			}
			else
			{
				var numberOfStates = 0L;
				long densityInNumbers;
				switch (DensityLimit)
				{
					case (int) ModelDensityLimit.Dense:
						densityInNumbers = numberOfStates;
						break;
					default:
						densityInNumbers = (long)DensityLimit;
						break;
				}
				numberOfStates = availableMemoryValue / sizeOfState;
				var numberOfDistributions = numberOfStates * (long)Math.Ceiling(Math.Sqrt(densityInNumbers));
				var numberOfTransitions = numberOfStates * densityInNumbers;

				return new ModelByteSize(sizeOfState, sizeOfTransition, numberOfStates, numberOfDistributions, numberOfTransitions);
			}
		}

	}

	public class ModelByteSize : ModelCapacity
	{
		public ByteSize TotalMemoryLimit { get; }
		public ByteSize MemoryLimitStates { get; }
		public ByteSize MemoryLimitTransitions { get; }
		public int SizeOfState { get; }
		public int SizeOfTransition { get; }
		public long NumberOfStates { get; }
		public long NumberOfDistributions { get; }
		public long NumberOfTransitions { get; }

		public ModelByteSize(int sizeOfState, int sizeOfTransition, long numberOfStates, long numberOfDistributions, long numberOfTransitions)
		{
			Requires.That(numberOfStates > 0, "At least one state is necessary");
			Requires.That(sizeOfState > 0, "Size of state must be at least 1");
			Requires.That(numberOfDistributions >= numberOfStates + 1, "At least one more distribution than states necessary");
			var memoryLimitForStates = numberOfStates * sizeOfState;
			var memoryLimitForTransitions = numberOfTransitions * sizeOfTransition;
			TotalMemoryLimit = new ByteSize(memoryLimitForStates + memoryLimitForTransitions);
			MemoryLimitStates = new ByteSize(memoryLimitForStates);
			MemoryLimitTransitions = new ByteSize(memoryLimitForTransitions);
			SizeOfState = sizeOfState;
			SizeOfTransition = sizeOfTransition;
			NumberOfStates = numberOfStates;
			NumberOfDistributions = numberOfDistributions;
			NumberOfTransitions = numberOfTransitions;
		}
		
		public override ModelByteSize DeriveModelByteSize(int sizeOfState, int sizeOfTransition)
		{
			return this;
		}
	}
}