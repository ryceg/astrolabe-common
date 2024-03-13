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
public abstract record ControlDefinition([property: SchemaOptions(typeof(ControlDefinitionType))] string Type)
{
    public string? Title { get; set; }
    
    public IEnumerable<DynamicProperty>? Dynamic { get; set; }
    
    public IEnumerable<ControlAdornment>? Adornments { get; set; }

    [SchemaTag(SchemaTags.NoControl)] 
    public IEnumerable<ControlDefinition>? Children { get; set; }
    
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
    
}

public record DataControlDefinition([property: SchemaTag(SchemaTags.SchemaField)] string Field) : ControlDefinition(ControlDefinitionType.Data.ToString())
{
    [DefaultValue(false)]
    public bool? Required { get; set; }
    
    public RenderOptions? RenderOptions { get; set; }
    
    public object? DefaultValue { get; set; }
    
    [DefaultValue(false)]
    public bool? Readonly { get; set; }
    
    public IEnumerable<SchemaValidator>? Validators { get; set; }
}

public record GroupedControlsDefinition() : ControlDefinition(ControlDefinitionType.Group.ToString()) 
{
    public static readonly ControlDefinition Default = new GroupedControlsDefinition();

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
    [Display(Name = "Date/Time")] DateTime,
    [Display(Name = "Checkbox")] Checkbox,
    [Display(Name = "Dropdown")] Dropdown,
}

[JsonBaseType("type", typeof(SimpleRenderOptions))]
[JsonSubType("HtmlEditor", typeof(HtmlEditorRenderOptions))]
[JsonSubType("IconList", typeof(IconListRenderOptions))]
[JsonSubType("Synchronised", typeof(SynchronisedRenderOptions))]
[JsonSubType("UserSelection", typeof(UserSelectionRenderOptions))]
[JsonSubType("DateTime", typeof(DateTimeRenderOptions))]
public abstract record RenderOptions([property: DefaultValue("Standard")] [property: SchemaOptions(typeof(DataRenderType))] string Type)
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
public abstract record DisplayData([property: SchemaOptions(typeof(DisplayDataType))] string Type)
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

public record DynamicProperty([property: SchemaOptions(typeof(DynamicPropertyType))] string Type, EntityExpression Expr);

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
public abstract record GroupRenderOptions([property: SchemaOptions(typeof(GroupRenderType))] [property: DefaultValue("Standard")] string Type)
{
    public bool? HideTitle { get; set; }
}

public record SimpleGroupRenderOptions(string Type) : GroupRenderOptions(Type);

public record GridRenderer(int? Columns) : GroupRenderOptions(GroupRenderType.Grid.ToString());

public record GroupElementRenderer([property: SchemaTag(SchemaTags.DefaultValue)] object Value) : GroupRenderOptions(GroupRenderType.GroupElement.ToString());


[JsonString]
public enum AdornmentPlacement {
    [Display(Name = "Start of control")] 
    ControlStart,
    [Display(Name = "End of control")]
    ControlEnd,
    [Display(Name = "Start of label")]
    LabelStart,
    [Display(Name = "End of label")]
    LabelEnd,
}

[JsonString]
public enum ControlAdornmentType
{
    Tooltip,
    Accordion,
    [Display(Name = "Help Text")]
    HelpText
}

[JsonBaseType("type", typeof(TooltipAdornment))]
[JsonSubType("Tooltip", typeof(TooltipAdornment))]
[JsonSubType("Accordion", typeof(AccordionAdornment))]
[JsonSubType("HelpText", typeof(HelpTextAdornment))]
public abstract record ControlAdornment([property: SchemaOptions(typeof(ControlAdornmentType))] string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record TooltipAdornment(string Tooltip) : ControlAdornment(ControlAdornmentType.Tooltip.ToString());

public record AccordionAdornment(string Title, bool DefaultExpanded) : ControlAdornment(ControlAdornmentType.Accordion.ToString());

public record HelpTextAdornment(string HelpText, AdornmentPlacement? Placement) : ControlAdornment(ControlAdornmentType.HelpText.ToString());
