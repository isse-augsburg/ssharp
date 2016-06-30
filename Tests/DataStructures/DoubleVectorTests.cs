using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataStructures
{
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class DoubleVectorTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		public DoubleVectorTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			var vec = new DoubleVector();
			vec[0] = 1.0;
			vec[7] = 2.0;
			vec[0].ShouldBe(1.0);
			vec[7].ShouldBe(2.0);
			vec.Count.ShouldBe(8);
			var sum = 0.0;
			for (int i = 0; i < vec.Count; i++)
			{
				sum += vec[i];
			}
			sum.ShouldBe(3.0);
			vec[1] = 3.0;
			vec[2] = 4.0;
			vec[4] = 5.0;
			vec[3] = 6.0;
			vec[6] = 7.0;
			vec[5] = 8.0;
			vec[0].ShouldBe(1.0);
			vec[1].ShouldBe(3.0);
			vec[2].ShouldBe(4.0);
			vec[3].ShouldBe(6.0);
			vec[4].ShouldBe(5.0);
			vec[5].ShouldBe(8.0);
			vec[6].ShouldBe(7.0);
			vec[7].ShouldBe(2.0);
			sum = 0.0;
			for (int i = 0; i < vec.Count; i++)
			{
				sum += vec[i];
			}
			sum.ShouldBe(36.0);
		}
	}
}
