using System;
using Microsoft.Z3;

namespace ProductionCell
{
    class Z3BasedObserverController : ObserverController
    {
        public override void Reconfigure()
        {
            using (var ctx = new Context())
            {
                var x = ctx.MkConst("x", ctx.MkIntSort());
                var y = ctx.MkConst("y", ctx.MkIntSort());
                var zero = ctx.MkNumeral(0, ctx.MkIntSort());
                var one = ctx.MkNumeral(1, ctx.MkIntSort());
                var three = ctx.MkNumeral(3, ctx.MkIntSort());

                var s = ctx.MkSolver();
                s.Assert(ctx.MkAnd(ctx.MkGt((ArithExpr)x, (ArithExpr)zero), ctx.MkEq((ArithExpr)y,
                    ctx.MkAdd((ArithExpr)x, (ArithExpr)one)), ctx.MkLt((ArithExpr)y, (ArithExpr)three)));
                Console.WriteLine(s.Check());

                var m = s.Model;
                foreach (var d in m.Decls)
                    Console.WriteLine(d.Name + " -> " + m.ConstInterp(d));

            }
        }
    }
}