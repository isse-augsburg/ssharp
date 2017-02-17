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

	public enum ModelDensity
	{
		Dense, //Transition per State is limited to State
		Medium, //Transition per State is limited to 4096
		Sparse //Transition per State is limited to  50
	}

	public struct ModelSize
	{
		public ByteSize ByteSize { get; }
		public int SizeOfState { get; }
		public int SizeOfTransition { get; }
		public long NumberOfStates { get; }
		public long NumberOfTransitions { get; }

		public static ModelSize CreateModelSizeFromAvailableMemoryDensityStateAndTransitionSize(ModelDensity density, ByteSize availableMemory, int sizeOfState, int sizeOfTransition)
		{
			const long limit = 5L * 1024L * 1024L * 1024L;
			var availableMemoryValue = availableMemory.Value;
			if (availableMemoryValue <= 0 || availableMemoryValue > limit)
				availableMemoryValue = limit;

			// Equation:
			//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = ByteSize
			//     (2) numberOfTransitions = numberOfStates * density
			//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = ByteSize
			//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
			var numberOfStates = 0L;
			var densityInNumbers = 0L;
			switch (density)
			{
				case ModelDensity.Dense:
					// (3) density = numberOfStates
					// => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * numberOfStates = ByteSize
					// => sizeOfTransition * numberOfStates^2 + sizeOfState * numberOfStates - ByteSize = 0
					// => (school) quadratic equation
					// exact: (var numberOfStatesEstimate = (- sizeOfState + (Math.Sqrt(4.0*ByteSize + sizeOfState*sizeOfState)) )/ (2.0*sizeOfTransition);)
					// estimate: 
					var numberOfStatesEstimate = Math.Sqrt(availableMemoryValue/ ((double)sizeOfTransition));
					numberOfStates = Convert.ToInt64(Math.Floor(numberOfStatesEstimate));
					if (numberOfStates < 4)
						numberOfStates = 4;
					densityInNumbers = numberOfStates;
					break;
				case ModelDensity.Medium:
					densityInNumbers = 4096;
					numberOfStates = availableMemoryValue / (sizeOfState + sizeOfTransition * densityInNumbers);
					break;
				case ModelDensity.Sparse:
					densityInNumbers = 50;
					numberOfStates = availableMemoryValue / (sizeOfState + sizeOfTransition * densityInNumbers);
					break;
			}
			var numberOfTransitions = numberOfStates * densityInNumbers;
			

			return new ModelSize(availableMemory,sizeOfState,sizeOfTransition,numberOfStates, numberOfTransitions);
		}


		public static ModelSize CreateModelSizeFromStateNumberDensityStateAndTransitionSize(long numberOfStates,ModelDensity density, int sizeOfState, int sizeOfTransition)
		{
			// Equation:
			//     (1) sizeOfState * numberOfStates + sizeOfTransition * numberOfTransitions = ByteSize
			//     (2) numberOfTransitions = numberOfStates * density
			//  => sizeOfState * numberOfStates + sizeOfTransition * numberOfStates * density = ByteSize
			//  => numberOfStates = ByteSize / (sizeOfState  + sizeOfTransition * density)
			var densityInNumbers = 0L;
			switch (density)
			{
				case ModelDensity.Dense:
					densityInNumbers = numberOfStates;
					break;
				case ModelDensity.Medium:
					densityInNumbers = 4096;
					break;
				case ModelDensity.Sparse:
					densityInNumbers = 50;
					break;
			}
			var numberOfTransitions = numberOfStates * densityInNumbers;

			var availableMemoryValue = numberOfTransitions * sizeOfTransition + numberOfStates * sizeOfState;

			return new ModelSize(new ByteSize(availableMemoryValue), sizeOfState, sizeOfTransition, numberOfStates, numberOfTransitions);
		}

		public static ModelSize CreateTinyModel(int sizeOfState, int sizeOfTransition)
		{
			return CreateModelSizeFromAvailableMemoryDensityStateAndTransitionSize(ModelDensity.Medium, new ByteSize(10L * 1024L), sizeOfState, sizeOfTransition);
		}

		public static ModelSize CreateSmallModel(int sizeOfState, int sizeOfTransition)
		{
			return CreateModelSizeFromAvailableMemoryDensityStateAndTransitionSize(ModelDensity.Medium, new ByteSize(10L * 1024L), sizeOfState, sizeOfTransition);
		}

		public static ModelSize CreateHugeModel(int sizeOfState, int sizeOfTransition)
		{
			return CreateModelSizeFromAvailableMemoryDensityStateAndTransitionSize(ModelDensity.Medium, new ByteSize(5L * 1024L * 1024L * 1024), sizeOfState, sizeOfTransition);
		}


		public ModelSize(ByteSize byteSize, int sizeOfState, int sizeOfTransition, long numberOfStates, long numberOfTransitions)
		{
			ByteSize = byteSize;
			SizeOfState = sizeOfState;
			SizeOfTransition = sizeOfTransition;
			NumberOfStates = numberOfStates;
			NumberOfTransitions = numberOfTransitions;
		}
	}
}