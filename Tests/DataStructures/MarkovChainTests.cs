using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataStructures
{
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class MarkovChainTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		private MarkovChain _markovChain;

		private void CreateExemplaryMarkovChain()
		{
			//_matrix = new SparseDoubleMatrix(6, 20);
			_markovChain = new MarkovChain();
			_markovChain.StateFormulaLabels = new string[] { };
			_markovChain.StateRewardRetrieverLabels = new string[] { };
			_markovChain.AddInitialState(8920,Probability.One);
			_markovChain.SetSourceStateOfUpcomingTransitions(4442);
			_markovChain.AddTransition(4442,Probability.One);
			//_markovChain.SetStateLabeling();
			_markovChain.FinishSourceState();
			_markovChain.SetSourceStateOfUpcomingTransitions(8920);
			_markovChain.AddTransition(4442, new Probability(0.6));
			_markovChain.AddTransition(8920, new Probability(0.4));
			//_markovChain.SetStateLabeling();
			_markovChain.FinishSourceState();
			//_markovChain.ProbabilityMatrix.OptimizeAndSeal();
		}

		public MarkovChainTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			CreateExemplaryMarkovChain();
			_markovChain.ProbabilityMatrix.PrintMatrix(Output.Log);
			_markovChain.ValidateStates();
			_markovChain.PrintPathWithStepwiseHighestProbability(10);
			var enumerator = _markovChain.ProbabilityMatrix.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextRow())
			{
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue!=null)
						counter += enumerator.CurrentColumnValue.Value.Value;
					else
						throw new Exception("Entry must not be null");
				}
			}
			Assert.Equal(counter, 2.0);
		}
	}
}
