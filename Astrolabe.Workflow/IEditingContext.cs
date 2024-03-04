namespace Astrolabe.Workflow;

public interface IEditingContext<T>
{
    (T?, T) GetEntities();
}

public record SimpleEditingContext<T>(T? Original, T Edited) : IEditingContext<T>
{
    public (T?, T) GetEntities()
    {
        return (Original, Edited);
    }
}

public static class WorkflowEditContextExtensions
{
    public static bool ChangedTo<T, V>(this IEditingContext<T> context, Func<T, V> getter, V compare)
    {
        var (orig, edited) = context.GetEntities();
        var newValue = getter(edited);
        if (Equals(newValue, compare))
        {
            return orig == null || !Equals(getter(orig), compare);
        }
        return false;
    }
    
    public static bool ChangedFrom<T, V>(this IEditingContext<T> context, Func<T, V> getter, V compare)
    {
        var (orig, edited) = context.GetEntities();
        if (orig == null)
            return false;
        var oldValue = getter(orig);
        if (Equals(oldValue, compare))
        {
            return !Equals(getter(edited), compare);
        }
        return false;
    }

} 