using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Schemas;

public static class ControlDefinitionExtensions
{
    private static readonly string VisibilityType = DynamicPropertyType.Visible.ToString();
    public static bool IsVisible(this ControlDefinition definition, JsonObject data, JsonPathSegments context)
    {
        var dynamicVisibility = definition.Dynamic?.FirstOrDefault(x => x.Type == VisibilityType);
        if (dynamicVisibility == null)
            return true;
        return dynamicVisibility.Expr.EvalBool(data, context);
    }
}