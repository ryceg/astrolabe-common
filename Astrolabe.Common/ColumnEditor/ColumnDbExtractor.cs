using System;
using System.Linq.Expressions;

namespace Astrolabe.Common.ColumnEditor;

public interface ColumnDbExtractor<TDB>
{
    public Func<ColumnContext<TDB>, object> GetDbValueObject { get; }

    public Expression<Func<TDB, object>> GetDbValueExpression { get; }

    public string Property { get; }
}