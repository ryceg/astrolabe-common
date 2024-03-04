namespace Astrolabe.Workflow;

public class WorkflowActionRunner
{
    public static async Task<TContext> PerformActions<TContext, TAction>(TContext context, 
        Func<TContext, IEnumerable<TAction>> getActions, Func<TContext, TContext> withNoActions, 
        Func<TContext, TAction, Task<TContext>> performAction)
    {
        do
        {
            var nextActions = getActions(context);
            if (!nextActions.Any())
                return context;
            context = withNoActions(context);
            foreach (var action in nextActions)
            {
                context = await performAction(context, action);
            }
        } while (true);
    }
}