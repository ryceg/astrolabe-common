namespace Astrolabe.Workflow;

public interface IWorkflowActionList<TContext, TAction>
{
    (ICollection<TAction>, TContext) NextActions();
}

public static class WorkflowActionListExtensions
{
    public static async Task<TContext> PerformActions<TContext, TAction>(this IWorkflowActionList<TContext, TAction> actionList, 
        Func<TContext, TAction, Task<TContext>> performAction) where TContext : IWorkflowActionList<TContext, TAction>
    {
        do
        {
            var (nextActions, context) = actionList.NextActions();
            if (!nextActions.Any())
                return context;
            foreach (var action in nextActions)
            {
                context = await performAction(context, action);
            }
            actionList = context;
        } while (true);
    }
}