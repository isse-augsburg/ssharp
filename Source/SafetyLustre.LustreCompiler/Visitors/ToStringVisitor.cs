using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace SafetyLustre.LustreCompiler.Visitors
{
    public class ToStringVisitor : Oc5BaseVisitor<string>
    {
        protected override string DefaultResult => base.DefaultResult;

        public override string Visit(IParseTree tree)
        {
            if (tree == null) return null;

            return base.Visit(tree);
        }

        public override string VisitOcfile([NotNull] Oc5Parser.OcfileContext context)
        {
            return context.VERSION() + Environment.NewLine + Visit(context.module()); ;
        }

        public override string VisitModule([NotNull] Oc5Parser.ModuleContext context)
        {
            return context.MODULE() + " " + context.IDENTIFIER() + Environment.NewLine +
                (context.constants() != null ? Visit(context.constants()) + Environment.NewLine : String.Empty) +
                (context.signals() != null ? (Visit(context.signals()) + Environment.NewLine) : String.Empty) +
                (context.variables() != null ? (Visit(context.variables()) + Environment.NewLine) : String.Empty) +
                //(context.actions() != null ? (Visit(context.actions()) + Environment.NewLine) : String.Empty) +
                //(context.constants() != null ? (Visit(context.constants()) + Environment.NewLine) : String.Empty) +
                //Visit(context.automaton()) + Environment.NewLine +
                context.ENDMODULE();
        }

        #region Constants

        public override string VisitConstants([NotNull] Oc5Parser.ConstantsContext context)
        {
            return context.CONSTANTS() + " " + context.NUMBER() + Environment.NewLine +
                string.Join(String.Empty, context.constant().ToList().Select(c => Visit(c) + Environment.NewLine)) +
                context.ENDTABLE();
        }

        public override string VisitConstant([NotNull] Oc5Parser.ConstantContext context)
        {
            return context.LIST_INDEX() + " " + context.IDENTIFIER() + Visit(context.index());
        }

        #endregion

        #region Signals

        public override string VisitSignals([NotNull] Oc5Parser.SignalsContext context)
        {
            return context.SIGNALS() + " " + context.NUMBER() + Environment.NewLine +
                string.Join(String.Empty, context.signal().ToList().Select(s => Visit(s) + Environment.NewLine)) +
                context.ENDTABLE();
        }

        public override string VisitSignal([NotNull] Oc5Parser.SignalContext context)
        {
            return context.LIST_INDEX() + " " +
                Visit(context.nature()) + " " +
                Visit(context.channel()) + " " +
                Visit(context.@bool());
        }

        public override string VisitNature([NotNull] Oc5Parser.NatureContext context)
        {
            return Visit(context.children.SingleOrDefault());
        }

        public override string VisitInput([NotNull] Oc5Parser.InputContext context)
        {
            return context.INPUT().ToString() + context.IDENTIFIER() + " " +
                Visit(context.presAction());
        }

        public override string VisitPresAction([NotNull] Oc5Parser.PresActionContext context)
        {
            return Visit(context.index()) ?? context.HYPHEN().ToString();
        }

        public override string VisitOutput([NotNull] Oc5Parser.OutputContext context)
        {
            return context.OUTPUT().ToString() + context.IDENTIFIER() + " " +
                Visit(context.outAction());
        }

        public override string VisitOutAction([NotNull] Oc5Parser.OutActionContext context)
        {
            return Visit(context.index()) ?? context.HYPHEN().ToString();
        }

        public override string VisitChannel([NotNull] Oc5Parser.ChannelContext context)
        {
            return Visit(context.children.SingleOrDefault());
        }

        public override string VisitPure([NotNull] Oc5Parser.PureContext context)
        {
            return context.PURE().ToString();
        }

        public override string VisitSingle([NotNull] Oc5Parser.SingleContext context)
        {
            return context.SINGLE() + Visit(context.index());
        }

        public override string VisitMultiple([NotNull] Oc5Parser.MultipleContext context)
        {
            return context.MULTIPLE() + " " +
                Visit(context.index()[0]) + " " +
                Visit(context.index()[1]);
        }

        public override string VisitBool([NotNull] Oc5Parser.BoolContext context)
        {
            return context.BOOL() + Visit(context.index());
        }

        #endregion

        #region Variables

        public override string VisitVariables([NotNull] Oc5Parser.VariablesContext context)
        {
            return context.VARIABLES() + " " + context.NUMBER() + Environment.NewLine +
                string.Join(String.Empty, context.variable().ToList().Select(v => Visit(v) + Environment.NewLine)) +
                context.ENDTABLE();
        }

        public override string VisitVariable([NotNull] Oc5Parser.VariableContext context)
        {
            return context.LIST_INDEX() + " " + Visit(context.index());
        }

        #endregion

        #region Actions

        #endregion

        #region Misc

        public override string VisitIndex([NotNull] Oc5Parser.IndexContext context)
        {
            return context.DOLLAR_SIGN()?.ToString() + context.NUMBER();
        }

        #endregion

        #region Automaton

        #endregion

        public override string VisitAction([NotNull] Oc5Parser.ActionContext context)
        {
            return base.VisitAction(context);
        }

        public override string VisitActions([NotNull] Oc5Parser.ActionsContext context)
        {
            return base.VisitActions(context);
        }

        public override string VisitActionTree([NotNull] Oc5Parser.ActionTreeContext context)
        {
            return base.VisitActionTree(context);
        }

        public override string VisitAtomExpression([NotNull] Oc5Parser.AtomExpressionContext context)
        {
            return base.VisitAtomExpression(context);
        }

        public override string VisitAtomValue([NotNull] Oc5Parser.AtomValueContext context)
        {
            return base.VisitAtomValue(context);
        }

        public override string VisitAutomaton([NotNull] Oc5Parser.AutomatonContext context)
        {
            return base.VisitAutomaton(context);
        }

        public override string VisitCallAction([NotNull] Oc5Parser.CallActionContext context)
        {
            return base.VisitCallAction(context);
        }

        public override string VisitCalls([NotNull] Oc5Parser.CallsContext context)
        {
            return base.VisitCalls(context);
        }

        public override string VisitChildren(IRuleNode node)
        {
            return base.VisitChildren(node);
        }

        public override string VisitClosedDag([NotNull] Oc5Parser.ClosedDagContext context)
        {
            return base.VisitClosedDag(context);
        }

        public override string VisitClosedTest([NotNull] Oc5Parser.ClosedTestContext context)
        {
            return base.VisitClosedTest(context);
        }

        public override string VisitConstantExpression([NotNull] Oc5Parser.ConstantExpressionContext context)
        {
            return base.VisitConstantExpression(context);
        }

        public override string VisitDszAction([NotNull] Oc5Parser.DszActionContext context)
        {
            return base.VisitDszAction(context);
        }

        public override string VisitErrorNode(IErrorNode node)
        {
            return base.VisitErrorNode(node);
        }

        public override string VisitExpression([NotNull] Oc5Parser.ExpressionContext context)
        {
            return base.VisitExpression(context);
        }

        public override string VisitExpressionList([NotNull] Oc5Parser.ExpressionListContext context)
        {
            return base.VisitExpressionList(context);
        }

        public override string VisitFunctionCallExpression([NotNull] Oc5Parser.FunctionCallExpressionContext context)
        {
            return base.VisitFunctionCallExpression(context);
        }

        public override string VisitIfAction([NotNull] Oc5Parser.IfActionContext context)
        {
            return base.VisitIfAction(context);
        }

        public override string VisitLinearAction([NotNull] Oc5Parser.LinearActionContext context)
        {
            return base.VisitLinearAction(context);
        }

        public override string VisitLinearActionList([NotNull] Oc5Parser.LinearActionListContext context)
        {
            return base.VisitLinearActionList(context);
        }

        public override string VisitNewState([NotNull] Oc5Parser.NewStateContext context)
        {
            return base.VisitNewState(context);
        }

        public override string VisitOpenDag([NotNull] Oc5Parser.OpenDagContext context)
        {
            return base.VisitOpenDag(context);
        }

        public override string VisitOpenTest([NotNull] Oc5Parser.OpenTestContext context)
        {
            return base.VisitOpenTest(context);
        }

        public override string VisitOpenTestList([NotNull] Oc5Parser.OpenTestListContext context)
        {
            return base.VisitOpenTestList(context);
        }

        public override string VisitOutputAction([NotNull] Oc5Parser.OutputActionContext context)
        {
            return base.VisitOutputAction(context);
        }

        public override string VisitPresentAction([NotNull] Oc5Parser.PresentActionContext context)
        {
            return base.VisitPresentAction(context);
        }

        public override string VisitStartpoint([NotNull] Oc5Parser.StartpointContext context)
        {
            return base.VisitStartpoint(context);
        }

        public override string VisitState([NotNull] Oc5Parser.StateContext context)
        {
            return base.VisitState(context);
        }

        public override string VisitStates([NotNull] Oc5Parser.StatesContext context)
        {
            return base.VisitStates(context);
        }

        public override string VisitTerminal(ITerminalNode node)
        {
            return base.VisitTerminal(node);
        }

        public override string VisitTestAction([NotNull] Oc5Parser.TestActionContext context)
        {
            return base.VisitTestAction(context);
        }

        public override string VisitVariableExpression([NotNull] Oc5Parser.VariableExpressionContext context)
        {
            return base.VisitVariableExpression(context);
        }

        protected override string AggregateResult(string aggregate, string nextResult)
        {
            return base.AggregateResult(aggregate, nextResult);
        }

        protected override bool ShouldVisitNextChild(IRuleNode node, string currentResult)
        {
            return base.ShouldVisitNextChild(node, currentResult);
        }
    }
}
