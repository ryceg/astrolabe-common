namespace Astrolabe.Workflow;

public class WorkflowRules<T, TAction> 
    where T : class 
    where TAction : notnull
{
    public List<IWorkflowRule<T, TAction>> Rules { get; }
    public Dictionary<TAction, IWorkflowRule<T, TAction>> ActionMap { get; set; }

    public WorkflowRules(List<IWorkflowRule<T, TAction>> rules)
    {
        Rules = rules;
        ActionMap = rules.SelectMany(r => r.Actions.Select(a => (a, r))).ToDictionary(x => x.a, x => x.r);
    }


    public static ActionRule<T, TAction> OnSave(TAction action)
    {
        return new ActionRule<T, TAction>(WorkflowRuleType.Save, action);
    }
    
    public static ActionRule<T, TAction> UserAction(TAction action)
    {
        return new ActionRule<T, TAction>(WorkflowRuleType.User, action);
    }

    public static ActionRule<T, TAction> OnCreate(TAction action)
    {
        return new ActionRule<T, TAction>(WorkflowRuleType.Create, action);
    }

    public List<TAction> GetUserActions(T? context) =>
        FilterRules(context, Rules.Where(x => x.Type == WorkflowRuleType.User));

    public static List<TAction> FilterRules(T? context, IEnumerable<IWorkflowRule<T, TAction>> rules) =>
        rules.SelectMany(x => context == null || x.RuleApplies(context) ? x.Actions : Array.Empty<TAction>()).ToList();

    public List<TAction> GetSaveActions(T? context) =>
        FilterRules(context, Rules.Where(x => x.Type == WorkflowRuleType.Save));

    public List<TAction> GetCreateActions(T? context) =>
        FilterRules(context, Rules.Where(x => x.Type is WorkflowRuleType.Save or WorkflowRuleType.Create));
}

public static class WorkflowRuleExtensions
{
    public static ActionRuleWhen<T, TAction> When<T, TAction>(this ActionRule<T, TAction> rule, Func<T, bool> when)
    {
        return new ActionRuleWhen<T, TAction>(rule.Type, rule.Action, when);
    }

    public static bool RuleApplies<T, TAction>(this IWorkflowRule<T, TAction> rule, T context)
    {
        return rule switch
        {
            ActionRuleWhen<T, TAction> ar => ar.CanPerform(context),
            _ => true
        };
    }
}