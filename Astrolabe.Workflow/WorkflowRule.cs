namespace Astrolabe.Workflow;

public interface IWorkflowRule<in T, out TAction>
{
    TAction Action { get; }

    bool RuleMatch(T context);
}

public record WorkflowRule<T, TAction>(
    TAction Action,
    Func<T, bool>? When,
    IDictionary<string, object?>? Properties = null) : IWorkflowRule<T, TAction>
{
    public WorkflowRule<T, TAction> AndWhen(Func<T, bool> andWhen)
    {
        return When == null ? this with { When = andWhen } : this with { When = t => When(t) && andWhen(t) };
    }

    public bool RuleMatch(T context)
    {
        return When?.Invoke(context) ?? true;
    }
}