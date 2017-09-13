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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DiscreteTimeMarkovChain
{
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using JetBrains.Annotations;
	using LabeledTransitionMarkovChainExamples;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class MarkovChainFromMarkovChainTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests()
		{
			foreach (var example in AllExamples.Examples)
			{
				yield return new object[] { example };// only one parameter
			}
		}

		public MarkovChainFromMarkovChainTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}
		
		[Theory, MemberData(nameof(DiscoverTests))]
		public void RetraverseMarkovChainWithOldFormulas(LabeledTransitionMarkovChainExample example)
		{
			var markovChainGenerator = new MarkovChainFromMarkovChainGenerator(example.Ltmc);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.UseCompactStateStorage = true;

			markovChainGenerator.AddFormulaToCheck(LabeledTransitionMarkovChainExample.Label1Formula);
			markovChainGenerator.AddFormulaToCheck(LabeledTransitionMarkovChainExample.Label2Formula);

			var newMarkovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			newMarkovChain.ExportToGv(Output.TextWriterAdapter());

			Assert.Equal(example.Ltmc.SourceStates.Count,newMarkovChain.SourceStates.Count);
			Assert.Equal(example.Ltmc.Transitions, newMarkovChain.Transitions);
		}


		[Theory, MemberData(nameof(DiscoverTests))]
		public void RetraverseMarkovChainWithNewFormula(LabeledTransitionMarkovChainExample example)
		{
			var markovChainGenerator = new MarkovChainFromMarkovChainGenerator(example.Ltmc);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.UseCompactStateStorage = true;

			markovChainGenerator.AddFormulaToCheck(example.ExampleFormula2);

			var newMarkovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			newMarkovChain.ExportToGv(Output.TextWriterAdapter());

			Assert.Equal(example.Ltmc.SourceStates.Count, newMarkovChain.SourceStates.Count);
			Assert.Equal(example.Ltmc.Transitions, newMarkovChain.Transitions);
		}
	}
}
