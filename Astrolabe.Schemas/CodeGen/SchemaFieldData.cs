using Astrolabe.CodeGen;

namespace Astrolabe.Schemas.CodeGen;

public record SchemaFieldData(Type Type, bool Nullable, ICollection<object> Metadata)
{ 
    public T? GetAttribute<T>()
    {
        return Metadata.OfType<T>().FirstOrDefault();
    }
}

public record EnumerableData(Type Type, bool Nullable, ICollection<object> Metadata, Func<SchemaFieldData> Element)
    : SchemaFieldData(Type, Nullable, Metadata);

public record ObjectData(Type Type, bool Nullable, ICollection<object> Metadata, IEnumerable<SchemaFieldMember> Members)
    : SchemaFieldData(Type, Nullable, Metadata)
{
    public static ObjectData FromMembers(Type type, bool nullable,
        ICollection<TypeMember<SchemaFieldData>> members)
    {
        return new ObjectData(type, nullable, type.GetCustomAttributes(true), members.Select(x =>
            new SchemaFieldMember(x.FieldName, x.Properties.First().Name,
                x.Properties.Select(p => (p.DeclaringType!, p.GetCustomAttributes(true))), x.Type, x.Data)));
    }
}

public record SchemaFieldMember(
    string FieldName,
    string PropertyName,
    IEnumerable<(Type, object[])> PropertyMetadata,
    Type Type,
    Func<SchemaFieldData> Data)
{
    public T? GetAttribute<T>()
    {
        return GetAttributes<T>().FirstOrDefault();
    }
    
    public IEnumerable<T> GetAttributes<T>()
    {
        return PropertyMetadata.SelectMany(x => x.Item2).OfType<T>();
    }

}