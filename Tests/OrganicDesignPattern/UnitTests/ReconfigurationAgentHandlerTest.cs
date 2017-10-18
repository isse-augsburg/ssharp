namespace Tests.OrganicDesignPattern.UnitTests
{
	using System;
	using Task = System.Threading.Tasks.Task;

	using SafetySharp.Odp;
	using SafetySharp.Odp.Reconfiguration;

	using NSubstitute;
	using Shouldly;
	using Xunit;

	public class ReconfigurationAgentHandlerTest
	{
		[Fact]
		public void DoneCompletesAllReconfigurations()
		{
			// arrange
			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();
			var state = Substitute.For<BaseAgent.State>(agent, null, false, new InvariantPredicate[0]);

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => Substitute.For<IReconfigurationAgent>());
			var reconfs = new[] { Tuple.Create(task, state) };

			// act
			var reconf1 = handler.Reconfigure(reconfs);
			var reconf2 = handler.Reconfigure(reconfs);

			// assert
			reconf1.IsCompleted.ShouldBeFalse();
			reconf2.IsCompleted.ShouldBeFalse();

			// act
			handler.Done(task);

			// assert
			reconf1.IsCompleted.ShouldBeTrue();
			reconf2.IsCompleted.ShouldBeTrue();
		}

		[Fact]
		public async Task ReconfigureDoesNotCreateAgentTwice()
		{
			// arrange
			var createReconfAgent = Substitute.For<Func<BaseAgent, ReconfigurationAgentHandler, ITask, IReconfigurationAgent>>();
			createReconfAgent.Invoke(null, null, null)
				.ReturnsForAnyArgs(Substitute.For<IReconfigurationAgent>());

			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();
			var state = Substitute.For<BaseAgent.State>(agent, null, false, new InvariantPredicate[0]);

			var handler = new ReconfigurationAgentHandler(agent, createReconfAgent);
			var reconfs = new[] { Tuple.Create(task, state) };

			// act
			var firstReconf = handler.Reconfigure(reconfs);
			var secondReconf = handler.Reconfigure(reconfs);

			handler.Done(task);

			await firstReconf;
			await secondReconf;

			// assert
			createReconfAgent.ReceivedWithAnyArgs(1).Invoke(null, null, null);
		}

		[Fact]
		public void ReconfigureAlwaysCallsStartReconfiguration()
		{
			// arrange
			var reconfAgent = Substitute.For<IReconfigurationAgent>();

			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();
			var state = Substitute.For<BaseAgent.State>(agent, null, false, new InvariantPredicate[0]);

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => reconfAgent);
			var reconfs = new[] { Tuple.Create(task, state) };

			// act - assert
			handler.Reconfigure(reconfs);
			reconfAgent.Received(1).StartReconfiguration(task, agent, state);

			handler.Reconfigure(reconfs);
			reconfAgent.Received(2).StartReconfiguration(task, agent, state);
		}

		[Fact]
		public void UpdateAllocatedRolesCallsAcknowledge()
		{
			// arrange
			var reconfAgent = Substitute.For<IReconfigurationAgent>();

			var config = Substitute.For<ConfigurationUpdate>();
			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();
			var state = Substitute.For<BaseAgent.State>(agent, null, false, new InvariantPredicate[0]);

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => reconfAgent);
			var reconfs = new[] { Tuple.Create(task, state) };

			// act
			handler.Reconfigure(reconfs);
			handler.UpdateAllocatedRoles(task, config);

			// assert
			reconfAgent.Received().Acknowledge();
		}
	}
}
