using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas;

[JsonString]
public enum ControlDefinitionType
{
    Data,
    Group,
    Display,
    Action
}

[JsonBaseType("type", typeof(DataControlDefinition))]
[JsonSubType("Data", typeof(DataControlDefinition))]
[JsonSubType("Group", typeof(GroupedControlsDefinition))]
[JsonSubType("Display", typeof(DisplayControlDefinition))]
[JsonSubType("Action", typeof(ActionControlDefinition))]
public abstract record ControlDefinition(string Type)
{
    public string? Title { get; set; }
    
    public IEnumerable<DynamicProperty>? Dynamic { get; set; }
    
    public IEnumerable<ControlAdornment>? Adornments { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record DataControlDefinition(string Field) : ControlDefinition(ControlDefinitionType.Data.ToString())
{
    public bool? Required { get; set; }
    
    public RenderOptions? RenderOptions { get; set; }
    
    public object? DefaultValue { get; set; }
    
    public bool? Readonly { get; set; }
}

public record GroupedControlsDefinition([property: SchemaTag(SchemaTags.NoControl)]  IEnumerable<ControlDefinition> Children) : ControlDefinition(ControlDefinitionType.Group.ToString()) 
{
    public static readonly ControlDefinition Default = new GroupedControlsDefinition(Array.Empty<ControlDefinition>());

    [SchemaTag(SchemaTags.NestedSchemaField)]
    public string? CompoundField { get; set; }
    
    public GroupRenderOptions? GroupOptions { get; set; }
}

public record DisplayControlDefinition(DisplayData DisplayData) : ControlDefinition(ControlDefinitionType.Display
    .ToString());

public record ActionControlDefinition(string ActionId) : ControlDefinition(ControlDefinitionType.Action.ToString());

[JsonString]
public enum DataRenderType
{
    [Display(Name = "Default")] Standard,
    [Display(Name = "Radio buttons")] Radio,
    [Display(Name = "HTML Editor")] HtmlEditor,
    [Display(Name = "Icon list")] IconList,
    [Display(Name = "Check list")] CheckList,
    [Display(Name = "User Selection")] UserSelection,
    [Display(Name = "Synchronised Fields")] Synchronised,
    [Display(Name = "Icon Selection")] IconSelector,
    [Display(Name = "Date/Time")] DateTime
}

[JsonBaseType("type", typeof(SimpleRenderOptions))]
[JsonSubType("HtmlEditor", typeof(HtmlEditorRenderOptions))]
[JsonSubType("IconList", typeof(IconListRenderOptions))]
[JsonSubType("Synchronised", typeof(SynchronisedRenderOptions))]
[JsonSubType("UserSelection", typeof(UserSelectionRenderOptions))]
[JsonSubType("DateTime", typeof(DateTimeRenderOptions))]
public abstract record RenderOptions([property: DefaultValue("Standard")] string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record SimpleRenderOptions(string Type) : RenderOptions(Type);

public record UserSelectionRenderOptions(bool NoGroups, bool NoUsers) : RenderOptions(DataRenderType.UserSelection.ToString());

public record DateTimeRenderOptions(string? Format) : RenderOptions(DataRenderType.DateTime.ToString());

public record SynchronisedRenderOptions([property: SchemaTag(SchemaTags.SchemaField)] string FieldToSync, SyncTextType SyncType) : RenderOptions(DataRenderType.Synchronised.ToString());

[JsonString]
public enum SyncTextType
{
    Camel,
    Snake,
    Pascal,
}
public record IconListRenderOptions(IEnumerable<IconMapping> IconMappings) : RenderOptions(DataRenderType.IconList.ToString());

public record HtmlEditorRenderOptions(bool AllowImages) : RenderOptions(DataRenderType.HtmlEditor.ToString());

public record IconMapping(string Value, string? MaterialIcon);

[JsonString]
public enum DisplayDataType
{
    Text,
    Html,
}

[JsonBaseType("type", typeof(SimpleDisplayData))]
[JsonSubType("Text", typeof(TextDisplay))]
[JsonSubType("Html", typeof(HtmlDisplay))]
public abstract record DisplayData(string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record SimpleDisplayData(string Type) : DisplayData(Type);

public record TextDisplay(string Text) : DisplayData(DisplayDataType.Text.ToString());

public record HtmlDisplay([property: SchemaTag(SchemaTags.HtmlEditor)] string Html) : DisplayData(DisplayDataType.Html.ToString());

[JsonString]
public enum DynamicPropertyType
{
    Visible,
    DefaultValue
}

public record DynamicProperty(string Type, EntityExpression Expr);

[JsonString]
public enum GroupRenderType
{
    Standard,
    Grid,
    GroupElement,
}

[JsonBaseType("type", typeof(SimpleGroupRenderOptions))]
[JsonSubType("Standard", typeof(SimpleGroupRenderOptions))]
[JsonSubType("GroupElement", typeof(GroupElementRenderer))]
[JsonSubType("Grid", typeof(GridRenderer))]
public abstract record GroupRenderOptions([property: DefaultValue("Standard")] string Type)
{
    public bool? HideTitle { get; set; }
}

public record SimpleGroupRenderOptions(string Type) : GroupRenderOptions(Type);

public record GridRenderer(int? Columns) : GroupRenderOptions(GroupRenderType.Grid.ToString());

public record GroupElementRenderer([property: SchemaTag(SchemaTags.DefaultValue)] object Value) : GroupRenderOptions(GroupRenderType.GroupElement.ToString());

[JsonString]
public enum ControlAdornmentType
{
    Tooltip,
    Accordion
}

[JsonBaseType("type", typeof(TooltipAdornment))]
[JsonSubType("Tooltip", typeof(TooltipAdornment))]
[JsonSubType("Accordion", typeof(AccordionAdornment))]
public abstract record ControlAdornment(string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record TooltipAdornment(string Tooltip) : ControlAdornment(ControlAdornmentType.Tooltip.ToString());

public record AccordionAdornment(string Title, bool DefaultExpanded) : ControlAdornment(ControlAdornmentType.Accordion.ToString());
