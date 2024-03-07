namespace Astrolabe.Workflow;

public interface IWorkflowRule<out TAction, in T>
{
    TAction Action { get; }

    bool RuleMatch(T context);
    
    IDictionary<string, object?>? Properties { get; }
}

public record WorkflowAction<TAction>(
    TAction Action,
    IDictionary<string, object?>? Properties = null) : IWorkflowRule<TAction, object>
{
    public bool RuleMatch(object context) => true;
}

public record WorkflowActionWhen<TAction, T>(
    TAction Action,
    Func<T, bool> When,
    IDictionary<string, object?>? Properties = null) : IWorkflowRule<TAction, T>
{
    public bool RuleMatch(T context) => When(context);
}

public static class WorkflowRules
{
    public static IWorkflowRule<TAction, object> Action<TAction>(TAction action) => new WorkflowAction<TAction>(action);
    public static IWorkflowRule<TAction, T> ActionWhen<TAction, T>(TAction action, Func<T, bool> when) => new WorkflowActionWhen<TAction, T>(action, when);
}