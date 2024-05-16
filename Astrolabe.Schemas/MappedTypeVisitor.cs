using System.Reflection;
using Astrolabe.CodeGen;
using Astrolabe.Schemas.CodeGen;

namespace Astrolabe.Schemas;

public class MappedTypeVisitor(
    Func<Type, bool, IEnumerable<TypeMember<SchemaFieldData>>, SchemaFieldData?>? overrideObject = null,
    Func<Type, bool, SchemaFieldData?>? overridePrimitive = null,
    Func<Type, bool, Func<SchemaFieldData>, SchemaFieldData?>? overrideEnumerable = null) : TypeVisitor<SchemaFieldData>
{
    protected override SchemaFieldData VisitEnumerable(Type type, bool nullable, Func<SchemaFieldData> elemData)
    {
        var overridden = overrideEnumerable?.Invoke(type, nullable, elemData);
        return overridden != null
            ? overridden
            : new EnumerableData(type, nullable, type.GetCustomAttributes(true), elemData);
    }

    protected override SchemaFieldData VisitPrimitive(Type type, bool nullable)
    {
        var overridden = overridePrimitive?.Invoke(type, nullable);
        return overridden != null ? overridden : new SchemaFieldData(type, nullable, type.GetCustomAttributes(true));
    }

    protected override SchemaFieldData VisitObject(Type type, bool nullable,
        IEnumerable<TypeMember<SchemaFieldData>> members)
    {
        var memberList = members.ToList();
        var overridden = overrideObject?.Invoke(type, nullable, memberList);
        return overridden ?? new ObjectData(type, nullable, type.GetCustomAttributes(true), memberList.Select(x =>
            new SchemaFieldMember(x.FieldName, x.Properties.First().Name,
                x.Properties.Select(p => (p.DeclaringType!, p.GetCustomAttributes(true))), x.Type, x.Data)));
    }
}