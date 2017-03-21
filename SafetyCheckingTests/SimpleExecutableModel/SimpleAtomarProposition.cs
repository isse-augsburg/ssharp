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


namespace Tests.SimpleExecutableModel
{
    using System;
    using ISSE.SafetyChecking.Formula;
    using System.Reflection;

    public abstract class SimpleAtomarProposition : AtomarPropositionFormula
    {
        public abstract bool Evaluate(SimpleModelBase model);
    }

    /*
    public class SimpleFieldOfModelIsTrue : SimpleAtomarProposition
    {
        public string FieldName { get; }
        public FieldInfo Field { get; }

        public SimpleFieldOfModelIsTrue(Type type, string fieldName)
        {
            FieldName = fieldName;
            Field = type.GetField(FieldName);
        }
        public override bool Evaluate(SimpleModelBase model)
        {
            var value = (bool) Field.GetValue(model);
            return value;
        }
    }
    */

    public class SimpleLocalVarIsTrue : SimpleAtomarProposition
    {
        public int Index { get; }

        public SimpleLocalVarIsTrue(int index)
        {
            Index = index;
        }
        public override bool Evaluate(SimpleModelBase model)
        {
            return model.LocalBools[Index];
        }
    }

    public class SimpleLocalVarInRangeFormula : SimpleAtomarProposition
    {
        public int Index { get; }
        public int From { get; }
        public int To { get; }

        public SimpleLocalVarInRangeFormula(int index, int from, int to)
        {
            Index = index;
            From = from;
            To = to;
        }

        public SimpleLocalVarInRangeFormula(int index, int exact)
        {
            Index = index;
            From = exact;
            To = exact;
        }

        public override bool Evaluate(SimpleModelBase model)
        {
            if (model.LocalInts[Index] >= From && model.LocalInts[Index] <= To)
                return true;
            return false;
        }
    }

    public class SimpleStateInRangeFormula : SimpleAtomarProposition
    {
        public int From { get; }
        public int To { get; }

        public SimpleStateInRangeFormula(int from, int to)
        {
            From = from;
            To = to;
        }

        public SimpleStateInRangeFormula(int exact)
        {
            From = exact;
            To = exact;
        }

        public override bool Evaluate(SimpleModelBase model)
        {
            if (model.State >= From && model.State <= To)
                return true;
            return false;
        }
    }
}
