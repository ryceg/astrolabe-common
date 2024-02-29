namespace Astrolabe.Workflow;

public interface IWorkflowEditContext<T>
{
    T? Original { get; }
    
    T Edited { get; }
    
}

public record SimpleWorkflowEditContext<T>(T? Original, T Edited) : IWorkflowEditContext<T>;

public static class WorkflowEditContextExtensions
{
    public static bool ChangedTo<T, V>(this IWorkflowEditContext<T> context, Func<T, V> getter, V compare)
    {
        var newValue = getter(context.Edited);
        if (Equals(newValue, compare))
        {
            return context.Original == null || !Equals(getter(context.Original), compare);
        }
        return false;
    }
    
    public static bool ChangedFrom<T, V>(this IWorkflowEditContext<T> context, Func<T, V> getter, V compare)
    {
        if (context.Original == null)
            return false;
        var newValue = getter(context.Original);
        if (!Equals(newValue, compare))
        {
            return !Equals(getter(context.Edited), compare);
        }
        return false;
    }

} 