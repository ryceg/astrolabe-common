namespace Astrolabe.Validation;

public enum CompareType
{
    Eq,
    Lt,
    LtEq,
    Gt,
    GtEq,
    Ne
}

public enum LogicType
{
    And,
    Or,
    Not
}

public enum MathBinOp
{
    Add,
    Minus,
    Multiply,
    Divide
}

public interface Expr
{
}


public static class ConstantExtensions
{
    public static Expr ToExpr(this object v)
    {
        return new ConstantExpr(v);
    }
}

public record ConstantExpr(object Value) : Expr;

public record FromPath(PathExpr Path, bool Config = false) : Expr;
    
public record PathExpr(Expr Segment, PathExpr? Parent)
{
    public static implicit operator PathExpr(string path)
    {
        return new PathExpr(path.ToExpr(), null);
    }
    
    public static PathExpr IndexPath(int index, PathExpr? parent)
    {
        return new PathExpr(index.ToExpr(), parent);
    }
}

public record LogicOpExpr(LogicType LogicType, Expr E1, Expr? E2) : Expr;

public record CompareExpr(CompareType CompareType, Expr E1, Expr E2) : Expr;

public record MathBinOpExpr(MathBinOp MathBinOp, Expr E1, Expr E2) : Expr;

