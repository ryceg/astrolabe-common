namespace Astrolabe.CodeGen.Typescript;

public abstract class CodeGenerator<T>
{
    private HashSet<T> _alreadyAdded = new();

    public IEnumerable<TsDeclaration> CreateDeclarations(T typeData)
    {
        if (_alreadyAdded.Contains(typeData))
            return Array.Empty<TsDeclaration>();
        _alreadyAdded.Add(typeData);
        return ToDeclarations(typeData);
    }

    protected abstract IEnumerable<TsDeclaration> ToDeclarations(T typeData);
}
