namespace Astrolabe.Workflow;

public interface IWorkflowRule<T, TAction> 
{ 
    WorkflowRuleType Type { get; }
    
    IEnumerable<TAction> Actions { get; }
}
public record ActionRule<T, TAction>(WorkflowRuleType Type, TAction Action) : IWorkflowRule<T, TAction>
{
    public IEnumerable<TAction> Actions => new [] { Action };
}

public record ActionRuleWhen<T, TAction>(WorkflowRuleType Type, TAction Action, Func<T, bool> CanPerform)
    : IWorkflowRule<T, TAction>
{
    public IEnumerable<TAction> Actions => new [] { Action };
}