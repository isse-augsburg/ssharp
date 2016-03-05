using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elbtunnel.DesignExploration
{
	using NUnit.Framework;
	using SafetySharp.Analysis;
	

	[TestFixture]
	public class DesignExploration_Tests
		{
		[TestCase]
		public void DesignHighTubeWithLb_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignHighTubeWithLb_Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignHighTubeWithLb/{modelChecker.Name}");
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignImprovedDetectionOfPreControl/{modelChecker.Name}");
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignRemovedCounterInMainControl/{modelChecker.Name}");
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignRemovedCounterInTolerantMainControl/{modelChecker.Name}");
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignTolerantMainControl/{modelChecker.Name}");
			Console.WriteLine(result);
		}

		[TestCase]
		public void OriginalDesign_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis(Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/OriginalDesign/{modelChecker.Name}");
			result.SaveCounterExamples("counter examples/elbtunnel/");

			Console.WriteLine(result);
		}
	}
}
