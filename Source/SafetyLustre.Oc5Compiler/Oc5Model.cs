using SafetyLustre.Oc5Compiler.Oc5Objects;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SafetyLustre.Oc5Compiler
{
    class Oc5Model
    {
        public List<ConstantExpression> Constants { get; set; } = new List<ConstantExpression>();
        public List<Signal> Signals { get; set; } = new List<Signal>();
        public List<Expression> Variables { get; set; } = new List<Expression>();
        public List<Expression> Actions { get; set; } = new List<Expression>();
        public List<Expression> States { get; set; } = new List<Expression>();
    }
}
