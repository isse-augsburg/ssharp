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

using ISSE.SafetyChecking.Formula;
using System.Linq;

namespace SafetyLustre
{
    public abstract class LustreAtomarProposition : AtomarPropositionFormula
    {
        protected LustreAtomarProposition(string label) : base(label) { }

        public abstract bool Evaluate(LustreModelBase model);
    }

    public class LustreFirstOutputIsTrue : LustreAtomarProposition
    {
        public LustreFirstOutputIsTrue(string label = null) : base(label) { }

        public override bool Evaluate(LustreModelBase model)
        {
            return (bool)model.Outputs.FirstOrDefault(); ;
        }
    }

    public class LustrePressureBelowThreshold : LustreAtomarProposition
    {

        public static int threshold = 60;

        public LustrePressureBelowThreshold(string label = null) : base(label)
        {
        }

        public override bool Evaluate(LustreModelBase model)
        {
            return (int)model.Outputs.FirstOrDefault() < threshold;
        }
    }

    /*
	public class LustreLocalVarIsTrue : LustreAtomarProposition
	{
		public int Index { get; }

		public LustreLocalVarIsTrue(int index, string label = null) : base(label)
		{
			Index = index;
		}
		public override bool Evaluate(LustreModelBase model)
		{
			return model.LocalBools[Index];
		}
	}

	public class LustreLocalVarInRangeFormula : LustreAtomarProposition
	{
		public int Index { get; }
		public int From { get; }
		public int To { get; }

		public LustreLocalVarInRangeFormula(int index, int from, int to, string label = null) : base(label)
		{
			Index = index;
			From = from;
			To = to;
		}

		public LustreLocalVarInRangeFormula(int index, int exact, string label = null) : base(label)
		{
			Index = index;
			From = exact;
			To = exact;
		}

		public override bool Evaluate(LustreModelBase model)
		{
			if (model.LocalInts[Index] >= From && model.LocalInts[Index] <= To)
				return true;
			return false;
		}
	}

	public class LustreStateInRangeFormula : LustreAtomarProposition
	{
		public int From { get; }
		public int To { get; }

		public LustreStateInRangeFormula(int from, int to, string label=null) : base(label)
		{
			From = from;
			To = to;
		}

		public LustreStateInRangeFormula(int exact, string label = null) : base(label)
		{
			From = exact;
			To = exact;
		}

		public override bool Evaluate(LustreModelBase model)
		{
			if (model.State >= From && model.State <= To)
				return true;
			return false;
		}
	}
	*/
}
