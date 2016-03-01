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
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignHighTubeWithLb/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignImprovedDetectionOfPreControl/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignRemovedCounterInMainControl/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignRemovedCounterInTolerantMainControl/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}

		[TestCase]
		public void DesignTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/DesignTolerantMainControl/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}

		[TestCase]
		public void OriginalDesign_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeSinglePointsOfFailures(specification.Collision, $"counter examples/elbtunnel/OriginalDesign/{modelChecker.Name}");
			var percentage = result.CheckedSetsCount / (float)(1 + result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
		}
	}
}
