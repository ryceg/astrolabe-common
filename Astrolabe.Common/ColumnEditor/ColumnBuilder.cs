using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentValidation;

namespace Astrolabe.Common.ColumnEditor;

public class ColumnBuilder<TEDIT, TDB, T, T2> : Column<TEDIT, TDB>
{
    public Func<ColumnContext<TDB>, T2> GetDbValue { get; set; }

    public Action<ColumnContext<TDB>, T2> SetDbValue { get; set; }

    public Expression<Func<TEDIT, T>> GetValueExpression { get; set; }

    public Func<TEDIT, T> GetValue { get; set; }

    public Func<ColumnContext<TDB>, object> GetDbValueObject { get; init; }

    public Expression<Func<TDB, object>> GetDbValueExpression { get; init; }
    public string Property { get; init; }

    public string DataFieldName { get; set; }

    public Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>> Edit { get; set; }

    public Func<TEDIT, string> ToStringValue { get; set; }

    public Func<IQueryable<TDB>, bool, IOrderedQueryable<TDB>> AddSort { get; set; }

    public Func<IOrderedQueryable<TDB>, bool, IOrderedQueryable<TDB>> AddExtraSort { get; set; }

    public Dictionary<string, object> Attributes { get; } = new Dictionary<string, object>();

    public Action<AbstractValidator<TEDIT>> AddValidation { get; set; }

    public ColumnBuilder<TEDIT, TDB, T, T2> WithDataField(string dataFieldName)
    {
        DataFieldName = dataFieldName;
        return this;
    }

    public ColumnBuilder<TEDIT, TDB, T, T2> WithEdit(Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>> editFunc)
    {
        Edit = editFunc;
        return this;
    }

    public ColumnBuilder<TEDIT, TDB, T, T2> WithValidation(
        Action<IRuleBuilderInitial<TEDIT, T>> validation)
    {
        AddValidation = (v) => validation(v.RuleFor(GetValueExpression));
        return this;
    }
}