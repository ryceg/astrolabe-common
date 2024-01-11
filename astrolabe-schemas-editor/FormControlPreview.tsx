import { Control, newControl } from "@react-typed-forms/core";
import React, { createContext, ReactNode, useContext, useMemo } from "react";
import { isNullOrEmpty } from "@astrolabe/client/util/arrays";
import { SchemaFieldForm } from "./schemaSchemas";
import { useDroppable } from "@dnd-kit/core";
import { LayoutGroup, motion } from "framer-motion";
import update from "immutability-helper";
import {
  ActionControlDefinition,
  AlwaysVisible,
  ArrayRendererProps,
  ControlDefinitionType,
  createAction,
  DataControlDefinition,
  DisplayControlDefinition,
  DynamicPropertyType,
  FormEditHooks,
  FormRenderer,
  getDefaultScalarControlProperties,
  GroupedControlsDefinition,
  SchemaField,
} from "@react-typed-forms/schemas";
import { useScrollIntoView } from "./useScrollIntoView";
import {
  ControlDragState,
  controlDropData,
  ControlForm,
  DragData,
  DropData,
  useFieldLookup,
  useFindScalarField,
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
  hooks: FormEditHooks;
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
  const { selected, dropSuccess } = usePreviewContext();
  const type = item.fields.type.value;
  const isSelected = selected.value === item;
  const scrollRef = useScrollIntoView(isSelected);
  const { setNodeRef, isOver } = useDroppable({
    id: item.uniqueId,
    disabled: Boolean(noDrop),
    data: controlDropData(parent, dropIndex, dropSuccess),
  });
  return (
    <div
      ref={(e) => {
        scrollRef.current = e;
        setNodeRef(e);
      }}
    >
      {contents()}
    </div>
  );

  function contents() {
    const allProps = { ...props, isSelected, isOver };
    switch (type) {
      case ControlDefinitionType.Data:
        return <DataControlPreview {...allProps} />;
      case ControlDefinitionType.Display:
        return <DisplayControlPreview {...allProps} />;
      case ControlDefinitionType.Group:
        return <GroupedControlPreview {...allProps} />;
      case ControlDefinitionType.Action:
        return <ActionControlPreview {...allProps} />;
      default:
        return <h1>Unknown {type}</h1>;
    }
  }
}

function ActionControlPreview({
  isSelected,
  item,
}: FormControlPreviewDataProps) {
  const {
    selected,
    renderer: { renderAction },
  } = usePreviewContext();

  return (
    <motion.div
      layout={defaultLayoutChange}
      layoutId={item.uniqueId.toString()}
      onClick={(e) => {
        e.stopPropagation();
        selected.value = item;
      }}
      style={{
        backgroundColor: isSelected ? "rgba(25, 118, 210, 0.08)" : undefined,
        position: "relative",
      }}
    >
      {renderAction({
        definition: item.value as ActionControlDefinition,
        visible: { canChange: false, value: true },
        onClick: () => {},
      })}
    </motion.div>
  );
}

function DisplayControlPreview({
  isSelected,
  item,
}: FormControlPreviewDataProps) {
  const {
    selected,
    renderer: { renderDisplay },
  } = usePreviewContext();
  return (
    <motion.div
      layout={defaultLayoutChange}
      layoutId={item.uniqueId.toString()}
      onMouseDownCapture={(e) => {
        e.stopPropagation();
        e.preventDefault();
      }}
      onClickCapture={(e) => {
        e.preventDefault();
        e.stopPropagation();
        selected.value = item;
      }}
      style={{
        backgroundColor: isSelected ? "rgba(25, 118, 210, 0.08)" : undefined,
        position: "relative",
      }}
    >
      {renderDisplay({
        definition: item.value as DisplayControlDefinition,
        visible: AlwaysVisible,
      })}
    </motion.div>
  );
}

function DataControlPreview({
  isSelected,
  isOver,
  item,
  fields,
  dropIndex,
}: FormControlPreviewDataProps) {
  const { selected, readonly, renderer, hooks } = usePreviewContext();
  const fieldDetails = item.value;
  const schemaField = useFindScalarField(fields, fieldDetails.field!);
  const isCollection = Boolean(schemaField?.collection);
  const control: Control<any> = useMemo(
    () => newControl(isCollection ? [undefined] : undefined),
    [isCollection],
  );
  const onlyForTypes = !isNullOrEmpty(schemaField?.onlyForTypes);

  return (
    <motion.div
      layout={defaultLayoutChange}
      layoutId={item.uniqueId.toString()}
      onMouseDownCapture={(e) => {
        e.stopPropagation();
        e.preventDefault();
      }}
      onClickCapture={(e) => {
        e.preventDefault();
        e.stopPropagation();
        selected.value = item;
      }}
      style={{
        backgroundColor: isSelected ? "rgba(25, 118, 210, 0.08)" : undefined,
        position: "relative",
      }}
    >
      <EditorDetails control={item} schemaVisibility={onlyForTypes} />
      {schemaField ? (
        renderRealField(schemaField)
      ) : (
        <div>No schema field: {fieldDetails.field}</div>
      )}
    </motion.div>
  );

  function renderRealField(field: SchemaField) {
    const definition = fieldDetails as DataControlDefinition;
    const dataProps = getDefaultScalarControlProperties(
      definition,
      field,
      AlwaysVisible,
      undefined,
      control,
      { fields: [], data: newControl({}), readonly, renderer, hooks },
    );
    const finalProps = !field.collection
      ? dataProps
      : {
          ...dataProps,
          array: makeArrayProps(),
        };
    return renderer.renderData(finalProps);

    function makeArrayProps(): ArrayRendererProps {
      return {
        control,
        definition,
        field,
        childCount: 1,
        childKey: (c) => 0,
        removeAction: (c) => createAction("Remove", () => {}, "removeElement"),
        addAction: createAction("Add", () => {}, "addElement"),
        renderChild: (c) =>
          renderer.renderData({ ...dataProps, control: control.elements[0] }),
      };
    }
  }
}

function GroupedControlPreview({ item, fields }: FormControlPreviewDataProps) {
  const {
    treeDrag,
    dropSuccess,
    selected,
    hooks,
    renderer: { renderGroup },
  } = usePreviewContext();

  const children = item.fields.children.elements ?? [];

  const {
    compoundField: cfc,
    type,
    title,
    dynamic,
    adornments,
    groupOptions,
  } = item.fields;

  const groupData = {
    compoundField: cfc.value,
    type: type.value,
    title: title.value,
    dynamic: dynamic.value,
    adornments: adornments.value,
    groupOptions: groupOptions.value,
  };

  const v = treeDrag.value;
  const dragChildIndex =
    v && v.draggedFrom && v.draggedFrom[0] === item
      ? v.draggedFrom[1]
      : undefined;
  const hasTarget = v && v.targetParent === item ? v : undefined;
  const isActive = v && v.draggedControl === item;
  const { compoundField } = groupData;
  const cf = useFieldLookup(fields, compoundField);
  const childFields = compoundField ? cf.fields.children : fields;

  const { setNodeRef, isOver } = useDroppable({
    id: item.uniqueId + "_bottom",
    data: controlDropData(item, children.length, dropSuccess),
  });

  const actualChildren = useMemo(() => {
    const withFields: [ControlForm, Control<SchemaFieldForm[]>][] =
      children.map((x) => [x, childFields]);
    if (hasTarget || dragChildIndex != null) {
      const splicing:
        | [number, number?]
        | [number, number, ...[ControlForm, Control<SchemaFieldForm[]>][]][] =
        dragChildIndex != null ? [[dragChildIndex, 1]] : [];
      if (hasTarget) {
        splicing.push([
          hasTarget.targetIndex,
          0,
          [hasTarget.draggedControl, hasTarget.dragFields ?? childFields],
        ]);
      }
      return update(withFields, {
        $splice: splicing,
      });
    }
    return withFields;
  }, [hasTarget?.targetIndex, dragChildIndex, children, childFields]);

  return (
    <motion.div
      layout
      layoutId={item.uniqueId.toString()}
      onClick={(e) => {
        e.stopPropagation();
        selected.value = item;
      }}
      style={{ position: "relative" }}
    >
      <EditorDetails control={item} />
      <LayoutGroup>
        {renderGroup({
          definition: groupData as Omit<GroupedControlsDefinition, "children">,
          childCount: actualChildren.length,
          visible: AlwaysVisible,
          renderChild,
          hooks,
          hideTitle: groupData.groupOptions?.hideTitle ?? false,
        })}
      </LayoutGroup>
      <div ref={setNodeRef} style={{ height: 5, marginTop: 20 }} />
    </motion.div>
  );

  function renderChild(i: number) {
    const [child, fields] = actualChildren[i];
    return (
      <FormControlPreview
        key={child.uniqueId}
        fields={fields}
        dropIndex={i}
        item={child}
        parent={item}
        noDrop={isActive}
      />
    );
  }
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
