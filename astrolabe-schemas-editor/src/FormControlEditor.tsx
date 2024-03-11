import { Control, newControl } from "@react-typed-forms/core";
import { ControlDefinitionForm, SchemaFieldForm } from "./schemaSchemas";
import { ReactElement } from "react";
import {
  defaultFormEditHooks,
  FormRenderer,
  GroupedControlsDefinition,
  renderControl,
  SchemaField,
} from "@react-typed-forms/schemas";
import {
  findAllParentsInControls,
  findSchemaFieldListForParents,
  makeEditorFormHooks,
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
  const fieldList =
    findSchemaFieldListForParents(
      fields,
      findAllParentsInControls(control, rootControls),
    ) ?? newControl<SchemaFieldForm[]>([]);
  const editorHooks = makeEditorFormHooks(fieldList, defaultFormEditHooks);
  return renderControl(editorControls, control, {
    fields: editorFields,
    hooks: editorHooks,
    renderer,
  });
}
