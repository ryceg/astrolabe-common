namespace Astrolabe.CodeGen.Typescript;

public class SimpleTypeVisitor : TypeVisitor<SimpleTypeData>
{
    protected override SimpleTypeData VisitEnumerable(Type type, bool nullable, Func<SimpleTypeData> elemData)
    {
        return new EnumerableTypeData(type, nullable, elemData);
    }

    protected override SimpleTypeData VisitPrimitive(Type type, bool nullable)
    {
        return new SimpleTypeData(type, nullable);
    }

    protected override SimpleTypeData VisitObject(Type type, bool nullable, IEnumerable<TypeMember<SimpleTypeData>> members)
    {
        return new ObjectTypeData(type, nullable, members);
    }
}

public record SimpleTypeData(Type Type, bool Nullable);

public record EnumerableTypeData(Type Type, bool Nullable, Func<SimpleTypeData> Element) : SimpleTypeData(Type, Nullable);

public record ObjectTypeData(Type Type, bool Nullable, IEnumerable<TypeMember<SimpleTypeData>> Members) : SimpleTypeData(Type, Nullable);