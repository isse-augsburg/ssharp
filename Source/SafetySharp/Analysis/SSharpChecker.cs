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

namespace SafetySharp.Analysis
{
	using System;
	using System.Diagnostics;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Represents a model checker specifically created to check S# models.
	/// </summary>
	public class SSharpChecker : ModelChecker
	{
		/// <summary>
		///   The default capacity used for state storage.
		/// </summary>
		public const int DefaultCapacity = 1 << 26;

		/// <summary>
		///   Gets or sets the number of states that can be stored during model checking.
		/// </summary>
		public int StateCapacity { get; set; } = DefaultCapacity;

		/// <summary>
		///   Gets or sets the number of CPUs that are used for model checking. The value is clamped to the interval of [1, #CPUs].
		/// </summary>
		public int CpuCount { get; set; } = Int32.MaxValue;

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />. Returns a
		///   <see cref="CounterExample" /> if the invariant is violated, <c>null</c> otherwise.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public override AnalysisResult CheckInvariant(Model model, Formula invariant)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var checker = new InvariantChecker(model, invariant, Output, StateCapacity, CpuCount, enableFaultOptimization: false))
			{
				var result = default(AnalysisResult);
				var initializationTime = stopwatch.Elapsed;
				stopwatch.Restart();

				try
				{
					result = checker.Check();
					return result;
				}
				finally
				{
					stopwatch.Stop();

					Output(String.Empty);
					Output("===============================================");
					Output($"Initialization time: {initializationTime}");
					Output($"Model checking time: {stopwatch.Elapsed}");
					Output($"{(int)(result.StateCount / stopwatch.Elapsed.TotalSeconds):n0} states per second");
					Output($"{(int)(result.TransitionCount / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
					Output("===============================================");
					Output(String.Empty);
				}
			}
		}

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public override AnalysisResult Check(Model model, Formula formula)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");
			throw new NotImplementedException();
		}
	}
}