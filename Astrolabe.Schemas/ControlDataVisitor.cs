using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Astrolabe.JSON;

namespace Astrolabe.Schemas;

public enum VisitorResult
{
    Continue,
    Skip,
    Stop
}

public delegate VisitorResult DataVisitor<in TJson, in TField>(DataControlDefinition dataControl, TJson data,
    TField field,
    ControlDataVisitorContext context);

public record ControlDataVisitor(
    DataVisitor<JsonNode?, SimpleSchemaField>? Data = null,
    DataVisitor<JsonArray, SimpleSchemaField>? DataCollection = null,
    DataVisitor<JsonObject, CompoundField>? Compound = null,
    DataVisitor<JsonArray, CompoundField>? CompoundCollection = null,
    DataVisitor<JsonNode?, SchemaField>? AnyData = null,
    Func<ControlDefinition, JsonNode?, SchemaField, ControlDataVisitorContext, VisitorResult>? Other = null
);

public record ControlDataVisitorContext(
    ControlDefinition Control,
    SchemaField Field,
    bool Element,
    JsonNode? Data,
    JsonPathSegments Path,
    ControlDataVisitorContext? Parent)
{
    public static ControlDataVisitorContext RootContext(IEnumerable<ControlDefinition> controls,
        IEnumerable<SchemaField> fields, JsonObject data)
    {
        return new ControlDataVisitorContext(new GroupedControlsDefinition { Children = controls },
            new CompoundField("", fields, false), false, data, JsonPathSegments.Empty, null);
    }

    public ControlDataVisitorContext ChildContext(ControlDefinition childDef)
    {
        return this with { Control = childDef, Element = false, Parent = this };
    }

    public ControlDataVisitorContext? FindParent(Func<ControlDataVisitorContext, bool> matching)
    {
        var currentParent = Parent;
        while (currentParent != null)
        {
            if (matching(currentParent))
                return currentParent;
            currentParent = currentParent.Parent;
        }
        return null;
    }
}

public static class ControlDataVisitorExtensions
{
    public static VisitorResult Visit(this ControlDataVisitorContext context, ControlDataVisitor visitor)
    {
        var visitChildren = (context.Control, context.Field, context.Data) switch
        {
            (DataControlDefinition dcd, SimpleSchemaField { Collection: not true } ssf, var value) when visitor is
                { Data: { } df } => df(dcd, value, ssf, context),
            (DataControlDefinition dcd, SimpleSchemaField { Collection: true } ssf, JsonArray value) when
                !context.Element && visitor is
                    { DataCollection: { } df } => df(dcd, value, ssf, context),
            (DataControlDefinition dcd, CompoundField { Collection: not true } ssf, JsonObject value) when visitor is
                { Compound: { } df } => df(dcd, value, ssf, context),
            (DataControlDefinition dcd, CompoundField { Collection: true } ssf, JsonArray value) when
                !context.Element && visitor is
                    { CompoundCollection: { } df } => df(dcd, value, ssf, context),
            (DataControlDefinition dcd, _, _) when visitor is
                { AnyData: { } df } => df(dcd, context.Data, context.Field, context),
            var (c, f, d) when visitor is { Other: { } df } => df(c, d, f, context),
            _ => VisitorResult.Continue
        };
        if (visitChildren is VisitorResult.Stop or VisitorResult.Skip)
            return visitChildren;
        if ((context.Field.Collection ?? false) && !context.Element)
        {
            if (context.Data is not JsonArray ja) return VisitorResult.Continue;
            var i = 0;
            foreach (var child in ja)
            {
                var childContext = context with
                {
                    Element = true, Data = child, Parent = context, Path = context.Path.Index(i)
                };
                if (childContext.Visit(visitor) == VisitorResult.Stop)
                    return VisitorResult.Stop;
                i++;
            }

            return VisitorResult.Continue;
        }

        var childControls = context.Control.Children ?? Array.Empty<ControlDefinition>();
        if (context is not { Field: CompoundField cf, Data: JsonObject jsonData })
        {
            return childControls.Any(childControl =>
                context.ChildContext(childControl).Visit(visitor) == VisitorResult.Stop)
                ? VisitorResult.Stop
                : VisitorResult.Continue;
        }

        foreach (var childControl in childControls)
        {
            var childContext = childControl.FindChildField(jsonData, cf.Children) is var (childData, childField)
                ? new ControlDataVisitorContext(childControl, childField, false,
                    childData, context.Path.Field(childField.Field), context)
                : context.ChildContext(childControl);
            if (childContext.Visit(visitor) == VisitorResult.Stop)
                return VisitorResult.Stop;
        }

        return VisitorResult.Continue;
    }
}