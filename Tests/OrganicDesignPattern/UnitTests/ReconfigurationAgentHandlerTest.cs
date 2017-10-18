namespace Tests.OrganicDesignPattern.UnitTests
{
	using System;
	using Xunit;
	using NSubstitute;
	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;
	using Shouldly;

	public class ReconfigurationAgentHandlerTest
	{
		[Fact]
		public void TestReconfigure()
		{
			// arrange
			var reconfAgent = Substitute.For<IReconfigurationAgent>();
			var createReconfAgent = Substitute.For<Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent>>();
			createReconfAgent.Invoke(null, null, null).ReturnsForAnyArgs(reconfAgent);

			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();
			var state = Substitute.For<BaseAgent.State>(agent, null, false, new InvariantPredicate[0]);

			var handler = new ReconfigurationAgentHandler(agent, createReconfAgent);
			var reconfs = new[] { Tuple.Create(task, state) };

			// act
			var firstReconf = handler.Reconfigure(reconfs);
			var secondReconf = handler.Reconfigure(reconfs);

			// assert
			firstReconf.IsCompleted.ShouldBeFalse();
			secondReconf.IsCompleted.ShouldBeFalse();
			createReconfAgent.Received(1).Invoke(agent, handler, task);
			reconfAgent.Received(2).StartReconfiguration(task, agent, state);

			// act
			handler.Done(task);

			// assert
			firstReconf.IsCompleted.ShouldBeTrue();
			secondReconf.IsCompleted.ShouldBeTrue();
		}
	}
}
