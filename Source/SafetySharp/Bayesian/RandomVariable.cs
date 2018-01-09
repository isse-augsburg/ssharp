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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Analysis;
    using ISSE.SafetyChecking.Formula;
    using ISSE.SafetyChecking.Modeling;

    /// <summary>
    /// Class for arbitrary random variables
    /// </summary>
    public abstract class RandomVariable
    {
        public IList<Probability> Probability { get; set; }
        public Probability TrueProbability => Probability[0];
        public string Name { get; set; }

        /// <summary>
        /// Returns a formula that checks the occurence of the 'true' value of the random variable in a current state
        /// </summary>
        /// <returns></returns>
        public abstract Formula ToFormula();

        /// <summary>
        /// Returns a formula that checks the occurence of the 'true' value of the random variable in a whole trace
        /// </summary>
        /// <returns></returns>
        public abstract Formula ToOnceFormula();

        public override string ToString() => Name;
    }

    /// <summary>
    /// Random variable that represents a fault in a model
    /// </summary>
    public class FaultRandomVariable : RandomVariable
    {
        public Fault Reference { get; }

        public FaultRandomVariable(Fault reference, string name = null)
        {
            Reference = reference;
            Name = name ?? reference.Name;
        }

        public FaultRandomVariable(Fault reference, Probability[] probability, string name = null)
        {
            Reference = reference;
            Probability = probability;
            Name = name ?? reference.Name;
        }

        public override Formula ToFormula()
        {
            return new FaultFormula(Reference, Name);
        }

        public override Formula ToOnceFormula()
        {
            return new UnaryFormula(ToFormula(), UnaryOperator.Once);
        }
    }

    /// <summary>
    /// Random variable that represents an arbitrary boolean state expression
    /// </summary>
    public class BooleanRandomVariable : RandomVariable
    {
        public Func<bool> Reference { get; }

        public BooleanRandomVariable(Func<bool> reference, string name = null)
        {
            Reference = reference;
            Name = name ?? reference.ToString();
        }

        public BooleanRandomVariable(Func<bool> reference, Probability[] probability, string name = null)
        {
            Reference = reference;
            Probability = probability;
            Name = name ?? reference.ToString();
        }

        public override Formula ToFormula()
        {
            return new ExecutableStateFormula(Reference, Name);
        }

        public override Formula ToOnceFormula()
        {
            return new UnaryFormula(ToFormula(), UnaryOperator.Once);
        }
    }

    /// <summary>
    /// Random variable that represents a minimal critical set in a model
    /// </summary>
    public class McsRandomVariable : RandomVariable
    {
        public MinimalCriticalSet Reference { get; private set; }
        public ISet<FaultRandomVariable> FaultVariables { get; }

        public McsRandomVariable(MinimalCriticalSet reference, ICollection<FaultRandomVariable> faultVariables, string name = null)
        {
            Reference = reference;
            FaultVariables = new HashSet<FaultRandomVariable>(faultVariables);
            Name = name ?? reference.ToString();
        }

        public McsRandomVariable(MinimalCriticalSet reference, ICollection<FaultRandomVariable> faultVariables, Probability[] probability, string name = null)
        {
            Reference = reference;
            Probability = probability;
            FaultVariables = new HashSet<FaultRandomVariable>(faultVariables);
            Name = name ?? reference.ToString();
        }

        public override Formula ToFormula()
        {
            var faultList = FaultVariables.ToList();
            var last = faultList.Last().ToFormula();

            for (var i = faultList.Count - 2; i >= 0; i--)
            {
                last = new BinaryFormula(faultList[i].ToFormula(), BinaryOperator.And, last);
            }
            return last;
        }

        public override Formula ToOnceFormula()
        {
            var faultList = FaultVariables.ToList();
            var last = faultList.Last().ToOnceFormula();

            for (var i = faultList.Count - 2; i >= 0; i--)
            {
                last = new BinaryFormula(faultList[i].ToOnceFormula(), BinaryOperator.And, last);
            }
            return last;
        }
    }
}