import { Control, newControl, useComputed } from "@react-typed-forms/core";
import { ControlDefinitionForm, SchemaFieldForm } from "./schemaSchemas";
import { ReactElement } from "react";
import {
  FormRenderer,
  GroupedControlsDefinition,
  SchemaField,
  useControlRenderer,
} from "@react-typed-forms/schemas";
import {
  findAllParentsInControls,
  findSchemaFieldListForParents,
  useEditorDataHook,
} from "./index";

export function FormControlEditor({
  control,
  renderer,
  editorFields,
  fields,
  editorControls,
  rootControls,
}: {
  control: Control<ControlDefinitionForm>;
  editorControls: GroupedControlsDefinition;
  editorFields: SchemaField[];
  fields: Control<SchemaFieldForm[]>;
  renderer: FormRenderer;
  rootControls: Control<ControlDefinitionForm[]>;
}): ReactElement {
  const fieldList = useComputed(() => {
    const parentFields = findAllParentsInControls(control, rootControls);
    return (
      findSchemaFieldListForParents(fields, parentFields) ??
      newControl<SchemaFieldForm[]>([])
    );
  }).value;
  const useDataHook = useEditorDataHook(fieldList.value);
  const RenderEditor = useControlRenderer(
    editorControls,
    editorFields,
    renderer,
    {
      useDataHook,
    },
  );
  return <RenderEditor control={control} />;
}
