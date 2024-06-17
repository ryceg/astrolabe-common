namespace Astrolabe.Workflow;

public abstract class AbstractWorkflowExecutor<TContext, TLoadContext, TAction>
{
    protected abstract Task<IEnumerable<TContext>> LoadData(TLoadContext loadContext);

    protected abstract Task ApplyChanges(TContext context);

    protected abstract Task<TContext> PerformAction(TContext context, TAction action);
}