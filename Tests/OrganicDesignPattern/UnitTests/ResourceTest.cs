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

namespace Tests.OrganicDesignPattern.UnitTests
{
	using System;
	using System.Linq;
	using SafetySharp.Odp;

	using NSubstitute;
	using Shouldly;
	using Xunit;

	public class ResourceTest
	{
		[Fact]
		public void ResourceStateIsUpdated()
		{
			var task = Substitute.For<ITask>();
			var capabilities = new ICapability[] { new ProduceCapability(), new TestCapability(), new ConsumeCapability() };
			task.RequiredCapabilities.Returns(capabilities);

			var resource = new TestResource(task);

			resource.State.ShouldBeEmpty();
			resource.IsComplete.ShouldBeFalse();

			resource.OnCapabilityApplied(capabilities[0]);

			resource.State.ShouldBe(capabilities.Take(1));
			resource.IsComplete.ShouldBeFalse();

			resource.OnCapabilityApplied(capabilities[1]);

			resource.State.ShouldBe(capabilities.Take(2));
			resource.IsComplete.ShouldBeFalse();

			resource.OnCapabilityApplied(capabilities[2]);

			resource.State.ShouldBe(capabilities.Take(3));
			resource.IsComplete.ShouldBeTrue();
		}

		[Fact]
		public void CapabilitiesAreComparedUsingEquals()
		{
			var task = Substitute.For<ITask>();
			var capabilities = new ICapability[] { new ProduceCapability(), new TestCapability(), new TestCapability(), new ConsumeCapability() };
			task.RequiredCapabilities.Returns(capabilities);

			var resource = new TestResource(task);

			// should not throw - ProduceCapability implements Equals
			resource.OnCapabilityApplied(new ProduceCapability());

			// should not throw - reference-equality
			resource.OnCapabilityApplied(capabilities[1]);

			// should throw - not reference-equal, Equals not implemented
			Should.Throw<InvalidOperationException>(() => resource.OnCapabilityApplied(new TestCapability()));
		}

		public class TestCapability : Capability<TestCapability>
		{
			public override CapabilityType CapabilityType => CapabilityType.Process;
		}

		public class TestResource : Resource
		{
			public TestResource(ITask task)
			{
				Task = task;
			}
		}
	}
}
