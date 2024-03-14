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

    public static (JsonNode?, SchemaField)? FindChildField(this ControlDefinition definition, JsonObject data,
        IEnumerable<SchemaField> fields)
    {
        var childField = definition switch
        {
            DataControlDefinition { Field: var field } => field,
            GroupedControlsDefinition { CompoundField: { } field } => field,
            _ => null
        };
        if (childField != null && fields.FirstOrDefault(x => x.Field == childField) is { } childSchema)
        {
            return (data[childField], childSchema);
        }
        return null;
    }
}