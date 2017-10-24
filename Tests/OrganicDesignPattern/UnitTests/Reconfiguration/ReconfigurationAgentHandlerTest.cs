// The MIT License (MIT)
//
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Tests.OrganicDesignPattern.UnitTests.Reconfiguration
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

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => Substitute.For<IReconfigurationAgent>());
			var reconfs = new[] { ReconfigurationRequest.Initial(task) };

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

			var handler = new ReconfigurationAgentHandler(agent, createReconfAgent);
			var reconfs = new[] { ReconfigurationRequest.Initial(task) };

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

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => reconfAgent);
			var reconfs = new[] { ReconfigurationRequest.Initial(task) };

			// act - assert
			handler.Reconfigure(reconfs);
			reconfAgent.Received(1).StartReconfiguration(reconfs[0]);

			handler.Reconfigure(reconfs);
			reconfAgent.Received(2).StartReconfiguration(reconfs[0]);
		}

		[Fact]
		public void UpdateAllocatedRolesCallsAcknowledge()
		{
			// arrange
			var reconfAgent = Substitute.For<IReconfigurationAgent>();

			var config = Substitute.For<ConfigurationUpdate>();
			var task = Substitute.For<ITask>();
			var agent = Substitute.For<BaseAgent>();

			var handler = new ReconfigurationAgentHandler(agent, (a, h, t) => reconfAgent);
			var reconfs = new[] { ReconfigurationRequest.Initial(task) };

			// act
			handler.Reconfigure(reconfs);
			handler.UpdateAllocatedRoles(task, config);

			// assert
			reconfAgent.Received().Acknowledge();
		}
	}
}
