using Namotion.Reflection;

namespace Astrolabe.CodeGen;

public abstract class CodeGenerator<T, D>
{
    private HashSet<string> _alreadyAdded;
    private readonly TypeVisitor<T> _visitor;
    private readonly BaseGeneratorOptions _options;

    protected CodeGenerator(BaseGeneratorOptions options, TypeVisitor<T> visitor)
    {
        _visitor = visitor;
        _options = options;
        _alreadyAdded =
            options.ExcludedTypes?.Select(x => TypeKey(visitor.VisitType(x.ToContextualType()))).ToHashSet() ??
            new HashSet<string>();
    }

    public IEnumerable<D> CollectData(T typeData)
    {
        return !_alreadyAdded.Add(TypeKey(typeData)) ? Array.Empty<D>() : ToData(typeData);
    }

    public IEnumerable<D> CollectDataForTypes(params Type[] types)
    {
        return types.Aggregate(Enumerable.Empty<D>(),
            (o, t) => o.Concat(CollectData(_visitor.VisitType(t.ToContextualType()))));
    }

    protected abstract string TypeKey(T typeData);

    protected abstract IEnumerable<D> ToData(T typeData);
}