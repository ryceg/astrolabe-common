import {
  ControlTree,
  removeNodeFromParent,
} from "@astroapps/ui-tree/ControlTree";
import {
  FormControlPreview,
  PreviewContextProvider,
} from "./FormControlPreview";
import {
  addElement,
  Control,
  Fselect,
  groupedChanges,
  RenderArrayElements,
  RenderControl,
  RenderElements,
  RenderOptional,
  trackedValue,
  useComputed,
  useControl,
  useControlEffect,
} from "@react-typed-forms/core";
import { FormControlEditor } from "./FormControlEditor";
import {
  ControlDefinitionForm,
  ControlDefinitionSchema,
  defaultControlDefinitionForm,
  SchemaFieldForm,
  toControlDefinitionForm,
  toSchemaFieldForm,
} from "./schemaSchemas";
import {
  addMissingControls,
  cleanDataForSchema,
  ControlDefinition,
  ControlDefinitionType,
  ControlRenderOptions,
  FormRenderer,
  getAllReferencedClasses,
  groupedControl,
  GroupedControlsDefinition,
  GroupRenderType,
  SchemaField,
  useControlRenderer,
} from "@react-typed-forms/schemas";
import {
  isControlDefinitionNode,
  makeControlTree,
  SchemaFieldsProvider,
} from "./controlTree";
import { ControlTreeNode, useTreeStateControl } from "@astroapps/ui-tree";
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { ReactElement, useMemo } from "react";
import { controlIsCompoundField, controlIsGroupControl } from "./";
import {
  createTailwindcss,
  TailwindConfig,
} from "@mhsdesign/jit-browser-tailwindcss";

interface PreviewData {
  showing: boolean;
  key: number;
  data: any;
  fields: SchemaField[];
  controls: ControlDefinition[];
}

export interface BasicFormEditorProps<A extends string> {
  formRenderer: FormRenderer;
  editorRenderer: FormRenderer;
  loadForm: (
    formType: A,
  ) => Promise<{ controls: ControlDefinition[]; fields: SchemaField[] }>;
  selectedForm: Control<A>;
  formTypes: [A, string][];
  saveForm: (controls: ControlDefinition[]) => Promise<any>;
  validation?: (
    data: Control<any>,
    controls: ControlDefinition[],
  ) => Promise<any>;
  controlDefinitionSchema?: SchemaField[];
  editorControls?: ControlDefinition[];
  previewOptions?: ControlRenderOptions;
  tailwindConfig?: TailwindConfig;
}

export default function BasicFormEditor<A extends string>({
  formRenderer,
  selectedForm,
  loadForm,
  editorRenderer,
  formTypes,
  validation,
  saveForm,
  controlDefinitionSchema = ControlDefinitionSchema,
  editorControls,
  previewOptions,
  tailwindConfig,
}: BasicFormEditorProps<A>): ReactElement {
  const controls = useControl<ControlDefinitionForm[]>([], {
    elems: makeControlTree(treeActions),
  });
  const fields = useControl<SchemaFieldForm[]>([]);
  const treeDrag = useControl();
  const treeState = useTreeStateControl();
  const selected = treeState.fields.selected;
  const previewData = useControl<PreviewData>({
    showing: false,
    key: 0,
    data: {},
    controls: [],
    fields: [],
  });
  const controlGroup: GroupedControlsDefinition = useMemo(() => {
    return {
      children: addMissingControls(
        controlDefinitionSchema,
        editorControls ?? [],
      ),
      type: ControlDefinitionType.Group,
      groupOptions: { type: GroupRenderType.Standard },
    };
  }, [editorControls]);

  useControlEffect(
    () => selectedForm.value,
    (ft) => {
      doLoadForm(ft);
    },
    true,
  );

  const genStyles = useMemo(
    () =>
      typeof window === "undefined"
        ? { generateStylesFromContent: async () => "" }
        : createTailwindcss({
            tailwindConfig: tailwindConfig ?? {
              corePlugins: { preflight: false },
            },
          }),
    [tailwindConfig],
  );

  const allClasses = useComputed(() => {
    const cv = trackedValue(controls);
    return cv.flatMap(getAllReferencedClasses).join(" ");
  });
  const styles = useControl("");

  useControlEffect(
    () => allClasses.value,
    (cv) => runTailwind(cv),
    true,
  );

  async function runTailwind(classes: string) {
    {
      const html = `<div class="${classes}"></div>`;

      styles.value = await genStyles.generateStylesFromContent(
        `@tailwind utilities;`,
        [html],
      );
    }
  }

  function button(onClick: () => void, action: string) {
    return formRenderer.renderAction({
      onClick,
      actionText: action,
      actionId: action,
    });
  }

  async function doLoadForm(dt: A) {
    const res = await loadForm(dt);
    groupedChanges(() => {
      controls.setInitialValue(res.controls.map(toControlDefinitionForm));
      fields.setInitialValue(res.fields.map(toSchemaFieldForm));
    });
  }

  async function doSave() {
    saveForm(
      controls.value.map((c) => cleanDataForSchema(c, ControlDefinitionSchema)),
    );
  }

  const previewMode = previewData.fields.showing.value;
  return (
    <PreviewContextProvider
      value={{
        selected,
        treeDrag,
        VisibilityIcon: <i className="fa fa-eye" />,
        dropSuccess: () => {},
        renderer: formRenderer,
      }}
    >
      <SchemaFieldsProvider value={fields}>
        <PanelGroup direction="horizontal">
          <Panel>
            <RenderControl render={() => <style>{styles.value}</style>} />
            <div className="overflow-auto w-full h-full p-8">
              {previewMode ? (
                <FormPreview
                  previewData={previewData}
                  formRenderer={formRenderer}
                  validation={validation}
                  previewOptions={previewOptions}
                />
              ) : (
                <RenderElements
                  control={controls}
                  children={(c, i) => (
                    <FormControlPreview
                      item={c}
                      fields={fields}
                      dropIndex={i}
                    />
                  )}
                />
              )}
            </div>
          </Panel>
          <PanelResizeHandle className="w-2 bg-surface-200" />
          <Panel maxSize={33}>
            <PanelGroup direction="vertical">
              <Panel>
                <div className="p-4 overflow-auto w-full h-full">
                  <div className="my-2 flex gap-2">
                    <Fselect control={selectedForm}>
                      <RenderArrayElements
                        array={formTypes}
                        children={(x) => <option value={x[0]}>{x[1]}</option>}
                      />
                    </Fselect>
                    {button(doSave, "Save " + selectedForm.value)}
                    {button(
                      togglePreviewMode,
                      previewMode ? "Edit Mode" : "Editable Preview",
                    )}
                    {button(addMissing, "Add missing controls")}
                  </div>
                  <ControlTree
                    treeState={treeState}
                    controls={controls}
                    indicator={false}
                    canDropAtRoot={() => true}
                  />
                  {button(
                    () =>
                      addElement(controls, {
                        ...defaultControlDefinitionForm,
                        type: ControlDefinitionType.Group,
                      }),
                    "Add Page",
                  )}
                </div>
              </Panel>
              <PanelResizeHandle className="h-2 bg-surface-200" />
              <Panel>
                <div className="p-4 overflow-auto w-full h-full">
                  <RenderOptional control={selected}>
                    {(c) => (
                      <FormControlEditor
                        key={c.value.uniqueId}
                        control={c.value}
                        fields={fields}
                        renderer={editorRenderer}
                        editorFields={controlDefinitionSchema}
                        rootControls={controls}
                        editorControls={controlGroup}
                      />
                    )}
                  </RenderOptional>
                </div>
              </Panel>
            </PanelGroup>
          </Panel>
        </PanelGroup>
      </SchemaFieldsProvider>
    </PreviewContextProvider>
  );

  function addMissing() {
    controls.value = addMissingControls(fields.value, controls.value).map(
      toControlDefinitionForm,
    );
  }

  function togglePreviewMode() {
    if (previewMode) previewData.fields.showing.value = false;
    else
      previewData.setValue((v) => ({
        ...v,
        showing: true,
        key: v.key + 1,
        controls: controls.value,
        fields: fields.value,
      }));
  }

  function treeActions(
    node: ControlTreeNode,
    schema: Control<SchemaFieldForm>,
  ) {
    const c = node.control;
    return (
      <>
        {isControlDefinitionNode(c) &&
          (controlIsGroupControl(c) || controlIsCompoundField(schema)) && (
            <i
              className="fa fa-plus"
              onClick={(e) => {
                e.stopPropagation();
                selected.value = addElement(c.fields.children, {
                  ...defaultControlDefinitionForm,
                  title: "New",
                });
              }}
            />
          )}
        <i
          className="fa fa-remove"
          onClick={(e) => {
            e.stopPropagation();
            removeNodeFromParent(node, selected);
          }}
        />
      </>
    );
  }
}

function FormPreview({
  previewData,
  formRenderer,
  validation,
  previewOptions,
}: {
  previewData: Control<PreviewData>;
  formRenderer: FormRenderer;
  validation?: (data: any, controls: ControlDefinition[]) => Promise<any>;
  previewOptions?: ControlRenderOptions;
}) {
  const data = previewData.fields.data;
  const { controls, fields } = previewData.value;
  const formControl: GroupedControlsDefinition = useMemo(
    () => groupedControl(controls),
    [controls],
  );
  const RenderPreview = useControlRenderer(
    formControl,
    fields,
    formRenderer,
    previewOptions,
  );
  useControlEffect(
    () => data.value,
    (v) => console.log(v),
  );
  return (
    <>
      <div className="my-2">
        {formRenderer.renderAction({
          onClick: runValidation,
          actionId: "validate",
          actionText: "Run Validation",
        })}
      </div>
      <RenderPreview control={data} />
    </>
  );

  async function runValidation() {
    data.touched = true;
    await validation?.(data, controls);
  }
}
