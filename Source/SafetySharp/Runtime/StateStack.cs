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

namespace SafetySharp.Runtime
{
	using System.Collections.Generic;
	using Utilities;

	/// <summary>
	///   Represents a stack of <see cref="RuntimeModel" /> states that have yet to be checked.
	/// </summary>
	/// <remarks>
	///   When enumerating all states of a model in a depth-first fashion, we have to store the next states (that are computed all
	///   at once) somewhere while also being able to generate the counter example. When check a new state, a new frame is allocated
	///   on the stack and all unknown successor states are stored in that frame. We then take the topmost state, compute its
	///   successor states, and so on. When a formula violation is detected, the counter example consists of the last states of each
	///   frame on the stack. When a frame has been fully enumerated without detecting a formula violation, the stack frame is
	///   removed, the topmost state of the topmost frame is removed, and the new topmost state is checked.
	/// </remarks>
	internal sealed unsafe class StateStack : DisposableObject
	{
		/// <summary>
		///   The memory where the frames are stored.
		/// </summary>
		private readonly Frame* _frames;

		/// <summary>
		///   The buffer that stores the frames.
		/// </summary>
		private readonly MemoryBuffer _framesBuffer = new MemoryBuffer();

		/// <summary>
		///   The memory where the states are stored.
		/// </summary>
		private readonly int* _states;

		/// <summary>
		///   The buffer that stores the states.
		/// </summary>
		private readonly MemoryBuffer _statesBuffer = new MemoryBuffer();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="capacity">The maximum number of states that can be stored in the stack.</param>
		public StateStack(int capacity)
		{
			_framesBuffer.Resize((long)capacity * sizeof(Frame));
			_statesBuffer.Resize((long)capacity * sizeof(int));

			_frames = (Frame*)_framesBuffer.Pointer;
			_states = (int*)_statesBuffer.Pointer;
		}

		/// <summary>
		///   Gets the number of frames on the stack.
		/// </summary>
		public int FrameCount { get; private set; }

		/// <summary>
		///   Pushes a new frame onto the stack.
		/// </summary>
		public void PushFrame()
		{
			var offset = FrameCount == 0 ? 0 : _frames[FrameCount - 1].Offset + _frames[FrameCount - 1].Count;
			_frames[FrameCount++] = new Frame { Offset = offset };
		}

		/// <summary>
		///   Pushes the <paramref name="state" /> onto the stack for the current frame.
		/// </summary>
		/// <param name="state">The state that should be pushed onto the stack.</param>
		public void PushState(int state)
		{
			var offset = _frames[FrameCount - 1].Offset + _frames[FrameCount - 1].Count;
			_states[offset] = state;

			_frames[FrameCount - 1].Count += 1;
		}

		/// <summary>
		///   Tries to get the topmost <paramref name="state" /> from the stack if there is one. Returns <c>false</c> to indicate that
		///   the stack was empty and no <paramref name="state" /> was returned.
		/// </summary>
		/// <param name="state">Returns the index of the topmost state on the stack.</param>
		public bool TryGetState(out int state)
		{
			while (FrameCount != 0)
			{
				if (_frames[FrameCount - 1].Count > 0)
				{
					// Return the frame's topmost state but do not yet remove the state as it might
					// be needed later when constructing the counter example
					state = _states[_frames[FrameCount - 1].Offset + _frames[FrameCount - 1].Count - 1];
					return true;
				}

				// We're done with the frame and we can now remove the topmost state of the previous frame
				// as we no longer need it to construct a counter example
				--FrameCount;

				if (FrameCount > 0)
					_frames[FrameCount - 1].Count -= 1;
			}

			state = 0;
			return false;
		}

		/// <summary>
		///   Gets the trace the stack currently represents, i.e., returns the sequence of topmost states of each frame, starting with
		///   the oldest one.
		/// </summary>
		public int[] GetTrace()
		{
			// We have to explicitly allocate and fill a list here, as pointers are not allowed in iterators
			var trace = new List<int>(FrameCount);

			for (var i = 0; i < FrameCount; ++i)
			{
				if (_frames[i].Count > 0)
					trace.Add(_states[_frames[i].Offset + _frames[i].Count - 1]);
			}

			return trace.ToArray();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_framesBuffer.SafeDispose();
			_statesBuffer.SafeDispose();
		}

		/// <summary>
		///   Represents a frame of the state stack.
		/// </summary>
		private struct Frame
		{
			/// <summary>
			///   The offset into the <see cref="_states" /> array where the index of the frame's first state is stored.
			/// </summary>
			public int Offset;

			/// <summary>
			///   The number of states the frame consists of.
			/// </summary>
			public int Count;
		}
	}
}