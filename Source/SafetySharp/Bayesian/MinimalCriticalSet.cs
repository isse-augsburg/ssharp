// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Stefan Fritsch
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

namespace SafetySharp.Bayesian
{
    using System.Collections.Generic;
    using System.Linq;
    using ISSE.SafetyChecking.Formula;
    using ISSE.SafetyChecking.Modeling;

    public class MinimalCriticalSet
    {
        public ISet<Fault> Faults { get; }

        public MinimalCriticalSet(ISet<Fault> faults)
        {
            Faults = faults;
        }

        public override string ToString()
        {
            return $"mcs{{{Faults}}}";
        }

        public Formula ToFormula()
        {
            var faultList = Faults.ToList();
            Formula last = new FaultFormula(faultList.Last());

            for (var i = faultList.Count - 2; i >= 0; i--)
            {
                var faultFormula = new FaultFormula(faultList[i]);
                last = new BinaryFormula(faultFormula, BinaryOperator.And, last);
            }
            return last;
        }
    }
}
