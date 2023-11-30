using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;
using Astrolabe.JSON.Extensions;

namespace Astrolabe.Schemas;

public interface IControlVisitorContext
{
    JsonPathSegments JsonContext { get; }
    
    IControlVisitorContext WithFieldContext();
    object? FindAttribute(object key);

    static readonly JsonSerializerOptions Options = new JsonSerializerOptions().AddStandardOptions();
}
public record ControlVisitorContext<T>(string NodeType, JsonObject JsonDefinition, IControlVisitorContext? Parent, JsonPathSegments JsonContext)
    : IControlVisitorContext
{
    private T? _definition;
    private IDictionary<object, object>? _attributes;

    private IDictionary<object, object> Attributes => _attributes ??= new Dictionary<object, object>();
    
    public void Set(object key, object value)
    {
        Attributes[key] = value;
    }
    
    public object? Get(object key)
    {
        if (_attributes == null)
            return null;
        return Attributes.TryGetValue(key, out var value) ? value : null;
    }

    public IControlVisitorContext WithFieldContext()
    {
        var fieldNode = JsonDefinition["compoundField"]?.GetValue<string>();
        if (string.IsNullOrEmpty(fieldNode))
            return this;
        var newOne = this with { Parent = this, NodeType = "CompoundGroup", JsonContext = JsonContext.Field(fieldNode)};
        newOne._definition = _definition;
        return newOne;
    }

    public T Definition => _definition ??= JsonDefinition.Deserialize<T>(IControlVisitorContext.Options)!;
    public object? FindAttribute(object key)
    {
        var haveIt = Get(key);
        if (haveIt != null)
            return haveIt;
        return Parent?.FindAttribute(key);
    }
}

public record ControlVisitor(Func<ControlVisitorContext<DataControlDefinition>, bool>? Data = null, 
    Func<ControlVisitorContext<DisplayControlDefinition>, bool>? Display = null, Func<ControlVisitorContext<ActionControlDefinition>, bool>? Action = null, 
    Func<ControlVisitorContext<GroupedControlsDefinition>, bool>? Group = null, Func<ControlVisitorContext<object>, bool>? Other = null)
{
    public bool VisitAll(IEnumerable<JsonNode?> controls, IControlVisitorContext? context = null)
    {
        foreach (var node in controls)
        {
            if (!Visit(node!.AsObject(), context))
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
            var ctx = new ControlVisitorContext<T>(controlType, control, context, context?.JsonContext ?? JsonPathSegments.Empty);
            return (visitFn?.Invoke(ctx) ?? true, ctx);
        }
    }
}

