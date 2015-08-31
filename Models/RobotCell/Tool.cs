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
	using SafetySharp.Modeling;
	using SafetySharp.Modeling.Faults;

	public class Tool : Component
	{
		private readonly Position _position;
		private readonly RobotTask _task;
		private bool _isWorking = true;

		public Tool(Position position, RobotTask task)
		{
			_position = position;
			_task = task;
		}

		public void UseTool()
		{
			_isWorking &= ModifyWorkpiece(_position, _task);
		}

		public bool IsBroken() => !_isWorking;
		public extern bool ModifyWorkpiece(Position position, RobotTask task);

		[Persistent]
		public class Broken : Fault
		{
			public bool ModifyWorkpiece(Position position, RobotTask task)
			{
				return false;
			}
		}
	}
}