namespace Astrolabe.CodeGen.Typescript;

public abstract class CodeGenerator<T>
{
    private HashSet<string> _alreadyAdded = new();

    public IEnumerable<TsDeclaration> CreateDeclarations(T typeData)
    {
        var key = TypeKey(typeData);
        if (_alreadyAdded.Contains(key))
            return Array.Empty<TsDeclaration>();
        _alreadyAdded.Add(key);
        return ToDeclarations(typeData);
    }

    protected abstract string TypeKey(T typeData);

    protected abstract IEnumerable<TsDeclaration> ToDeclarations(T typeData);
}
