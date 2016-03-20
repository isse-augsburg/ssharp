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
	public class DesignExploration_CollisionSpofTests
	{
		[TestCase]
		public void DesignHighTubeWithLb()
		{
			var specification = new DesignHighTubeWithLb_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl()
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl()
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl()
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignTolerantMainControl()
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void OriginalDesign()
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			result.SaveCounterExamples("counter examples/elbtunnel collision spof/");

			Console.WriteLine(result);
		}
	}

	[TestFixture]
	public class DesignExploration_CollisionTests
	{
		[TestCase]
		public void DesignHighTubeWithLb()
		{
			var specification = new DesignHighTubeWithLb_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl()
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl()
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl()
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignTolerantMainControl()
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void OriginalDesign()
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 3);
			result.SaveCounterExamples("counter examples/elbtunnel collision/");

			Console.WriteLine(result);
		}
	}

	[TestFixture]
	public class DesignExploration_FalseAlarmTests
	{
		[TestCase]
		public void DesignHighTubeWithLb()
		{
			var specification = new DesignHighTubeWithLb_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm,maxCardinality:3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl()
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl()
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl()
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignTolerantMainControl()
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm, maxCardinality: 3);
			Console.WriteLine(result);
		}

		[TestCase]
		public void OriginalDesign()
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.FalseAlarm, maxCardinality: 3);
			result.SaveCounterExamples("counter examples/elbtunnel falsealarm/");

			Console.WriteLine(result);
		}
	}
}
