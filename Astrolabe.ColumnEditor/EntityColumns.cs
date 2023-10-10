using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Astrolabe.Common;
using FluentValidation;

namespace Astrolabe.ColumnEditor;

public abstract class EntityColumns<TEDIT, TDB>
{
    public readonly List<ColumnEditor<TEDIT, TDB>> Columns = new();

    protected EntityColumns()
    {
    }

    public ColumnEditor<TEDIT, TDB> FindColumn(string field)
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
    
    protected PropertyColumnBuilder<TEDIT, TDB, T, T> Add<T>(Expression<Func<TEDIT, T>> property)
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

    protected PropertyColumnBuilder<TEDIT, TDB, T, T> Add<T>(Expression<Func<TEDIT, T>> property,
        Expression<Func<TDB, T>> getDbLambdaExpr, Action<TDB, T> setterDb)
    {
        var propInfo = property.GetPropertyInfo();
        var getDbValue = getDbLambdaExpr.Compile();
        var getEditValue = property.Compile();
        var col = new PropertyColumnBuilder<TEDIT, TDB, T, T>(propInfo.Name, property, getDbLambdaExpr, (edit, ctx) =>
        {
            var existing = getDbValue(ctx.Entity);
            var newVal = getEditValue(edit);
            var changed = !Equals(existing, newVal);
            if (changed) setterDb(ctx.Entity, newVal);
            ctx.Edited |= changed;
            return Task.FromResult(ctx);
        });
        Columns.Add(col);
        return col;
    }

    protected ColumnEditorBuilder<TEDIT, TDB, T, T2> Add<T, T2>(Expression<Func<TEDIT, T>> property,
        Expression<Func<TDB, T2>> getterDbExpr,
        Func<Func<TEDIT, T>, Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>>> makeEdit)
        where T2 : class
    {
        var propInfo = property.GetPropertyInfo();
        var col = new PropertyColumnBuilder<TEDIT, TDB, T, T2>(propInfo.Name, property, getterDbExpr, makeEdit(property.Compile()));
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
            ColumnValidationExtensions.ApplyValidation(validator, col);
        }
    }
}