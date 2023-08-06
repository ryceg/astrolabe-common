using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Astrolabe.Common.ColumnEditor;

public interface Column<TEDIT, TDB> : ColumnDbExtractor<TDB>
{
    public Func<TEDIT, ColumnContext<TDB>, Task<ColumnContext<TDB>>> Edit { get; }

    public Func<TEDIT, string> ToStringValue { get; }

    public Action<AbstractValidator<TEDIT>> AddValidation { get; }

    public Func<IQueryable<TDB>, bool, IOrderedQueryable<TDB>> AddSort { get; }

    public Func<IOrderedQueryable<TDB>, bool, IOrderedQueryable<TDB>> AddExtraSort { get; set; }

    public Dictionary<string, object> Attributes { get; }
}