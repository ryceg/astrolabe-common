using System.Linq.Expressions;
using Astrolabe.Evaluator;
using Sprache;

namespace ParseTest;

public class ExprParser
{
    private static Expr ResolveFunctionCall(string name, IList<Expr> args)
    {
        InbuiltFunction? inbuilt = name.ToLower() switch
        {
            "sum" => InbuiltFunction.Sum,
            "count" => InbuiltFunction.Count,
            "string" => InbuiltFunction.String,
            _ => null
        };
        if (inbuilt != null)
            return new CallExpr(inbuilt.Value, args);
        return new CallEnvExpr(name.ToLower(), args);
    }

    public static Parser<Expr> InfixOps(
        Parser<Expr> nextExpr,
        IDictionary<char, InbuiltFunction> ops
    )
    {
        return Parse.ChainOperator(
            Parse.Chars(ops.Keys.ToArray()).Token(),
            nextExpr,
            (op, left, right) => new CallExpr(ops[op], [left, right])
        );
    }

    public static Parser<Expr> InfixOps(
        Parser<Expr> nextExpr,
        IDictionary<string, InbuiltFunction> ops
    )
    {
        return Parse.ChainOperator(
            ops.Keys.Select(x => Parse.String(x).Text()).Aggregate((a, x) => a.Or(x)).Token(),
            nextExpr,
            (op, left, right) => new CallExpr(ops[op], [left, right])
        );
    }

    private static readonly Parser<Expr> Number = Parse.Decimal.Select(x =>
        ExprValue.From(double.Parse(x))
    );

    private static readonly Parser<Expr> String = Parse
        .Char('"')
        .Then(_ =>
            Parse
                .CharExcept('\\')
                .Or(Parse.Char('\\').Then(x => Parse.Chars("\\\"")))
                .Until(Parse.Char('"'))
        )
        .Text()
        .Select(ExprValue.From);

    private static readonly Parser<string> Identifier = Parse.Identifier(
        Parse.Letter,
        Parse.LetterOrDigit.Or(Parse.Chars("_-"))
    );

    private static readonly Parser<Expr> VarExpr = Parse
        .Char('$')
        .Then(_ => Identifier.Select(x => new VarExpr(x)));

    private static readonly Parser<Expr> PathSegment = Identifier.Select(x =>
        ExprValue.From(new FieldPath(x, DataPath.Empty))
    );

    public static readonly Parser<Expr> LetExpression =
        from l in Parse.String("let").Token()
        from v in VarExpr
        from eq in Parse.Char('=').Token()
        from e in Expression
        from _ in Parse.String("in").Token()
        from inE in Expression
        select new LetExpr([(v.AsVar(), e)], inE);

    public static readonly Parser<Func<Expr, Expr>> ArgList = Parse
        .Ref(() => Expression)
        .DelimitedBy(Parse.Char(','))
        .Contained(Parse.Char('('), Parse.Char(')'))
        .Then(args =>
            Parse.Return<Func<Expr, Expr>>(v => ResolveFunctionCall(v.AsVar().Name, args.ToList()))
        );

    public static readonly Parser<Func<Expr, Expr>> LambdaExpr = Parse
        .String("=>")
        .Token()
        .Then(_ =>
            Expression.Then(x => Parse.Return<Func<Expr, Expr>>(v => new LambdaExpr(v.AsVar(), x)))
        );

    public static readonly Parser<Expr> VarOrCallExpression = VarExpr.Then(x =>
        ArgList
            .Or(LambdaExpr)
            .Optional()
            .Select(afterVar => afterVar.IsDefined ? afterVar.Get()(x) : x)
    );

    public static readonly Parser<Expr> TermExpression = Number
        .Or(String)
        .Or(LetExpression)
        .Or(PathSegment)
        .Or(VarOrCallExpression)
        .Or(
            Parse
                .Ref(() => TermExpression)
                .Contained(Parse.Char('(').Token(), Parse.Char(')').Token())
        );

    private static readonly Parser<Expr> IndexExpression = Parse
        .Ref(() => Expression)
        .Contained(Parse.Char('['), Parse.Char(']'));

    public static readonly Parser<Expr> DotArgument = TermExpression.Then(x =>
        IndexExpression.Optional().Select(ix => ix.IsDefined ? new FilterExpr(x, ix.Get()) : x)
    );

    public static readonly Parser<Expr> DotParser = DotArgument
        .DelimitedBy(Parse.Char('.'))
        .Select(x => x.Aggregate((acc, next) => new DotExpr(acc, next)));

    public static readonly Parser<Expr> ComparisonOps = InfixOps(
        DotParser,
        new Dictionary<string, InbuiltFunction>
        {
            { ">=", InbuiltFunction.GtEq },
            { "<=", InbuiltFunction.LtEq },
            { ">", InbuiltFunction.Gt },
            { "<", InbuiltFunction.Lt },
        }
    );

    public static readonly Parser<Expr> MulDivOp = InfixOps(
        ComparisonOps,
        new Dictionary<char, InbuiltFunction>
        {
            { '*', InbuiltFunction.Multiply },
            { '/', InbuiltFunction.Divide }
        }
    );

    public static readonly Parser<Expr> AddMinusOp = InfixOps(
        MulDivOp,
        new Dictionary<char, InbuiltFunction>
        {
            { '+', InbuiltFunction.Add },
            { '-', InbuiltFunction.Minus }
        }
    );

    public static readonly Parser<Expr> Bracketed = AddMinusOp.Contained(
        Parse.Char('('),
        Parse.Char(')')
    );

    public static readonly Parser<Expr> Expression = Bracketed.Or(AddMinusOp);
}
