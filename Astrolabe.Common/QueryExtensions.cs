using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Astrolabe.Common;

public static class QueryExtensions
{
    public static IQueryable<T> WhereOne<T>(this IQueryable<T> query, ICollection<Expression<Func<T, bool>>> clauses)
    {
        if (!clauses.Any())
        {
            return query;
        }

        if (clauses.Count == 1)
        {
            return query.Where(clauses.Single());
        }

        var lambdaParam = Expression.Parameter(typeof(T));
        var expression = clauses.Skip(1).Aggregate(CallPredicate(clauses.First()),
            (e, next) => Expression.OrElse(e, CallPredicate(next)));
        return query.Where(Expression.Lambda<Func<T, bool>>(expression, lambdaParam));

        Expression CallPredicate(Expression<Func<T, bool>> pred)
        {
            return Expression.Invoke(pred, lambdaParam);
        }
    }
}