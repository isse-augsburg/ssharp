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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System.Collections.Generic;

	public class TaskFragment
	{
		public ITask Task { get; }
		public int Start { get; private set; }
		public int End { get; private set; }

		public ISet<ICapability> Capabilities { get; } = new HashSet<ICapability>();

		public int Length => End - Start + 1;

		public TaskFragment(ITask task, int start, int end)
		{
			Task = task;
			Start = start;
			End = end;

			Capabilities.UnionWith(task.RequiredCapabilities.Slice(start, end));
		}

		public bool Prepend(int newStart)
		{
			if (newStart >= Start)
				return false;
			
			Capabilities.UnionWith(Task.RequiredCapabilities.Slice(newStart, Start - 1));
			Start = newStart;
			return true;
		}

		public bool Append(int newEnd)
		{
			if (newEnd <= End)
				return false;

			Capabilities.UnionWith(Task.RequiredCapabilities.Slice(End + 1, newEnd));
			End = newEnd;
			return true;
		}
	}
}