// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace Tests
{
	using System.Collections.Generic;
	using System.Reflection;
	using SafetySharp.Odp;
	using Utilities;
	using Xunit.Abstractions;
	using JetBrains.Annotations;
	using System;
	using SafetySharp.Odp.Reconfiguration;

	public partial class OdpTests : Tests
	{
		public OdpTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	internal abstract class OdpTestObject : TestObject
	{
	}

	internal class Agent : BaseAgent
	{
		public const int MaxCapabilityCount = 20;
		public readonly List<ICapability> Capabilities = new List<ICapability>(20);

		public override IEnumerable<ICapability> AvailableCapabilities => Capabilities;

		internal void ConfigureTask(ITask task)
		{
			PerformReconfiguration(new [] { ReconfigurationRequest.Initial(task) });
		}
	}

	internal class Task : ITask
	{
		public Task(params ICapability[] capabilities)
		{
			RequiredCapabilities = capabilities;
		}

		public ICapability[] RequiredCapabilities { get; }

		public bool IsCompleted => false;
	}

	internal static class BaseAgentExtensions
	{
		private static readonly FieldInfo _machineField = typeof(BaseAgent).GetField("_stateMachine", BindingFlags.Instance | BindingFlags.NonPublic);

		public static string GetState(this BaseAgent agent)
		{
			var machine = _machineField.GetValue(agent);
			return machine.GetType().GetProperty("State").GetValue(machine).ToString();
		}
	}
}
