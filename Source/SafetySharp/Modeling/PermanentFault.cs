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

namespace SafetySharp.Modeling
{
	/// <summary>
	///   Represents a permanent fault that can be activated completely nondeterministically; once activated, it is always
	///   activated when an activation is possible.
	/// </summary>
	public sealed class PermanentFault : Fault
	{
		private bool _isActive;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public PermanentFault()
			: base(requiresActivationNotification: true)
		{
		}

		/// <summary>
		///   Checks whether the fault can be activated nondeterministically, or whether it has to be or cannot be activated. This
		///   method has no side effects, as otherwise S#'s fault activation mechanism would be completely broken.
		/// </summary>
		protected override Activation CheckActivation()
		{
			return _isActive ? Activation.Forced : Activation.Nondeterministic;
		}

		/// <summary>
		///   Invoked when the fault was activated.
		/// </summary>
		public override void OnActivated()
		{
			_isActive = true;
		}
	}
}