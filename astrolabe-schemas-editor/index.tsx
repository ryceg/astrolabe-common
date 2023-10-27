import { Control, newControl } from "@react-typed-forms/core";
import {
  ActionControlDefinition,
  ActionControlProperties,
  ControlDefinitionType,
  DataControlDefinition,
  DataRendererProps,
  DataRenderType,
  defaultValueForFields,
  fieldHasTag,
  FieldOption,
  FormEditHooks,
  FormEditState,
  getOptionsForScalarField,
  GridRenderer,
  GroupControlProperties,
  GroupedControlsDefinition,
  GroupRenderType,
  isCompoundField,
  isScalarField,
  renderControl,
  SchemaField,
  useIsControlVisible,
} from "@react-typed-forms/schemas";
import {
  ControlDefinitionForm,
  defaultControlDefinitionForm,
  defaultSchemaFieldForm,
  SchemaFieldForm,
} from "./schemaSchemas";
import { ReactElement, useEffect, useMemo } from "react";

export type ControlForm = Control<ControlDefinitionForm>;

export interface ControlDragState {
  draggedFrom?: [Control<any>, number];
  targetIndex: number;
  draggedControl: ControlForm;
  targetParent: ControlForm;
  dragFields?: Control<SchemaFieldForm[]>;
}

export interface DragData {
  overlay: (dd: DragData) => ReactElement;
}

export interface DropData {
  success: (drag: DragData, drop: DropData) => void;
}

export interface ControlDropData extends DropData {
  parent?: ControlForm;
  dropIndex: number;
}

export const NonExistentField = newControl<SchemaFieldForm>(
  defaultSchemaFieldForm,
);

enum SchemaOptionTag {
  SchemaField = "_SchemaField",
  NestedSchemaField = "_NestedSchemaField",
  ValuesOf = "_ValuesOf:",
  TableList = "_TableList",
  HtmlEditor = "_HtmlEditor",
}

function isSchemaOptionTag(x: string): x is SchemaOptionTag {
  return (
    x.startsWith(SchemaOptionTag.ValuesOf) ||
    x === SchemaOptionTag.SchemaField ||
    x === SchemaOptionTag.NestedSchemaField ||
    x === SchemaOptionTag.TableList
  );
}

export function useFieldLookup(
  fields: Control<SchemaFieldForm[] | undefined>,
  field: string | undefined | null,
): Control<SchemaFieldForm> {
  return (
    fields.elements?.find((x) => x.fields.field.value === field) ??
    NonExistentField
  );
}

export function useFindScalarField(
  fields: Control<SchemaFieldForm[]>,
  field: string,
): SchemaField | undefined {
  const fc = useFieldLookup(fields, field);
  return fc === NonExistentField ? undefined : fc.value;
}

export function controlDropData(
  parent: ControlForm | undefined,
  dropIndex: number,
  dropSuccess: (drag: DragData, drop: DropData) => void,
): ControlDropData {
  return {
    dropIndex,
    parent,
    success: dropSuccess,
  };
}

interface InternalHooksContext {
  makeOnClick?: (action: ActionControlDefinition) => () => void;
  tableList?: Control<FieldOption[] | undefined>;
  tableFields?: Control<SchemaFieldForm[]>;
}

export function makeEditorFormHooks(
  fields: Control<SchemaFieldForm[]>,
  editHooks: FormEditHooks,
  context?: InternalHooksContext,
): FormEditHooks {
  return {
    ...editHooks,
    useGroupProperties(
      formState: FormEditState,
      definition: GroupedControlsDefinition,
      hooks,
    ): GroupControlProperties {
      const nestedField = useFieldLookup(fields, definition.compoundField);
      if (nestedField !== NonExistentField) {
        hooks = makeEditorFormHooks(
          nestedField.fields.children,
          editHooks,
          context,
        );
      }
      return editHooks.useGroupProperties(formState, definition, hooks);
    },
    useDataProperties: (fs, c, sf) => {
      const control = fs.data.fields[sf.field];
      const visible = useIsControlVisible(c, fs, editHooks.useExpression);
      const fieldList = fields.value;
      const otherField = sf.tags?.find(isSchemaOptionTag);
      const options = otherField
        ? otherFieldOptions(otherField)
        : getOptionsForScalarField(sf);

      const customRender =
        visible && fieldHasTag(sf, "_DefaultValue")
          ? (p: DataRendererProps) => (
              <RenderDefaultValueControls
                editHooks={editHooks}
                fields={fields}
                tableFields={context?.tableFields}
                {...p}
              />
            )
          : undefined;
      return {
        options,
        control,
        defaultValue: sf.defaultValue,
        required: c.required ?? false,
        visible,
        customRender,
        readonly: c.readonly ?? false,
      };

      function otherFieldOptions(ot: SchemaOptionTag) {
        switch (ot) {
          case SchemaOptionTag.SchemaField:
            return fieldList
              .filter((x) => !isCompoundField(x))
              .map(schemaFieldOption);
          case SchemaOptionTag.NestedSchemaField:
            return fieldList.filter(isCompoundField).map(schemaFieldOption);
          case SchemaOptionTag.TableList:
            return context?.tableList?.value ?? [];

          default:
            const otherField = ot.substring(SchemaOptionTag.ValuesOf.length);
            const otherFieldName = fs.data.fields[otherField].value;
            const fieldInSchema =
              fields.current.elements.find(
                (x) => x.fields.field.value === otherFieldName,
              ) ?? NonExistentField;
            const opts =
              fieldInSchema.fields.options.value ??
              fieldInSchema.fields.restrictions.fields?.options.value;
            return opts && opts.length > 0 ? opts : undefined;
        }
      }
    },
    useActionProperties(fs, def): ActionControlProperties {
      const visible = useIsControlVisible(def, fs, editHooks.useExpression);
      return {
        visible,
        onClick: context?.makeOnClick ? context.makeOnClick(def) : () => {},
      };
    },
  };
}

function RenderDefaultValueControls(
  {
    editHooks,
    fields,
    tableFields,
  }: {
    fields: Control<SchemaFieldForm[]>;
    tableFields?: Control<SchemaFieldForm[]>;
    editHooks: FormEditHooks;
  } & DataRendererProps,
  control: Control<any>,
) {
  const currentFields = fields.value;
  const currentValue = control.value;
  useEffect(() => {
    if (currentValue == null) {
      control.setValue(defaultValueForFields(currentFields));
    }
  }, [currentValue == null]);
  const internalFields = fields.value;
  const groupControl: GroupedControlsDefinition = useMemo(
    () => ({
      ...defaultControlDefinitionForm,
      children: internalFields.map(defaultControlForField),
      compoundField: undefined,
      groupOptions: { type: GroupRenderType.Grid, hideTitle: true, columns: 2 },
      type: ControlDefinitionType.Group,
      title: "",
    }),
    [internalFields],
  );
  if (!currentValue) {
    return <></>;
  }
  return renderControl(
    groupControl,
    { fields: currentFields, data: control },
    makeEditorFormHooks(
      tableFields ?? newControl<SchemaFieldForm[]>([]),
      editHooks,
    ),
    "nested",
  );
}

function schemaFieldOption(c: SchemaFieldForm): FieldOption {
  return { name: c.field, value: c.field };
}

export function defaultControlForField(
  sf: SchemaField,
): DataControlDefinition | GroupedControlsDefinition {
  if (isCompoundField(sf)) {
    return {
      ...defaultControlDefinitionForm,
      type: ControlDefinitionType.Group,
      title: sf.displayName,
      compoundField: sf.field,
      groupOptions: {
        type: GroupRenderType.Grid,
        hideTitle: false,
        columns: 3,
      } as GridRenderer,
      children: sf.children.map(defaultControlForField),
    };
  } else if (isScalarField(sf)) {
    const htmlEditor = sf.tags?.includes(SchemaOptionTag.HtmlEditor);
    return {
      ...defaultControlDefinitionForm,
      type: ControlDefinitionType.Data,
      title: sf.displayName,
      field: sf.field,
      required: sf.required,
      renderOptions: {
        type: htmlEditor ? DataRenderType.HtmlEditor : DataRenderType.Standard,
      },
    };
  }
  throw "Unknown schema field";
}
