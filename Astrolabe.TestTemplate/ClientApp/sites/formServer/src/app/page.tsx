"use client";

import BasicFormEditor from "@astroapps/schemas-editor/BasicFormEditor";
import { useControl } from "@react-typed-forms/core";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import {
  addMissingControls,
  buildSchema,
  compoundField,
  createDefaultRenderers,
  createFormRenderer,
  dataControl,
  defaultTailwindTheme,
  DynamicPropertyType,
  dynamicVisibility,
  jsonataExpr,
  stringField,
  stringOptionsField,
  textDisplayControl,
  withScalarOptions,
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
  greetings: {
    pls: { cool: MyEnum }[];
  };
}

const myEnumField = stringOptionsField(
  "Greeting",
  { name: "HAI", value: MyEnum.Hai },
  { name: "Hello", value: MyEnum.Hello },
);

const fields = buildSchema<OurData>({
  greeting: myEnumField,
  greetings: compoundField(
    "Greetings",
    buildSchema<{ pls: { cool: MyEnum }[] }>({
      pls: compoundField(
        "PLS",
        buildSchema<{ cool: MyEnum }>({ cool: myEnumField }),
        { collection: true },
      ),
    }),
    {},
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
    <BasicFormEditor<string>
      formRenderer={StdFormRenderer}
      editorRenderer={StdFormRenderer}
      loadForm={async (c) => {
        const controls = addMissingControls(fields, []);
        controls[1].children![0].children!.push(
          textDisplayControl("", {
            dynamic: [
              {
                type: DynamicPropertyType.Display,
                expr: jsonataExpr("cool"),
              },
            ],
          }),
        );
        return {
          fields,
          controls,
        };
      }}
      selectedForm={selectedForm}
      formTypes={[["MyForm", "MyForm"]]}
      saveForm={async (controls) => {}}
      controlDefinitionSchema={CustomControlSchema}
    />
  );
}
