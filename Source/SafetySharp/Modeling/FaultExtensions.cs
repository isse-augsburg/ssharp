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
	using System.Collections.Generic;
	using Utilities;

	/// <summary>
	///   Provides helper methods for working with <see cref="Fault" /> instances.
	/// </summary>
	public static class FaultExtensions
	{
		/// <summary>
		///   Suppresses all activations of the <paramref name="faults" />.
		/// </summary>
		public static void SuppressActivations(this IEnumerable<Fault> faults)
		{
			Requires.NotNull(faults, nameof(faults));

			foreach (var fault in faults)
				fault.Activation = Activation.Suppressed;
		}

		/// <summary>
		///   Forces the activations of the <paramref name="faults" />, i.e., whenever the faults can be activated, they are activated.
		/// </summary>
		public static void ForceActivations(this IEnumerable<Fault> faults)
		{
			Requires.NotNull(faults, nameof(faults));

			foreach (var fault in faults)
				fault.Activation = Activation.Forced;
		}

		/// <summary>
		///   Suppresses all activations of the <paramref name="fault" />.
		/// </summary>
		public static void SuppressActivation(this Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			fault.Activation = Activation.Suppressed;
		}

		/// <summary>
		///   Forces the activations of the <paramref name="fault" />, i.e., whenever the fault can be activated, it is indeed
		///   activated.
		/// </summary>
		public static void ForceActivation(this Fault fault)
		{
			Requires.NotNull(fault, nameof(fault));
			fault.Activation = Activation.Forced;
		}

		/// <summary>
		///   Toggles the <paramref name="fault" />'s <see cref="Activation" /> between <see cref="Activation.Forced" /> and
		///   <see cref="Activation.Suppressed" />, with <see cref="Activation.Nondeterministic" /> being
		///   treated as <see cref="Activation.Suppressed" />. This method should not be used while model checking.
		/// </summary>
		public static void ToggleActivationMode(this Fault fault)
		{
			fault.Activation = fault.Activation == Activation.Forced ? Activation.Suppressed : Activation.Forced;
		}
	}
}