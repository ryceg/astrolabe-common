using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON.Extensions;

namespace Astrolabe.Schemas;

public interface IControlVisitorContext
{
    string FieldContext { get; }
    
    IControlVisitorContext WithFieldContext();
}
public record ControlVisitorContext<T>(JsonObject JsonDefinition, IControlVisitorContext? Parent, string FieldContext)
    : IControlVisitorContext
{
    private T? _definition;
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions().AddStandardOptions();
    
    public IControlVisitorContext WithFieldContext()
    {
        var fieldNode = JsonDefinition["compoundField"]?.GetValue<string>();
        if (string.IsNullOrEmpty(fieldNode))
            return this;
        return this with { FieldContext = FieldContext.Length == 0 ? fieldNode : $"{FieldContext}/{fieldNode}" };
    }

    public T Definition => _definition ??= JsonDefinition.Deserialize<T>(Options)!;
}

public record ControlVisitor(Func<ControlVisitorContext<DataControlDefinition>, bool>? Data = null, 
    Func<ControlVisitorContext<DisplayControlDefinition>, bool>? Display = null, Func<ControlVisitorContext<ActionControlDefinition>, bool>? Action = null, 
    Func<ControlVisitorContext<GroupedControlsDefinition>, bool>? Group = null, Func<ControlVisitorContext<object>, bool>? Other = null)
{
    public bool VisitAll(IEnumerable<JsonNode?> controls, IControlVisitorContext? context = null)
    {
        foreach (var node in controls)
        {
            if (!Visit(node!.AsObject()))
                return false;
        }
        return true;
    }

    
    public bool Visit(JsonObject control, IControlVisitorContext? context = null)
    {
        var controlType = control["type"]!.GetValue<string>();
        var (keepGoing, myContext) = controlType switch
        {
            "Data" => RunVisitor(Data),
            "Display" => RunVisitor(Display),
            "Group" => RunVisitor(Group),
            "Action" => RunVisitor(Action),
            _ => RunVisitor(Other)
        };
        if (keepGoing && controlType == "Group")
        {
            var children = control["children"]!.AsArray();
            return VisitAll(children, myContext.WithFieldContext());
        }
        return keepGoing;
        
        (bool, IControlVisitorContext) RunVisitor<T>(Func<ControlVisitorContext<T>, bool>? visitFn)
        {
            var ctx = new ControlVisitorContext<T>(control, context, context?.FieldContext ?? "");
            return (visitFn?.Invoke(ctx) ?? true, ctx);
        }
    }
}

