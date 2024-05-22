"use client";

import {
  applyEditorExtensions,
  BasicFormEditor,
} from "@astroapps/schemas-editor";
import {
  RenderArrayElements,
  useControl,
  useTrackedComponent,
} from "@react-typed-forms/core";
import {
  addMissingControls,
  buildSchema,
  compoundField,
  createDefaultRenderers,
  createDisplayRenderer,
  createFormRenderer,
  createGroupRenderer,
  DataControlDefinition,
  DataGroupRenderOptions,
  defaultTailwindTheme,
  GroupRendererProps,
  stringField,
  useDynamicHooks,
} from "@react-typed-forms/schemas";
import { useQueryControl } from "@astroapps/client/hooks/useQueryControl";
import {
  convertStringParam,
  useSyncParam,
} from "@astroapps/client/hooks/queryParamSync";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";

const CustomControlSchema = applyEditorExtensions({
  GroupRenderOptions: {
    name: "Test",
    value: "Test",
    fields: [],
  },
  DisplayData: {
    name: "Custom display",
    value: "Custom",
    fields: [],
  },
});

interface GroupData {
  show: string;
  field1: string;
  field2: string;
}

interface OurData {
  group: GroupData[];
}

const fields = buildSchema<OurData>({
  group: compoundField(
    "HELP",
    buildSchema<GroupData>({
      show: stringField("Type", { isTypeField: true }),
      field1: stringField("Field 1", { onlyForTypes: ["one"] }),
      field2: stringField("Field 2", { onlyForTypes: ["one", "two"] }),
    }),
    { collection: true },
  ),
});

const customDisplay = createDisplayRenderer(
  (p) => <div>PATH: {p.dataContext.path.join(",")}</div>,
  { renderType: "Custom" },
);

// const testRender = createGroupRenderer((p, r) => <TestChildVis {...p} />, {
//   renderType: "Test",
// });

// function TestChildVis(p: GroupRendererProps) {
//   const visHooks = useDynamicHooks(
//     Object.fromEntries(
//       p.childDefinitions.map((x, i) => [i, p.useChildVisibility(i)]),
//     ),
//   );
//   const Render = useTrackedComponent(() => {
//     const visses = Object.entries(visHooks(p.dataContext)).map((x) => x[1]);
//     return (
//       <div>
//         <RenderArrayElements array={visses}>
//           {(c, i) => (
//             <>
//               {c.uniqueId}
//               {c.value ? "Visible" : "Hidden"}
//               {p.renderChild(i, i)}
//             </>
//           )}
//         </RenderArrayElements>
//       </div>
//     );
//   }, [visHooks]);
//   return <Render />;
// }

const StdFormRenderer = createFormRenderer(
  [customDisplay],
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
