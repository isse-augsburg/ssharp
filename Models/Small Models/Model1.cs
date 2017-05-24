using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Small_Models
{
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using SafetySharp.Modeling;


	public class Model1Component : Component
	{
		public Model1Component()
		{
			F1 = new TransientFault();
			F1.ProbabilityOfOccurrence = new Probability(0.3);
		}

		public Fault F1;


		public bool HazardActive = false;

		public override void Update()
		{
		}


		[FaultEffect(Fault = nameof(F1)), Priority(0)]
		public class F1Effect : Model1Component
		{
			public override void Update()
			{
				HazardActive = true;
			}
		}
	}


	public sealed class Model1ModelBase : ModelBase
	{
		[Root(RootKind.Controller)]
		public Model1Component Model1Component { get; } = new Model1Component();
	}


	public class Model1Analysis
	{
		[Test]
		public void CalculateProbability()
		{
			var model = new Model1ModelBase();
			
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Model1Component.HazardActive, 50);
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateDcca()
		{
			var model = new Model1ModelBase();
			
			var analysis = new SafetySharpSafetyAnalysis { Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.Model1Component.HazardActive);
			//result.SaveCounterExamples("counter examples/height control/dcca/collision/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}
	}
}
