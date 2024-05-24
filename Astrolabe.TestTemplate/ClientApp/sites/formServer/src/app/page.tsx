"use client";

import {
  applyEditorExtensions,
  BasicFormEditor,
} from "@astroapps/schemas-editor";
import { useControl } from "@react-typed-forms/core";
import {
  addMissingControls,
  buildSchema,
  createDefaultRenderers,
  createDisplayRenderer,
  createFormRenderer,
  createRadioRenderer,
  defaultTailwindTheme,
  FieldOption,
  intField,
} from "@react-typed-forms/schemas";
import { useQueryControl } from "@astroapps/client/hooks/useQueryControl";
import {
  convertStringParam,
  useSyncParam,
} from "@astroapps/client/hooks/queryParamSync";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";

const CustomControlSchema = applyEditorExtensions({});

enum TestOption {
  Yes,
  No,
  Maybe,
}

interface OurData {
  options: TestOption[];
  single: TestOption;
}

const options: FieldOption[] = [
  { name: "Yes", value: TestOption.Yes },
  { name: "No", value: TestOption.No },
  { name: "Maybe", value: TestOption.Maybe },
];

const fields = buildSchema<OurData>({
  options: intField("Multi", { collection: true, options }),
  single: intField("Single", { options }),
});

const customDisplay = createDisplayRenderer(
  (p) => <div>PATH: {p.dataContext.path.join(",")}</div>,
  { renderType: "Custom" },
);

const StdFormRenderer = createFormRenderer(
  [customDisplay],
  createDefaultRenderers({
    ...defaultTailwindTheme,
  }),
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
        loadForm={async (c) => {
          const controls = addMissingControls(fields, []);
          // const dcd = controls[0] as DataControlDefinition;
          // dcd.renderOptions = {
          //   type: "Group",
          //   groupOptions: { type: "Standard" },
          // } as DataGroupRenderOptions;
          return {
            fields,
            controls,
          };
        }}
        selectedForm={selectedForm}
        formTypes={[["MyForm", "MyForm"]]}
        saveForm={async (controls) => {}}
        controlDefinitionSchemaMap={CustomControlSchema}
      />
    </DndProvider>
  );
}
