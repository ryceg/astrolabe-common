using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Astrolabe.Evaluator.Parser;

namespace Astrolabe.Evaluator;

using BinState = (EvalExpr?, ITerminalNode?);

public class ExprParser
{
    public static EvalExpr Parse(string expression)
    {
        var inputStream = new AntlrInputStream(expression);
        var speakLexer = new AstroExprLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(speakLexer);
        var speakParser = new AstroExprParser(commonTokenStream);
        var chatContext = speakParser.main();
        var visitor = new AstroExprVisitor();
        return visitor.Visit(chatContext);
    }

    public class AstroExprVisitor : AstroExprBaseVisitor<EvalExpr>
    {
        public override EvalExpr VisitOrExpr(AstroExprParser.OrExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.Or, context);
        }

        public override EvalExpr VisitMain(AstroExprParser.MainContext context)
        {
            return Visit(context.expr());
        }

        public override EvalExpr VisitVariableReference(
            AstroExprParser.VariableReferenceContext context
        )
        {
            return new VarExpr(context.Identifier().GetText());
        }

        public override EvalExpr VisitLambdaExpr(AstroExprParser.LambdaExprContext context)
        {
            return new LambdaExpr(
                Visit(context.variableReference()).AsVar(),
                Visit(context.expr())
            );
        }

        public override EvalExpr VisitTerminal(ITerminalNode node)
        {
            return node.Symbol.Type switch
            {
                AstroExprParser.Identifier => new PathExpr(new FieldPath(node.GetText(), DataPath.Empty)),
                AstroExprParser.Number => ValueExpr.From(double.Parse(node.GetText())),
                AstroExprParser.False => ValueExpr.False,
                AstroExprParser.True => ValueExpr.True,
                _ => throw new NotImplementedException()
            };
        }

        public override EvalExpr VisitPrimaryExpr(AstroExprParser.PrimaryExprContext context)
        {
            var leftPar = context.LPAR();
            return leftPar != null ? Visit(context.expr()) : base.VisitPrimaryExpr(context);
        }

        public override EvalExpr VisitPredicate(AstroExprParser.PredicateContext context)
        {
            return Visit(context.expr());
        }

        public override EvalExpr VisitFilterExpr(AstroExprParser.FilterExprContext context)
        {
            var baseExpr = Visit(context.primaryExpr());
            var predicate = context.predicate();
            return predicate != null
                ? CallExpr.Inbuilt(InbuiltFunction.Filter, [baseExpr, Visit(predicate)])
                : baseExpr;
        }

        public override EvalExpr VisitConditionExpression(
            AstroExprParser.ConditionExpressionContext context
        )
        {
            var ifExpr = Visit(context.orExpr());
            var thenExpr = context.expr();
            var elseExpr = context.conditionExpression();
            if (thenExpr != null)
                return CallExpr.Inbuilt(
                    InbuiltFunction.IfElse,
                    [ifExpr, Visit(thenExpr), Visit(elseExpr)]
                );
            return ifExpr;
        }

        public override EvalExpr VisitFunctionCall(AstroExprParser.FunctionCallContext context)
        {
            var variableString = context.variableReference().Identifier().GetText();
            InbuiltFunction? inbuilt = variableString switch
            {
                "sum" => InbuiltFunction.Sum,
                "count" => InbuiltFunction.Count,
                "string" => InbuiltFunction.String,
                _ => null
            };
            var args = context.expr().Select(Visit).ToList();
            if (inbuilt != null)
                return CallExpr.Inbuilt(inbuilt.Value, args);
            return new CallExpr(variableString, args);
        }

        public override EvalExpr VisitMapExpr(AstroExprParser.MapExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.Map, context);
        }

        public override EvalExpr VisitAndExpr(AstroExprParser.AndExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.And, context);
        }

        public override EvalExpr VisitRelationalExpr(AstroExprParser.RelationalExprContext context)
        {
            return DoFunction(
                x =>
                    x.Symbol.Type switch
                    {
                        AstroExprParser.LESS => InbuiltFunction.Lt,
                        AstroExprParser.MORE_ => InbuiltFunction.Gt,
                        AstroExprParser.LE => InbuiltFunction.LtEq,
                        AstroExprParser.GE => InbuiltFunction.GtEq,
                    },
                context
            );
        }

        public override EvalExpr VisitEqualityExpr(AstroExprParser.EqualityExprContext context)
        {
            return DoFunction(
                t => t.Symbol.Type == AstroExprParser.EQ ? InbuiltFunction.Eq : InbuiltFunction.Ne,
                context
            );
        }

        public override EvalExpr VisitMultiplicativeExpr(
            AstroExprParser.MultiplicativeExprContext context
        )
        {
            return DoFunction(
                t =>
                    t.Symbol.Type == AstroExprParser.MUL
                        ? InbuiltFunction.Multiply
                        : InbuiltFunction.Divide,
                context
            );
        }

        public override EvalExpr VisitAdditiveExpr(AstroExprParser.AdditiveExprContext context)
        {
            return DoFunction(
                t =>
                    t.Symbol.Type == AstroExprParser.PLUS
                        ? InbuiltFunction.Add
                        : InbuiltFunction.Minus,
                context
            );
        }

        public EvalExpr DoFunction(Func<ITerminalNode, InbuiltFunction> func, ParserRuleContext context)
        {
            return DoBinOps((t, e1, e2) => CallExpr.Inbuilt(func(t), [e1, e2]), context);
        }

        public EvalExpr DoBinOps(
            Func<ITerminalNode, EvalExpr, EvalExpr, EvalExpr> createExpr,
            ParserRuleContext context
        )
        {
            return context
                .children.Aggregate(
                    (BinState)(null, null),
                    (acc, next) =>
                        next switch
                        {
                            ITerminalNode tn => (acc.Item1, tn),
                            _ when Visit(next) is { } e
                                => acc is { Item2: not null, Item1: not null }
                                    ? (createExpr(acc.Item2, acc.Item1, e), null)
                                    : (e, null),
                        }
                )
                .Item1;
        }
    }
}
