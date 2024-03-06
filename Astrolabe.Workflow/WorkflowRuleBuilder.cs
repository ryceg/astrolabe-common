namespace Astrolabe.Workflow;

public class WorkflowRuleBuilder<T, TAction> 
    where T : class 
    where TAction : notnull
{
    public WorkflowRule<T, TAction> Action(TAction action) => new(action, _ => true);

    public WorkflowRule<T, TAction> ActionWhen(TAction action, Func<T, bool> when) => new(action, when);
}
