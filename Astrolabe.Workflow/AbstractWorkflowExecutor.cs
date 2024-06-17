namespace Astrolabe.Workflow;

public abstract class AbstractWorkflowExecutor<TContext, TLoadContext, TAction> 
    where TContext : IWorkflowActionList<TContext, TAction>
{
    protected abstract Task<IEnumerable<TContext>> LoadData(TLoadContext loadContext);

    protected async Task<TContext> ApplyChanges(TContext context)
    {
        context = await context.PerformActions(PerformAction);
        return await AfterActions(context);
    }

    protected virtual Task<TContext> AfterActions(TContext context)
    {
        return Task.FromResult(context);
    }

    protected abstract Task<TContext> PerformAction(TContext context, TAction action);
}