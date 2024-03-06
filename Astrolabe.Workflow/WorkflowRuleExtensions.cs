namespace Astrolabe.Workflow;

public delegate bool ActionAllowed<in T, in TAction>(T context, TAction action);

public static class WorkflowRuleExtensions
{
    public static ActionAllowed<T, TAction> RuleMatcher<T, TAction>(
        this IEnumerable<IWorkflowRule<T, TAction>> rules) where TAction : notnull
    {
        var actionMap = rules.ToDictionary(x => x.Action);
        return (context, action) => actionMap[action].RuleMatch(context);
    }

    public static IEnumerable<TAction> ActionsFor<T, TAction>(this IEnumerable<IWorkflowRule<T, TAction>> rules,
        T context)
    {
        return rules.Where(x => x.RuleMatch(context)).Select(x => x.Action);
    }
    
    public static IEnumerable<TAction> AllActions<T, TAction>(this IEnumerable<IWorkflowRule<T, TAction>> rules)
    {
        return rules.Select(x => x.Action);
    }

}