using System.Linq.Expressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Astrolabe.Evaluator.Parser;

namespace Astrolabe.Evaluator;

using BinState = (Expr?, ITerminalNode?);

public class ExprParser
{
    public static Expr Parse(string expression)
    {
        var inputStream = new AntlrInputStream(expression);
        var speakLexer = new AstroExprLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(speakLexer);
        var speakParser = new AstroExprParser(commonTokenStream);
        var chatContext = speakParser.main();
        var visitor = new AstroExprVisitor();
        return visitor.Visit(chatContext);
    }

    public class AstroExprVisitor : AstroExprBaseVisitor<Expr>
    {
        public override Expr VisitOrExpr(AstroExprParser.OrExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.Or, context);
        }

        public override Expr VisitMain(AstroExprParser.MainContext context)
        {
            return Visit(context.expr());
        }

        public override Expr VisitVariableReference(
            AstroExprParser.VariableReferenceContext context
        )
        {
            return new VarExpr(context.Identifier().GetText());
        }

        public override Expr VisitLambdaExpr(AstroExprParser.LambdaExprContext context)
        {
            return new LambdaExpr(
                Visit(context.variableReference()).AsVar(),
                Visit(context.expr())
            );
        }

        public override Expr VisitTerminal(ITerminalNode node)
        {
            return node.Symbol.Type switch
            {
                AstroExprParser.Identifier
                    => ExprValue.From(new FieldPath(node.GetText(), DataPath.Empty)),
                AstroExprParser.Number => ExprValue.From(double.Parse(node.GetText())),
                AstroExprParser.False => ExprValue.False,
                AstroExprParser.True => ExprValue.True,
                _ => throw new NotImplementedException()
            };
        }

        public override Expr VisitPrimaryExpr(AstroExprParser.PrimaryExprContext context)
        {
            var leftPar = context.LPAR();
            return leftPar != null ? Visit(context.expr()) : base.VisitPrimaryExpr(context);
        }

        public override Expr VisitPredicate(AstroExprParser.PredicateContext context)
        {
            return Visit(context.expr());
        }

        public override Expr VisitFilterExpr(AstroExprParser.FilterExprContext context)
        {
            var baseExpr = Visit(context.primaryExpr());
            var predicate = context.predicate();
            return predicate != null
                ? new CallExpr(InbuiltFunction.Filter, [baseExpr, Visit(predicate)])
                : baseExpr;
        }

        public override Expr VisitConditionExpression(
            AstroExprParser.ConditionExpressionContext context
        )
        {
            var ifExpr = Visit(context.orExpr());
            var thenExpr = context.expr();
            var elseExpr = context.conditionExpression();
            if (thenExpr != null)
                return new CallExpr(
                    InbuiltFunction.IfElse,
                    [ifExpr, Visit(thenExpr), Visit(elseExpr)]
                );
            return ifExpr;
        }

        public override Expr VisitFunctionCall(AstroExprParser.FunctionCallContext context)
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
                return new CallExpr(inbuilt.Value, args);
            return new CallEnvExpr(variableString, args);
        }

        public override Expr VisitMapExpr(AstroExprParser.MapExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.Map, context);
        }

        public override Expr VisitAndExpr(AstroExprParser.AndExprContext context)
        {
            return DoFunction(_ => InbuiltFunction.And, context);
        }

        public override Expr VisitRelationalExpr(AstroExprParser.RelationalExprContext context)
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

        public override Expr VisitEqualityExpr(AstroExprParser.EqualityExprContext context)
        {
            return DoFunction(
                t => t.Symbol.Type == AstroExprParser.EQ ? InbuiltFunction.Eq : InbuiltFunction.Ne,
                context
            );
        }

        public override Expr VisitMultiplicativeExpr(
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

        public override Expr VisitAdditiveExpr(AstroExprParser.AdditiveExprContext context)
        {
            return DoFunction(
                t =>
                    t.Symbol.Type == AstroExprParser.PLUS
                        ? InbuiltFunction.Add
                        : InbuiltFunction.Minus,
                context
            );
        }

        public Expr DoFunction(Func<ITerminalNode, InbuiltFunction> func, ParserRuleContext context)
        {
            return DoBinOps((t, e1, e2) => new CallExpr(func(t), [e1, e2]), context);
        }

        public Expr DoBinOps(
            Func<ITerminalNode, Expr, Expr, Expr> createExpr,
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
