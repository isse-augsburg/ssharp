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

	public class WorkpieceCollection : Component
	{
		private readonly Workpiece _workpiece1;
		private readonly Workpiece _workpiece2;
		private readonly Workpiece _workpiece3;

		public WorkpieceCollection(Workpiece workpiece1, Workpiece workpiece2, Workpiece workpiece3)
		{
			_workpiece1 = workpiece1;
			_workpiece2 = workpiece2;
			_workpiece3 = workpiece3;
		}

		public Position GetWorkpiecePosition(int workpiece)
		{
			switch (workpiece)
			{
				case 0:
					return _workpiece1.GetPosition();
				case 1:
					return _workpiece2.GetPosition();
				case 2:
					return _workpiece3.GetPosition();
				default:
					return Position.Unknown;
			}
		}

		public void MoveWorkpiece(Position position)
		{
			// TODO!
			if (_workpiece1.GetPosition() == position)
				_workpiece1.MoveTo(position);

			if (_workpiece2.GetPosition() == position)
				_workpiece1.MoveTo(position);

			if (_workpiece3.GetPosition() == position)
				_workpiece1.MoveTo(position);
		}

		private int GetWorkpieceAt(Position position)
		{
			if (_workpiece1.GetPosition() == position)
				return 0;

			if (_workpiece2.GetPosition() == position)
				return 1;

			if (_workpiece3.GetPosition() == position)
				return 2;

			return -1;
		}

		public bool ApplyTool(Position position, RobotTask task)
		{
			switch (GetWorkpieceAt(position))
			{
				case 0:
					_workpiece1.ApplyTool(task);
					return true;
				case 1:
					_workpiece2.ApplyTool(task);
					return true;
				case 2:
					_workpiece3.ApplyTool(task);
					return true;
				default:
					return false;
			}
		}
	}
}