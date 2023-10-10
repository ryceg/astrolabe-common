using System.Linq.Expressions;

namespace Astrolabe.ColumnEditor;

public interface ColumnDbExtractor<TDb>
{
    public Func<ColumnContext<TDb>, object?> GetDbValueObject { get; }

    public Expression<Func<TDb, object?>> GetDbValueExpression { get; }

    public IOrderedQueryable<TDb> AddSort(IQueryable<TDb> query, bool desc);
    
    public IOrderedQueryable<TDb> AddExtraSort(IOrderedQueryable<TDb> query, bool desc);
    
    public string Property { get; }
    
    public Type DbValueType { get; }
}