// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace RobotCell
{
	using System.Linq;
	using SafetySharp.Modeling;

	public class RobotCellModel : Model
	{
		public RobotCellModel()
		{
			Carts = new[] { new Cart(), new Cart() };
			Workpieces = new[] { new Workpiece(), new Workpiece(), new Workpiece() };

			const int count = 3;
			Sensors = Enumerable.Range(0, count).Select(index => new WorkpieceSensor(Position.Robot + index)).ToArray();
			DrillTools = Enumerable.Range(0, count).Select(index => new Tool(Position.Robot + index, RobotTask.Drill)).ToArray();
			InsertTools = Enumerable.Range(0, count).Select(index => new Tool(Position.Robot + index, RobotTask.Insert)).ToArray();
			TightenTools = Enumerable.Range(0, count).Select(index => new Tool(Position.Robot + index, RobotTask.Tighten)).ToArray();
			Robots = Enumerable.Range(0, count).Select(
				index => new Robot(Sensors[index], DrillTools[index], InsertTools[index], TightenTools[index], Position.Robot + index)).ToArray();

			ObserverController = new ObserverController(Robots[0], Robots[1], Robots[2], Carts[0], Carts[1]);
			var workpieces = new WorkpieceCollection(Workpieces[0], Workpieces[1], Workpieces[2]);

			AddRootComponents(ObserverController, workpieces);

			foreach (var sensor in Sensors)
				Bind(workpieces, sensor);

			foreach (var tool in DrillTools.Concat(InsertTools).Concat(TightenTools))
				Bind(workpieces, tool);

			foreach (var cart in Carts)
				Bind(workpieces, cart);
		}

		public Cart[] Carts { get; }
		public Workpiece[] Workpieces { get; }
		public WorkpieceSensor[] Sensors { get; }
		public Tool[] DrillTools { get; }
		public Tool[] InsertTools { get; }
		public Tool[] TightenTools { get; }
		public Robot[] Robots { get; }
		public ObserverController ObserverController { get; }

		void Bind(WorkpieceCollection workpieces, Cart cart)
		{
			Bind(cart.RequiredPorts.MoveTo = workpieces.ProvidedPorts.MoveWorkpiece);
		}

		private void Bind(WorkpieceCollection workpieces, WorkpieceSensor sensor)
		{
			Bind(sensor.RequiredPorts.GetWorkpiecePosition = workpieces.ProvidedPorts.GetWorkpiecePosition);
		}

		private void Bind(WorkpieceCollection workpieces, Tool tool)
		{
			Bind(tool.RequiredPorts.ModifyWorkpiece = workpieces.ProvidedPorts.ApplyTool);
		}
	}
}