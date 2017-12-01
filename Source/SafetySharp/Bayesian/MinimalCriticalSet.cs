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
