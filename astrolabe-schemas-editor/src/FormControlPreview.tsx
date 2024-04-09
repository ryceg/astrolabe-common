import { Control, newControl, useControl } from "@react-typed-forms/core";
import React, {
  createContext,
  HTMLAttributes,
  ReactNode,
  useContext,
} from "react";
import { SchemaFieldForm } from "./schemaSchemas";
import { useDroppable } from "@dnd-kit/core";
import { motion } from "framer-motion";
import {
  ControlDefinitionType,
  defaultDataProps,
  defaultValueForField,
  DynamicPropertyType,
  FormRenderer,
  getControlData,
  getDisplayOnlyOptions,
  isGroupControlsDefinition,
  lookupSchemaField,
  renderControlLayout,
} from "@react-typed-forms/schemas";
import { useScrollIntoView } from "./useScrollIntoView";
import {
  ControlDragState,
  controlDropData,
  ControlForm,
  DragData,
  DropData,
} from ".";

export interface FormControlPreviewProps {
  item: ControlForm;
  parent?: ControlForm;
  dropIndex: number;
  noDrop?: boolean;
  fields: Control<SchemaFieldForm[]>;
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
  const { item, parent, dropIndex, noDrop } = props;
  const { selected, dropSuccess, renderer } = usePreviewContext();
  const fields = trackedValue(props.fields);
  const definition = trackedValue(item);
  const isSelected = selected.value === item;
  const scrollRef = useScrollIntoView(isSelected);
  const { setNodeRef, isOver } = useDroppable({
    id: item.uniqueId,
    disabled: Boolean(noDrop),
    data: controlDropData(parent, dropIndex, dropSuccess),
  });
  const children = definition.children ?? [];
  const schemaField = lookupSchemaField(definition, fields);
  const groupControl = newControl({});
  const groupContext = {
    groupControl,
    fields,
  };
  const [, childContext] = getControlData(schemaField, groupContext);
  const displayOptions = getDisplayOnlyOptions(definition);
  const childControl = newControl(
    displayOptions
      ? displayOptions.sampleText ?? "Sample Data"
      : schemaField &&
          defaultValueForField(
            schemaField,
            schemaField.collection || definition.required,
          ),
  );
  const adornments =
    definition.adornments?.map((x) =>
      renderer.renderAdornment({ adornment: x }),
    ) ?? [];
  const displayControl = useControl(undefined);

  const layout = renderControlLayout({
    definition,
    renderer,
    childCount: children.length,
    renderChild: (k, i, c) => (
      <FormControlPreview
        key={k}
        item={unsafeRestoreControl(children[i])}
        parent={item}
        dropIndex={0}
        fields={unsafeRestoreControl(childContext.fields).as()}
      />
    ),
    createDataProps: defaultDataProps,
    formOptions: {},
    groupContext,
    control: childControl,
    schemaField,
    displayControl,
  });
  const mouseCapture: Pick<
    HTMLAttributes<HTMLDivElement>,
    "onClick" | "onClickCapture" | "onMouseDownCapture"
  > = isGroupControlsDefinition(definition)
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
      layoutId={item.uniqueId.toString()}
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
        control={item}
        schemaVisibility={!!schemaField?.onlyForTypes?.length}
      />

      {child}
    </motion.div>
  );
}
function EditorDetails({
  control,
  schemaVisibility,
}: {
  control: ControlForm;
  schemaVisibility?: boolean;
}) {
  const { VisibilityIcon } = usePreviewContext();
  const {
    type: { value: type },
    field,
    compoundField,
    dynamic,
  } = control.fields;
  const hasVisibilityScripting = dynamic.value?.some(
    (x) => x.type === DynamicPropertyType.Visible,
  );

  const fieldName =
    type === ControlDefinitionType.Data
      ? field.value
      : type === ControlDefinitionType.Group
        ? compoundField.value
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

const restoreControlSymbol = Symbol("restoreControl");
export function trackedValue<A>(c: Control<A>): A {
  const cv: any = c.current.value;
  if (cv == null) return cv;
  if (typeof cv !== "object") return c.value;
  const arr = Array.isArray(cv);
  return new Proxy(cv, {
    get(target: object, p: string | symbol, receiver: any): any {
      if (p === restoreControlSymbol) return c;
      if (arr) {
        if (p === "length" || p === "toJSON") return Reflect.get(cv, p);
        const nc = (c.elements as any)[p];
        if (typeof nc === "function") return nc;
        return trackedValue(nc);
      }
      return trackedValue((c.fields as any)[p]);
    },
  }) as A;
}

export function unsafeRestoreControl<A>(v: A): Control<A> {
  return (v as any)[restoreControlSymbol];
}
