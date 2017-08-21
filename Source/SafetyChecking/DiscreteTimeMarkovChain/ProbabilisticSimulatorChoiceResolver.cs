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

using System;
using System.Collections.Generic;

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using ExecutableModel;
	using Modeling;
	using Utilities;

	internal class ProbabilisticSimulatorChoiceResolver : ChoiceResolver
	{
		private readonly List<int> _choices = new List<int>();
		private readonly List<Probability> _probabilities = new List<Probability>();

		private int _choiceIndex = -1;

		private readonly Random _random;

		private Probability GetProbabilityOfPreviousPath()
		{
			if (_choiceIndex == -1 || _choiceIndex == 0)
				return Probability.One;
			return _probabilities[_choiceIndex - 1];
		}

		public ProbabilisticSimulatorChoiceResolver(int seed = 0)
				: base(false)
		{
			_random=new Random(seed);
		}

		internal override int LastChoiceIndex => _choices[_choices.Count - 1];

		public override int HandleChoice(int valueCount)
		{
			++_choiceIndex;

			// TODO: Use probability of choice to resolve the choice
			var randomIndex = _random.Next(valueCount);

			_choices.Add(randomIndex);
			_probabilities.Add(GetProbabilityOfPreviousPath() / valueCount);

			return randomIndex;
		}

		public override int HandleProbabilisticChoice(int valueCount)
		{
			return HandleChoice(valueCount);
		}

		public override bool PrepareNextPath()
		{
			return false;
		}

		public override void PrepareNextState()
		{
		}

		public override void SetProbabilityOfLastChoice(Probability probability)
		{
			Assert.That(_choiceIndex >= 0, "_choiceIndex>=0");
			_probabilities[_choiceIndex] = GetProbabilityOfPreviousPath() * probability;
		}

		/// <summary>
		///	  The probability of the current path
		/// </summary>
		internal Probability CalculateProbabilityOfPath()
		{
			if (_choiceIndex == -1)
				return Probability.One;
			return _probabilities[_choiceIndex];
		}

		protected override void OnDisposing(bool disposing)
		{
		}

		internal override void Clear()
		{
			_choices.Clear();
			_choiceIndex = -1;
			_probabilities.Clear();

		}

		internal override void ForwardUntakenChoicesAtIndex(int choiceIndexToForward)
		{
			throw new Exception("Should never be called during probabilistic simulation");
		}

		internal override IEnumerable<int> GetChoices()
		{
			return _choices;
		}

		internal override void SetChoices(int[] choices)
		{
			throw new Exception("Should never be called during probabilistic simulation");
		}
	}
}
