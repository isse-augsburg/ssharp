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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
	using System;
	using System.IO;
	using System.Linq;
	using Odp;

	internal class MiniZincController : Odp.Reconfiguration.AbstractMiniZincController
	{
		private const string MinizincModel = "ConstraintModel.mzn";

		public MiniZincController(Agent[] agents) : base(MinizincModel, agents) { }

		protected override void WriteInputData(ITask task, StreamWriter writer)
		{
			var taskSequence = string.Join(",", task.RequiredCapabilities.Select(GetIdentifier));
			var isCart = string.Join(",", Agents.Select(a => (a is CartAgent).ToString().ToLower()));
			var capabilities = string.Join(",", Agents.Select(a =>
				$"{{{string.Join(",", a.AvailableCapabilities.Select(GetIdentifier))}}}"));
			var isConnected = string.Join("\n|", Agents.Select(from =>
				string.Join(",", Agents.Select(to => (from.Outputs.Contains(to) || from == to).ToString().ToLower()))));

			writer.WriteLine($"task = [{taskSequence}];");
			writer.WriteLine($"noAgents = {Agents.Length};");
			writer.WriteLine($"capabilities = [{capabilities}];");
			writer.WriteLine($"isCart = [{isCart}];");
			writer.WriteLine($"isConnected = [|{isConnected}|]");
		}

		private int GetIdentifier(ICapability capability)
		{
			if (capability is ProduceCapability)
				return 1;
			if (capability is ProcessCapability)
				return (int)((ProcessCapability)capability).ProductionAction + 1;
			if (capability is ConsumeCapability)
				return (int)Enum.GetValues(typeof(ProductionAction)).Cast<ProductionAction>().Max() + 2;
			throw new InvalidOperationException("unsupported capability");
		}
	}
}