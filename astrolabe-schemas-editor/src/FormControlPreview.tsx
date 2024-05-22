import {
  Control,
  unsafeRestoreControl,
  useComputed,
  useControl,
} from "@react-typed-forms/core";
import React, {
  createContext,
  HTMLAttributes,
  ReactNode,
  useContext,
} from "react";
import { ControlDefinitionForm, SchemaFieldForm } from "./schemaSchemas";
import { useDroppable } from "@dnd-kit/core";
import { motion } from "framer-motion";
import {
  ControlDataContext,
  ControlDefinition,
  ControlDefinitionType,
  defaultDataProps,
  defaultSchemaInterface,
  defaultValueForField,
  DynamicPropertyType,
  elementValueForField,
  findChildDefinition,
  FormRenderer,
  getControlData,
  getDisplayOnlyOptions,
  isDataControlDefinition,
  isGroupControlsDefinition,
  lookupSchemaField,
  makeHook,
  renderControlLayout,
  SchemaField,
  SchemaInterface,
} from "@react-typed-forms/schemas";
import { useScrollIntoView } from "./useScrollIntoView";
import {
  ControlDragState,
  controlDropData,
  ControlForm,
  DragData,
  DropData,
} from "./util";

export interface FormControlPreviewProps {
  definition: ControlDefinition;
  parent?: ControlDefinition;
  dropIndex: number;
  noDrop?: boolean;
  fields: SchemaField[];
  elementIndex?: number;
  schemaInterface?: SchemaInterface;
}

export interface FormControlPreviewContext {
  selected: Control<Control<any> | undefined>;
  treeDrag: Control<ControlDragState | undefined>;
  dropSuccess: (drag: DragData, drop: DropData) => void;
  readonly?: boolean;
  VisibilityIcon: ReactNode;
  renderer: FormRenderer;
}

export interface FormControlPreviewDataProps extends FormControlPreviewProps {
  isSelected: boolean;
  isOver: boolean;
}

const defaultLayoutChange = "position";

const PreviewContext = createContext<FormControlPreviewContext | undefined>(
  undefined,
);
export const PreviewContextProvider = PreviewContext.Provider;

function usePreviewContext() {
  const pc = useContext(PreviewContext);
  if (!pc) throw "Must supply a PreviewContextProvider";
  return pc;
}

export function FormControlPreview(props: FormControlPreviewProps) {
  const { definition, parent, elementIndex, fields, dropIndex, noDrop } = props;
  const { selected, dropSuccess, renderer } = usePreviewContext();
  const item = unsafeRestoreControl(definition) as
    | Control<ControlDefinitionForm>
    | undefined;
  const isSelected = selected.value === item;
  const scrollRef = useScrollIntoView(isSelected);
  const { setNodeRef, isOver } = useDroppable({
    id: item?.uniqueId ?? 0,
    disabled: Boolean(noDrop),
    data: controlDropData(
      parent ? unsafeRestoreControl(parent)?.as() : undefined,
      dropIndex,
      dropSuccess,
    ),
  });
  const schemaField = lookupSchemaField(definition, fields);
  const groupControl = useControl({});
  const dataContext: ControlDataContext = {
    data: groupControl,
    path: [],
    fields,
    schemaInterface: props.schemaInterface ?? defaultSchemaInterface,
  };
  const [, , childContext] = getControlData(
    schemaField,
    dataContext,
    elementIndex,
  );
  const displayOptions = getDisplayOnlyOptions(definition);
  const childControl = useComputed(() =>
    displayOptions
      ? displayOptions.sampleText ?? "Sample Data"
      : schemaField &&
        (elementIndex == null
          ? defaultValueForField(
              schemaField,
              schemaField.collection ||
                (isDataControlDefinition(definition) && definition.required),
            )
          : elementValueForField(schemaField)),
  );
  const adornments =
    definition.adornments?.map((x) =>
      renderer.renderAdornment({ adornment: x }),
    ) ?? [];

  const layout = renderControlLayout({
    definition,
    renderer,
    parentContext: dataContext,
    elementIndex,
    renderChild: (k, def, c) => {
      return (
        <FormControlPreview
          key={k}
          definition={def}
          parent={definition}
          dropIndex={0}
          elementIndex={c?.elementIndex}
          fields={c?.dataContext?.fields ?? childContext.fields}
        />
      );
    },
    createDataProps: defaultDataProps,
    formOptions: {},
    dataContext: childContext,
    control: childControl,
    field: schemaField,
    useChildVisibility: () => makeHook(() => useControl(true), undefined),
  });
  const mouseCapture: Pick<
    HTMLAttributes<HTMLDivElement>,
    "onClick" | "onClickCapture" | "onMouseDownCapture"
  > = isGroupControlsDefinition(definition) ||
  (isDataControlDefinition(definition) &&
    (definition.children?.length ?? 0) > 0)
    ? { onClick: (e) => (selected.value = item) }
    : {
        onClickCapture: (e) => {
          e.preventDefault();
          e.stopPropagation();
          selected.value = item;
        },
        onMouseDownCapture: (e) => {
          e.stopPropagation();
          e.preventDefault();
        },
      };
  const {
    style,
    children: child,
    className,
  } = renderer.renderLayout({
    ...layout,
    adornments,
    className: definition.layoutClass,
  });
  return (
    <motion.div
      layout={defaultLayoutChange}
      layoutId={item?.uniqueId.toString()}
      style={{
        ...style,
        backgroundColor: isSelected ? "rgba(25, 118, 210, 0.08)" : undefined,
        position: "relative",
      }}
      {...mouseCapture}
      className={className!}
      ref={(e) => {
        scrollRef.current = e;
        setNodeRef(e);
      }}
    >
      <EditorDetails
        control={definition}
        arrayElement={elementIndex != null}
        schemaVisibility={!!schemaField?.onlyForTypes?.length}
      />

      {child}
    </motion.div>
  );
}
function EditorDetails({
  control,
  schemaVisibility,
  arrayElement,
}: {
  control: ControlDefinition;
  arrayElement: boolean;
  schemaVisibility?: boolean;
}) {
  const { VisibilityIcon } = usePreviewContext();
  const { dynamic } = control;
  const hasVisibilityScripting = dynamic?.some(
    (x) => x.type === DynamicPropertyType.Visible,
  );

  const fieldName = !arrayElement
    ? isDataControlDefinition(control)
      ? control.field
      : isGroupControlsDefinition(control)
      ? control.compoundField
      : null
    : null;

  if (!fieldName && !(hasVisibilityScripting || schemaVisibility)) return <></>;
  return (
    <div
      style={{
        backgroundColor: "white",
        fontSize: "12px",
        position: "absolute",
        top: 0,
        right: 0,
        padding: 2,
        border: "solid 1px black",
      }}
    >
      {fieldName}
      {(hasVisibilityScripting || schemaVisibility) && (
        <span style={{ paddingLeft: 4 }}>{VisibilityIcon}</span>
      )}
    </div>
  );
}
