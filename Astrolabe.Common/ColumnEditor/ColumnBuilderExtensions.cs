using System;
using System.Threading.Tasks;

namespace Astrolabe.Common.ColumnEditor;

public static class ColumnBuilderExtensions
{
    public static Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>> StandardEdit<TEDIT, TDB, T>
        (this ColumnBuilder<TEDIT, TDB, T, T> column, Func<T, T, bool> equals = null)
    {
        equals ??= (a, b) => Equals(a, b);
        return (e, ctx) =>
        {
            var existing = column.GetDbValue(ctx);
            var newVal = column.GetValue(e);
            var changed = !equals(existing, newVal);
            if (changed) column.SetDbValue(ctx, newVal);
            ctx.Edited |= changed;
            return Task.FromResult(ctx);
        };
    }
}