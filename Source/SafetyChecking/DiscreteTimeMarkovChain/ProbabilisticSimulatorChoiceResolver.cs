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
	using System.Runtime.CompilerServices;
	using ExecutableModel;
	using Modeling;
	using Utilities;

	internal class ProbabilisticSimulatorChoiceResolver : ChoiceResolver
	{
		private readonly List<int> _choices = new List<int>();
		private readonly List<Probability> _probabilities = new List<Probability>();

		private bool _useProbabilityOfChoice;

		private int _choiceIndex = -1;

		private readonly Random _random;

		private Probability GetProbabilityOfPreviousPath()
		{
			if (_choiceIndex == -1 || _choiceIndex == 0)
				return Probability.One;
			return _probabilities[_choiceIndex - 1];
		}

		public ProbabilisticSimulatorChoiceResolver(bool useProbabilityOfChoice=true, int seed = 0)
				: base(false)
		{
			_useProbabilityOfChoice = useProbabilityOfChoice;
			_random =new Random(seed);
		}

		internal override int LastChoiceIndex => _choices[_choices.Count - 1];

		public override int HandleChoice(int valueCount)
		{
			++_choiceIndex;
			
			var randomIndex = _random.Next(valueCount);

			_choices.Add(randomIndex);
			_probabilities.Add(GetProbabilityOfPreviousPath() / valueCount);

			return randomIndex;
		}

		/// <summary>
		///   Handles a probabilistic choice that chooses between two options.
		/// </summary>
		/// <param name="p0">The probability of option 0.</param>
		/// <param name="p1">The probability of option 1.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int HandleProbabilisticChoice(Probability p0, Probability p1)
		{
			return HandleProbabilisticChoice(new [] { p0, p1 });
		}

		/// <summary>
		///   Handles a probabilistic choice that chooses between three options.
		/// </summary>
		/// <param name="p0">The probability of option 0.</param>
		/// <param name="p1">The probability of option 1.</param>
		/// <param name="p2">The probability of option 2.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int HandleProbabilisticChoice(Probability p0, Probability p1, Probability p2)
		{
			return HandleProbabilisticChoice(new[] { p0, p1, p2 });
		}

		/// <summary>
		///   Handles a probabilistic choice that chooses between different options.
		/// </summary>
		/// <param name="p">Array with probabilities of each option.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int HandleProbabilisticChoice(params Probability[] p)
		{
			++_choiceIndex;

			int selectedOption;

			if (_useProbabilityOfChoice)
			{
				var randomP = _random.NextDouble();
				selectedOption = 0;
				var currentP = 0.0;
				for (var i = 0; i < p.Length; i++)
				{
					var nextP = currentP + p[i].Value;
					if (randomP <= nextP)
					{
						selectedOption = i;
						break;
					}
					currentP = nextP;
				}
			}
			else
			{
				selectedOption = _random.Next(p.Length);
			}

			var probability = p[selectedOption];
			_choices.Add(selectedOption);
			_probabilities.Add(GetProbabilityOfPreviousPath() * probability);

			return selectedOption;
		}

		public override bool PrepareNextPath()
		{
			return false;
		}

		public override void PrepareNextState()
		{
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
