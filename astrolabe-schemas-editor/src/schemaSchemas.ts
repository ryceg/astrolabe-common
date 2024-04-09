import {
  AdornmentPlacement,
  applyDefaultValues,
  buildSchema,
  ControlAdornment,
  ControlDefinition,
  DateComparison,
  defaultValueForFields,
  DisplayData,
  DynamicProperty,
  EntityExpression,
  FieldOption,
  FieldType,
  GroupRenderOptions,
  IconMapping,
  makeCompoundField,
  makeScalarField,
  RenderOptions,
  SchemaField,
  SchemaValidator,
  SyncTextType,
} from "@react-typed-forms/schemas";

export interface FieldOptionForm {
  name: string;
  value: any;
}

export const FieldOptionSchema = buildSchema<FieldOptionForm>({
  name: makeScalarField({
    type: FieldType.String,
    notNullable: true,
    required: true,
    displayName: "Name",
  }),
  value: makeScalarField({
    type: FieldType.Any,
    notNullable: true,
    required: true,
    displayName: "Value",
  }),
});

export const defaultFieldOptionForm: FieldOptionForm =
  defaultValueForFields(FieldOptionSchema);

export function toFieldOptionForm(v: FieldOption): FieldOptionForm {
  return applyDefaultValues(v, FieldOptionSchema);
}

export interface SchemaValidatorForm {
  type: string;
  expression: string;
  comparison: DateComparison;
  fixedDate: string | null;
  daysFromCurrent: number | null;
  min: number | null;
  max: number | null;
}

export const SchemaValidatorSchema = buildSchema<SchemaValidatorForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Jsonata",
        value: "Jsonata",
      },
      {
        name: "Date",
        value: "Date",
      },
      {
        name: "Length",
        value: "Length",
      },
    ],
  }),
  expression: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Jsonata"],
    notNullable: true,
    required: true,
    displayName: "Expression",
  }),
  comparison: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Date"],
    notNullable: true,
    required: true,
    displayName: "Comparison",
    options: [
      {
        name: "Not Before",
        value: "NotBefore",
      },
      {
        name: "Not After",
        value: "NotAfter",
      },
    ],
  }),
  fixedDate: makeScalarField({
    type: FieldType.Date,
    onlyForTypes: ["Date"],
    displayName: "FixedDate",
  }),
  daysFromCurrent: makeScalarField({
    type: FieldType.Int,
    onlyForTypes: ["Date"],
    displayName: "DaysFromCurrent",
  }),
  min: makeScalarField({
    type: FieldType.Int,
    onlyForTypes: ["Length"],
    displayName: "Min",
  }),
  max: makeScalarField({
    type: FieldType.Int,
    onlyForTypes: ["Length"],
    displayName: "Max",
  }),
});

export const defaultSchemaValidatorForm: SchemaValidatorForm =
  defaultValueForFields(SchemaValidatorSchema);

export function toSchemaValidatorForm(v: SchemaValidator): SchemaValidatorForm {
  return applyDefaultValues(v, SchemaValidatorSchema);
}

export interface SchemaFieldForm {
  type: string;
  field: string;
  displayName: string | null;
  system: boolean | null;
  tags: string[] | null;
  onlyForTypes: string[] | null;
  required: boolean | null;
  notNullable: boolean | null;
  collection: boolean | null;
  defaultValue: any | null;
  isTypeField: boolean | null;
  searchable: boolean | null;
  options: FieldOptionForm[] | null;
  validators: SchemaValidatorForm[] | null;
  entityRefType: string;
  parentField: string | null;
  children: SchemaFieldForm[];
  treeChildren: boolean | null;
}

export const SchemaFieldSchema = buildSchema<SchemaFieldForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "String",
        value: "String",
      },
      {
        name: "Bool",
        value: "Bool",
      },
      {
        name: "Int",
        value: "Int",
      },
      {
        name: "Date",
        value: "Date",
      },
      {
        name: "DateTime",
        value: "DateTime",
      },
      {
        name: "Double",
        value: "Double",
      },
      {
        name: "EntityRef",
        value: "EntityRef",
      },
      {
        name: "Compound",
        value: "Compound",
      },
      {
        name: "AutoId",
        value: "AutoId",
      },
      {
        name: "Image",
        value: "Image",
      },
      {
        name: "Any",
        value: "Any",
      },
    ],
  }),
  field: makeScalarField({
    type: FieldType.String,
    notNullable: true,
    required: true,
    displayName: "Field",
  }),
  displayName: makeScalarField({
    type: FieldType.String,
    displayName: "DisplayName",
  }),
  system: makeScalarField({
    type: FieldType.Bool,
    displayName: "System",
  }),
  tags: makeScalarField({
    type: FieldType.String,
    collection: true,
    displayName: "Tags",
  }),
  onlyForTypes: makeScalarField({
    type: FieldType.String,
    collection: true,
    displayName: "OnlyForTypes",
  }),
  required: makeScalarField({
    type: FieldType.Bool,
    displayName: "Required",
  }),
  notNullable: makeScalarField({
    type: FieldType.Bool,
    displayName: "NotNullable",
  }),
  collection: makeScalarField({
    type: FieldType.Bool,
    displayName: "Collection",
  }),
  defaultValue: makeScalarField({
    type: FieldType.Any,
    displayName: "DefaultValue",
  }),
  isTypeField: makeScalarField({
    type: FieldType.Bool,
    displayName: "IsTypeField",
  }),
  searchable: makeScalarField({
    type: FieldType.Bool,
    displayName: "Searchable",
  }),
  options: makeCompoundField({
    children: FieldOptionSchema,
    collection: true,
    displayName: "Options",
  }),
  validators: makeCompoundField({
    children: SchemaValidatorSchema,
    collection: true,
    displayName: "Validators",
  }),
  entityRefType: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["EntityRef"],
    notNullable: true,
    required: true,
    displayName: "EntityRefType",
  }),
  parentField: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["EntityRef"],
    displayName: "ParentField",
  }),
  children: makeCompoundField({
    treeChildren: true,
    collection: true,
    onlyForTypes: ["Compound"],
    notNullable: true,
    displayName: "Children",
  }),
  treeChildren: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["Compound"],
    displayName: "TreeChildren",
  }),
});

export const defaultSchemaFieldForm: SchemaFieldForm =
  defaultValueForFields(SchemaFieldSchema);

export function toSchemaFieldForm(v: SchemaField): SchemaFieldForm {
  return applyDefaultValues(v, SchemaFieldSchema);
}

export interface EntityExpressionForm {
  type: string;
  expression: string;
  field: string;
  value: any;
  userMatch: string;
}

export const EntityExpressionSchema = buildSchema<EntityExpressionForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Jsonata",
        value: "Jsonata",
      },
      {
        name: "Data Match",
        value: "FieldValue",
      },
      {
        name: "UserMatch",
        value: "UserMatch",
      },
      {
        name: "Data",
        value: "Data",
      },
    ],
  }),
  expression: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Jsonata"],
    notNullable: true,
    required: true,
    displayName: "Expression",
  }),
  field: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["FieldValue", "Data"],
    notNullable: true,
    required: true,
    displayName: "Field",
    tags: ["_SchemaField"],
  }),
  value: makeScalarField({
    type: FieldType.Any,
    onlyForTypes: ["FieldValue"],
    notNullable: true,
    required: true,
    displayName: "Value",
    tags: ["_ValuesOf:field"],
  }),
  userMatch: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["UserMatch"],
    notNullable: true,
    required: true,
    displayName: "UserMatch",
  }),
});

export const defaultEntityExpressionForm: EntityExpressionForm =
  defaultValueForFields(EntityExpressionSchema);

export function toEntityExpressionForm(
  v: EntityExpression,
): EntityExpressionForm {
  return applyDefaultValues(v, EntityExpressionSchema);
}

export interface DynamicPropertyForm {
  type: string;
  expr: EntityExpressionForm;
}

export const DynamicPropertySchema = buildSchema<DynamicPropertyForm>({
  type: makeScalarField({
    type: FieldType.String,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Visible",
        value: "Visible",
      },
      {
        name: "DefaultValue",
        value: "DefaultValue",
      },
      {
        name: "Readonly",
        value: "Readonly",
      },
      {
        name: "Disabled",
        value: "Disabled",
      },
      {
        name: "Display",
        value: "Display",
      },
    ],
  }),
  expr: makeCompoundField({
    children: EntityExpressionSchema,
    notNullable: true,
    displayName: "Expr",
  }),
});

export const defaultDynamicPropertyForm: DynamicPropertyForm =
  defaultValueForFields(DynamicPropertySchema);

export function toDynamicPropertyForm(v: DynamicProperty): DynamicPropertyForm {
  return applyDefaultValues(v, DynamicPropertySchema);
}

export interface ControlAdornmentForm {
  type: string;
  tooltip: string;
  title: string;
  defaultExpanded: boolean;
  helpText: string;
  placement: AdornmentPlacement | null;
  iconClass: string;
}

export const ControlAdornmentSchema = buildSchema<ControlAdornmentForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Tooltip",
        value: "Tooltip",
      },
      {
        name: "Accordion",
        value: "Accordion",
      },
      {
        name: "Help Text",
        value: "HelpText",
      },
      {
        name: "Icon",
        value: "Icon",
      },
    ],
  }),
  tooltip: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Tooltip"],
    notNullable: true,
    required: true,
    displayName: "Tooltip",
  }),
  title: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Accordion"],
    notNullable: true,
    required: true,
    displayName: "Title",
  }),
  defaultExpanded: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["Accordion"],
    notNullable: true,
    required: true,
    displayName: "DefaultExpanded",
  }),
  helpText: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["HelpText"],
    notNullable: true,
    required: true,
    displayName: "HelpText",
  }),
  iconClass: makeScalarField({
    type: FieldType.String,
    displayName: "Icon class",
    onlyForTypes: ["Icon"],
  }),
  placement: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["HelpText", "Icon"],
    displayName: "Placement",
    options: [
      {
        name: "Start of control",
        value: "ControlStart",
      },
      {
        name: "End of control",
        value: "ControlEnd",
      },
      {
        name: "Start of label",
        value: "LabelStart",
      },
      {
        name: "End of label",
        value: "LabelEnd",
      },
    ],
  }),
});

export const defaultControlAdornmentForm: ControlAdornmentForm =
  defaultValueForFields(ControlAdornmentSchema);

export function toControlAdornmentForm(
  v: ControlAdornment,
): ControlAdornmentForm {
  return applyDefaultValues(v, ControlAdornmentSchema);
}

export interface IconMappingForm {
  value: string;
  materialIcon: string | null;
}

export const IconMappingSchema = buildSchema<IconMappingForm>({
  value: makeScalarField({
    type: FieldType.String,
    notNullable: true,
    required: true,
    displayName: "Value",
  }),
  materialIcon: makeScalarField({
    type: FieldType.String,
    displayName: "MaterialIcon",
  }),
});

export const defaultIconMappingForm: IconMappingForm =
  defaultValueForFields(IconMappingSchema);

export function toIconMappingForm(v: IconMapping): IconMappingForm {
  return applyDefaultValues(v, IconMappingSchema);
}

export interface RenderOptionsForm {
  type: string;
  noGroups: boolean;
  noUsers: boolean;
  format: string | null;
  fieldToSync: string;
  syncType: SyncTextType;
  iconMappings: IconMappingForm[];
  allowImages: boolean;
  emptyText: string | null;
  sampleText: string | null;
}

export const RenderOptionsSchema = buildSchema<RenderOptionsForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    defaultValue: "Standard",
    displayName: "Type",
    options: [
      {
        name: "Default",
        value: "Standard",
      },
      {
        name: "Radio buttons",
        value: "Radio",
      },
      {
        name: "HTML Editor",
        value: "HtmlEditor",
      },
      {
        name: "Icon list",
        value: "IconList",
      },
      {
        name: "Check list",
        value: "CheckList",
      },
      {
        name: "User Selection",
        value: "UserSelection",
      },
      {
        name: "Synchronised Fields",
        value: "Synchronised",
      },
      {
        name: "Icon Selection",
        value: "IconSelector",
      },
      {
        name: "Date/Time",
        value: "DateTime",
      },
      {
        name: "Checkbox",
        value: "Checkbox",
      },
      {
        name: "Dropdown",
        value: "Dropdown",
      },
      {
        name: "Display Only",
        value: "DisplayOnly",
      },
    ],
  }),
  noGroups: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["UserSelection"],
    notNullable: true,
    required: true,
    displayName: "NoGroups",
  }),
  noUsers: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["UserSelection"],
    notNullable: true,
    required: true,
    displayName: "NoUsers",
  }),
  format: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["DateTime"],
    displayName: "Format",
  }),
  emptyText: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["DisplayOnly"],
    displayName: "Empty Text",
  }),
  sampleText: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["DisplayOnly"],
    displayName: "Sample Text",
  }),
  fieldToSync: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Synchronised"],
    notNullable: true,
    required: true,
    displayName: "FieldToSync",
    tags: ["_SchemaField"],
  }),
  syncType: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Synchronised"],
    notNullable: true,
    required: true,
    displayName: "SyncType",
    options: [
      {
        name: "Camel",
        value: "Camel",
      },
      {
        name: "Snake",
        value: "Snake",
      },
      {
        name: "Pascal",
        value: "Pascal",
      },
    ],
  }),
  iconMappings: makeCompoundField({
    children: IconMappingSchema,
    collection: true,
    onlyForTypes: ["IconList"],
    notNullable: true,
    displayName: "IconMappings",
  }),
  allowImages: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["HtmlEditor"],
    notNullable: true,
    required: true,
    displayName: "AllowImages",
  }),
});

export const defaultRenderOptionsForm: RenderOptionsForm =
  defaultValueForFields(RenderOptionsSchema);

export function toRenderOptionsForm(v: RenderOptions): RenderOptionsForm {
  return applyDefaultValues(v, RenderOptionsSchema);
}

export interface GroupRenderOptionsForm {
  type: string;
  hideTitle: boolean | null;
  columns: number | null;
  value: any;
}

export const GroupRenderOptionsSchema = buildSchema<GroupRenderOptionsForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    defaultValue: "Standard",
    displayName: "Type",
    options: [
      {
        name: "Standard",
        value: "Standard",
      },
      {
        name: "Grid",
        value: "Grid",
      },
      {
        name: "GroupElement",
        value: "GroupElement",
      },
    ],
  }),
  hideTitle: makeScalarField({
    type: FieldType.Bool,
    displayName: "HideTitle",
  }),
  columns: makeScalarField({
    type: FieldType.Int,
    onlyForTypes: ["Grid"],
    displayName: "Columns",
  }),
  value: makeScalarField({
    type: FieldType.Any,
    onlyForTypes: ["GroupElement"],
    notNullable: true,
    required: true,
    displayName: "Value",
    tags: ["_DefaultValue"],
  }),
});

export const defaultGroupRenderOptionsForm: GroupRenderOptionsForm =
  defaultValueForFields(GroupRenderOptionsSchema);

export function toGroupRenderOptionsForm(
  v: GroupRenderOptions,
): GroupRenderOptionsForm {
  return applyDefaultValues(v, GroupRenderOptionsSchema);
}

export interface DisplayDataForm {
  type: string;
  text: string;
  html: string;
}

export const DisplayDataSchema = buildSchema<DisplayDataForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Text",
        value: "Text",
      },
      {
        name: "Html",
        value: "Html",
      },
    ],
  }),
  text: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Text"],
    notNullable: true,
    required: true,
    displayName: "Text",
  }),
  html: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Html"],
    notNullable: true,
    required: true,
    displayName: "Html",
    tags: ["_HtmlEditor"],
  }),
});

export const defaultDisplayDataForm: DisplayDataForm =
  defaultValueForFields(DisplayDataSchema);

export function toDisplayDataForm(v: DisplayData): DisplayDataForm {
  return applyDefaultValues(v, DisplayDataSchema);
}

export interface ControlDefinitionForm {
  type: string;
  title: string | null;
  dynamic: DynamicPropertyForm[] | null;
  adornments: ControlAdornmentForm[] | null;
  children: ControlDefinitionForm[] | null;
  field: string;
  hideTitle: boolean | null;
  required: boolean | null;
  renderOptions: RenderOptionsForm | null;
  defaultValue: any | null;
  readonly: boolean | null;
  validators: SchemaValidatorForm[] | null;
  compoundField: string | null;
  groupOptions: GroupRenderOptionsForm | null;
  displayData: DisplayDataForm;
  actionId: string;
  styleClass: string | null;
  layoutClass: string | null;
}

export const ControlDefinitionSchema = buildSchema<ControlDefinitionForm>({
  type: makeScalarField({
    type: FieldType.String,
    isTypeField: true,
    notNullable: true,
    required: true,
    displayName: "Type",
    options: [
      {
        name: "Data",
        value: "Data",
      },
      {
        name: "Group",
        value: "Group",
      },
      {
        name: "Display",
        value: "Display",
      },
      {
        name: "Action",
        value: "Action",
      },
    ],
  }),
  title: makeScalarField({
    type: FieldType.String,
    displayName: "Title",
  }),
  dynamic: makeCompoundField({
    children: DynamicPropertySchema,
    collection: true,
    displayName: "Dynamic",
  }),
  adornments: makeCompoundField({
    children: ControlAdornmentSchema,
    collection: true,
    displayName: "Adornments",
  }),
  children: makeCompoundField({
    treeChildren: true,
    collection: true,
    displayName: "Children",
    tags: ["_NoControl"],
  }),
  field: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Data"],
    notNullable: true,
    required: true,
    displayName: "Field",
    tags: ["_SchemaField"],
  }),
  hideTitle: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["Data"],
    displayName: "HideTitle",
  }),
  required: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["Data"],
    defaultValue: false,
    displayName: "Required",
  }),
  renderOptions: makeCompoundField({
    children: RenderOptionsSchema,
    onlyForTypes: ["Data"],
    displayName: "RenderOptions",
  }),
  defaultValue: makeScalarField({
    type: FieldType.Any,
    onlyForTypes: ["Data"],
    displayName: "DefaultValue",
  }),
  readonly: makeScalarField({
    type: FieldType.Bool,
    onlyForTypes: ["Data"],
    defaultValue: false,
    displayName: "Readonly",
  }),
  validators: makeCompoundField({
    children: SchemaValidatorSchema,
    collection: true,
    onlyForTypes: ["Data"],
    displayName: "Validators",
  }),
  compoundField: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Group"],
    displayName: "CompoundField",
    tags: ["_NestedSchemaField"],
  }),
  groupOptions: makeCompoundField({
    children: GroupRenderOptionsSchema,
    onlyForTypes: ["Group"],
    displayName: "GroupOptions",
  }),
  displayData: makeCompoundField({
    children: DisplayDataSchema,
    onlyForTypes: ["Display"],
    notNullable: true,
    displayName: "DisplayData",
  }),
  actionId: makeScalarField({
    type: FieldType.String,
    onlyForTypes: ["Action"],
    notNullable: true,
    required: true,
    displayName: "ActionId",
  }),
  styleClass: makeScalarField({
    type: FieldType.String,
    displayName: "StyleClass",
  }),
  layoutClass: makeScalarField({
    type: FieldType.String,
    displayName: "LayoutClass",
  }),
});

export const defaultControlDefinitionForm: ControlDefinitionForm =
  defaultValueForFields(ControlDefinitionSchema);

export function toControlDefinitionForm(
  v: ControlDefinition,
): ControlDefinitionForm {
  return applyDefaultValues(v, ControlDefinitionSchema);
}
