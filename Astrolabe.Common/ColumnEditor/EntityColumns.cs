using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentValidation;

namespace Astrolabe.Common.ColumnEditor;

public abstract class EntityColumns<TEDIT, TDB>
{
    public readonly List<Column<TEDIT, TDB>> Columns = new();

    protected EntityColumns()
    {
    }

    public Column<TEDIT, TDB> FindColumn(string field)
    {
        return Columns.Find(x =>
            string.Equals(x.Property, field, StringComparison.CurrentCultureIgnoreCase));
    }

    public virtual ColumnContext<TDB> InitContext(TDB entity)
    {
        return new ColumnContext<TDB>(entity);
    }

    public static Func<string, object> StringConverter(Type typeT)
    {
        if (typeT == typeof(bool))
        {
            return value => !string.IsNullOrWhiteSpace(value) && bool.Parse(value);
        }

        if (typeT == typeof(bool?))
        {
            return value => !string.IsNullOrWhiteSpace(value) ? bool.Parse(value) : null;
        }

        if (typeT == typeof(double?))
        {
            return value => !string.IsNullOrWhiteSpace(value) ? double.Parse(value) : null;
        }

        if (typeT == typeof(DateTime?))
        {
            return value => !string.IsNullOrWhiteSpace(value)
                ? DateTime.ParseExact(value, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal)
                : null;
        }

        if (typeT == typeof(string))
        {
            return value => value;
        }

        return null;
    }
    
    protected ColumnBuilder<TEDIT, TDB, T, T> Add<T>(Expression<Func<TEDIT, T>> property)
    {
        var propInfo = property.GetPropertyInfo();
        var dbPropInfo = typeof(TDB).GetProperty(propInfo.Name);
        Debug.Assert(dbPropInfo != null, "No property called " + propInfo.Name + " on " + nameof(TDB));
        Debug.Assert(dbPropInfo.GetMethod != null, "No getter for " + propInfo.Name + " on " + nameof(TDB));
        Debug.Assert(dbPropInfo.SetMethod != null, "No setter for " + propInfo.Name + " on " + nameof(TDB));

        var param = Expression.Variable(typeof(TDB));
        var getDbPropExpr = Expression.MakeMemberAccess(param, dbPropInfo);
        var getDbLambdaExpr = Expression.Lambda<Func<TDB, T>>(getDbPropExpr, param);
        void SetterDb(TDB x, T value) => dbPropInfo.SetMethod.Invoke(x, new object[] { value });
        return Add(property, getDbLambdaExpr, SetterDb);
    }

    protected ColumnBuilder<TEDIT, TDB, T, T> Add<T>(Expression<Func<TEDIT, T>> property,
        Expression<Func<TDB, T>> getDbLambdaExpr, Action<TDB, T> setterDb)
    {
        var propInfo = property.GetPropertyInfo();
        var getter = property.Compile();
        var getterDb = getDbLambdaExpr.Compile();
        var converter = StringConverter(typeof(T));
        var col = new ColumnBuilder<TEDIT, TDB, T, T>
        {
            Property = propInfo.Name,
            GetValueExpression = property,
            GetValue = getter,
            SetDbValue = (ctx, v) => setterDb(ctx.Entity, v),
            GetDbValue = ctx => getterDb(ctx.Entity),
            GetDbValueObject = (x) => getterDb(x.Entity),
            AddSort = (q, desc) => desc ? q.OrderByDescending(getDbLambdaExpr) : q.OrderBy(getDbLambdaExpr),
            AddExtraSort = (q, desc) => desc ? q.ThenByDescending(getDbLambdaExpr) : q.ThenBy(getDbLambdaExpr),
            ToStringValue = e => getter(e)?.ToString() ?? "",
            GetDbValueExpression =
                Expression.Lambda<Func<TDB, object>>(Expression.Convert(getDbLambdaExpr.Body, typeof(object)),
                    getDbLambdaExpr.Parameters)
        };
        col.Edit = col.StandardEdit();
        Columns.Add(col);
        return col;
    }

    protected ColumnBuilder<TEDIT, TDB, T, T2> Add<T, T2>(Expression<Func<TEDIT, T>> property,
        Expression<Func<TDB, T2>> getterDbExpr,
        Func<Func<TEDIT, T>, Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>>> makeEdit)
        where T2 : class
    {
        var propInfo = property.GetPropertyInfo();
        var getter = property.Compile();
        var getterDb = getterDbExpr.Compile();
        var col = new ColumnBuilder<TEDIT, TDB, T, T2>
        {
            Property = propInfo.Name,
            GetValueExpression = property,
            GetValue = getter,
            GetDbValue = x => getterDb(x.Entity),
            GetDbValueObject = (x) => getterDb(x.Entity),
            GetDbValueExpression =
                Expression.Lambda<Func<TDB, object>>(Expression.Convert(getterDbExpr.Body, typeof(object)),
                    getterDbExpr.Parameters),
            Edit = makeEdit(getter),
            ToStringValue = e => getter(e)?.ToString() ?? ""
        };
        Columns.Add(col);
        return col;
    }

    public async Task<ColumnContext<TDB>> Edit(TEDIT edit, ColumnContext<TDB> ctx)
    {
        foreach (var col in Columns)
        {
            ctx = await col.Edit(edit, ctx);
        }

        return ctx;
    }

    public void AddRules(AbstractValidator<TEDIT> validator)
    {
        foreach (var col in Columns)
        {
            col.AddValidation?.Invoke(validator);
        }
    }
}