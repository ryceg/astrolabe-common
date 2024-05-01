"use client";

import BasicFormEditor from "@astroapps/schemas-editor/BasicFormEditor";
import { useControl } from "@react-typed-forms/core";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import {
  buildSchema,
  createDefaultRenderers,
  createFormRenderer,
  defaultTailwindTheme,
  stringField,
  stringOptionsField,
} from "@react-typed-forms/schemas";
import { addCustomRenderOptions } from "@astroapps/schemas-editor";
import { ControlDefinitionSchema } from "@astroapps/schemas-editor/schemaSchemas";
import { useQueryControl } from "@astroapps/client/hooks/useQueryControl";
import {
  convertStringParam,
  useSyncParam,
} from "@astroapps/client/hooks/queryParamSync";

const CustomControlSchema = addCustomRenderOptions(ControlDefinitionSchema, [
  { name: "Fixed array", value: "FixedArray", fields: [] },
]);

enum MyEnum {
  Hai = "Hai",
  Hello = "Hello",
}

interface OurData {
  greeting: MyEnum;
}

const fields = buildSchema<OurData>({
  greeting: stringOptionsField(
    "Greeting",
    { name: "HAI", value: MyEnum.Hai },
    { name: "Hello", value: MyEnum.Hello },
  ),
});

const StdFormRenderer = createFormRenderer(
  [],
  createDefaultRenderers(defaultTailwindTheme),
);

export default function Editor() {
  const qc = useQueryControl();
  const selectedForm = useControl("");
  useSyncParam(
    qc,
    selectedForm,
    "form",
    convertStringParam(
      (x) => x,
      (x) => x,
      "",
    ),
  );
  return (
    <DndProvider backend={HTML5Backend}>
      <BasicFormEditor<string>
        formRenderer={StdFormRenderer}
        editorRenderer={StdFormRenderer}
        loadForm={async (c) => ({ fields, controls: [] })}
        selectedForm={selectedForm}
        formTypes={[["MyForm", "MyForm"]]}
        saveForm={async (controls) => {}}
        controlDefinitionSchema={CustomControlSchema}
      />
    </DndProvider>
  );
}
