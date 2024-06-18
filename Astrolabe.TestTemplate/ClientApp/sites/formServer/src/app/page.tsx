"use client";

import {
  applyEditorExtensions,
  BasicFormEditor,
  ControlDefinitionSchema,
} from "@astroapps/schemas-editor";
import { newControl, useControl } from "@react-typed-forms/core";
import {
  buildSchema,
  compoundField,
  createDefaultRenderers,
  createDisplayRenderer,
  createFormRenderer,
  defaultSchemaInterface,
  defaultTailwindTheme,
  FieldType,
  intField,
  makeScalarField,
  visitControlData,
} from "@react-typed-forms/schemas";
import { useQueryControl } from "@astroapps/client/hooks/useQueryControl";
import {
  convertStringParam,
  useSyncParam,
} from "@astroapps/client/hooks/queryParamSync";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";
import { Client } from "../client";
import controlsJson from "../ControlDefinition.json";
import { createDatePickerRenderer } from "@astroapps/schemas-datepicker";

const CustomControlSchema = applyEditorExtensions({});

const customDisplay = createDisplayRenderer(
  (p) => <div>PATH: {p.dataContext.path.join(",")}</div>,
  { renderType: "Custom" },
);

const StdFormRenderer = createFormRenderer(
  [customDisplay, createDatePickerRenderer()],
  createDefaultRenderers({
    ...defaultTailwindTheme,
  }),
);

interface TestSchema {
  things: {
    sub: {
      thingId: string;
      other: number;
    };
  };
}

const TestSchema = buildSchema<TestSchema>({
  things: compoundField(
    "Things",

    buildSchema<TestSchema["things"]>({
      sub: compoundField(
        "Sub",
        buildSchema<TestSchema["things"]["sub"]>({
          thingId: makeScalarField({
            type: FieldType.String,
            options: [
              { name: "One", value: "one" },
              { name: "Two", value: "two" },
            ],
          }),
          other: intField("Test drop down"),
        }),
      ),
    }),
  ),
});

export default function Editor() {
  const qc = useQueryControl();
  const selectedForm = useControl("Test");
  useSyncParam(
    qc,
    selectedForm,
    "form",
    convertStringParam(
      (x) => x,
      (x) => x,
      "Test",
    ),
  );
  return (
    <DndProvider backend={HTML5Backend}>
      <BasicFormEditor<string>
        formRenderer={StdFormRenderer}
        editorRenderer={StdFormRenderer}
        loadForm={async (c) => {
          return c === "EditorControls"
            ? {
                fields: ControlDefinitionSchema,
                controls: controlsJson,
              }
            : { fields: TestSchema, controls: [] };
        }}
        selectedForm={selectedForm}
        formTypes={[
          ["EditorControls", "EditorControls"],
          ["Test", "Test"],
        ]}
        saveForm={async (controls) => {
          if (selectedForm.value === "EditorControls") {
            await new Client().controlDefinition(controls);
          } else {
            visitControlData(
              controls[0],
              {
                data: newControl({
                  things: { sub: { thingId: "", other: 1 } },
                } satisfies TestSchema),
                fields: TestSchema,
                schemaInterface: defaultSchemaInterface,
                path: [],
              },
              (d, f, c) => console.log(d.field, c.value),
            );
          }
        }}
        previewOptions={{
          actionOnClick: (aid, data) => () => console.log("Clicked", aid, data),
        }}
        controlDefinitionSchemaMap={CustomControlSchema}
        editorControls={controlsJson}
      />
    </DndProvider>
  );
}
