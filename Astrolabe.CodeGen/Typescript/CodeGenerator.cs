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

    protected abstract string TypeKey(T typeData);

    protected abstract IEnumerable<D> ToData(T typeData);
}
