using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace HemodialysisMachine.Tests
{
	using Utilities;

	class IntFlowTests
	{
		[Test]
		public void FlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source,direct);
			combinator.ConnectOutWithIn(direct,sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
		}

	}
}
