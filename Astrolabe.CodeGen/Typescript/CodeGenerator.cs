using Namotion.Reflection;

namespace Astrolabe.CodeGen.Typescript;

public abstract class CodeGenerator<T, D>
{
    private HashSet<string> _alreadyAdded = new();

    public IEnumerable<D> CollectData(T typeData)
    {
        var key = TypeKey(typeData);
        if (!_alreadyAdded.Add(key))
            return Array.Empty<D>();
        return ToData(typeData);
    }

    public IEnumerable<D> CollectDataForTypes(TypeVisitor<T> visitor, params Type[] types)
    {
        return types.Aggregate(Enumerable.Empty<D>(),
            (o, t) => o.Concat(CollectData(visitor.VisitType(t.ToContextualType()))));
    }

    protected abstract string TypeKey(T typeData);

    protected abstract IEnumerable<D> ToData(T typeData);
}