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


	public class Model2Component : Component
	{
		public Model2Component()
		{
			F1 = new TransientFault();
			F1.ProbabilityOfOccurrence = new Probability(0.003);


			F2 = new TransientFault();
			F2.ProbabilityOfOccurrence = new Probability(0.0001);
		}

		public Fault F1;
		public Fault F2;
		
		public int Step = 0;

		public int Value = 0;

		public bool LoopRequestBug = false;

		public override void Update()
		{
			if (Step > 10)
				return;
			Step++;

			if (Step==1)
				Request();
			if (Step== 5 || LoopRequestBug)
				SetValueTo2();
		}

		public virtual void Request()
		{
		}

		public virtual void SetValueTo2()
		{
			Value = 2;
		}


		[FaultEffect(Fault = nameof(F1)), Priority(0)]
		public class F1Effect : Model2Component
		{
			public override void Request()
			{
				LoopRequestBug = true;
			}
		}

		[FaultEffect(Fault = nameof(F2)), Priority(0)]
		public class F2Effect : Model2Component
		{
			public override void SetValueTo2()
			{
				Value = 3;
			}
		}
	}


	public sealed class Model2ModelBase : ModelBase
	{
		[Root(RootKind.Controller)]
		public Model2Component Model2Component { get; } = new Model2Component();
	}


	public class Model2Analysis
	{
		[Test]
		public void CalculateProbability()
		{
			var model = new Model2ModelBase();
			
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Model2Component.Value==3, 50);
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateDcca()
		{
			var model = new Model2ModelBase();
			
			var analysis = new SafetySharpSafetyAnalysis { Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.Model2Component.Value == 3);
			//result.SaveCounterExamples("counter examples/height control/dcca/collision/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}
	}
}
