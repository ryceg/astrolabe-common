import { Control, newControl } from "@react-typed-forms/core";
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
  const RenderEditor = useControlRenderer(
    editorControls,
    editorFields,
    renderer,
  );
  return <RenderEditor control={control} />;
  // const editorHooks = makeEditorFormHooks(fieldList, defaultFormEditHooks);
  // return renderControl(editorControls, control, {
  //   fields: editorFields,
  //   hooks: editorHooks,
  //   renderer,
  // });
}
