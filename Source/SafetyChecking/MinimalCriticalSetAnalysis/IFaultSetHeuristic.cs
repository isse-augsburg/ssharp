// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using System.Collections.Generic;
	using AnalysisModel;

	/// <summary>
	///   Represents a heuristic for finding large safe fault sets.
	/// </summary>
	public interface IFaultSetHeuristic
	{
		/// <summary>
		///   Changes the sets that will be checked by DCCA, by reordering and adding sets.
		/// </summary>
		/// <param name="cardinalityLevel">The level of cardinality that is currently checked.</param>
		/// <param name="setsToCheck">The next sets to be checked, in reverse order (the last set is checked first).</param>
		void Augment(uint cardinalityLevel, LinkedList<FaultSet> setsToCheck);

		/// <summary>
		///   Informs the heuristic of the result of analyzing <paramref name="checkedSet" />
		///   and allows it to adapt the sets to check next.
		/// </summary>
		void Update(LinkedList<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe);
	}
}