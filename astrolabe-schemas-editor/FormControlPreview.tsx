import { Control, newControl, useControlValue } from "@react-typed-forms/core";
import React, {
  createContext,
  Key,
  ReactElement,
  ReactNode,
  useContext,
  useMemo,
} from "react";
import { isNullOrEmpty } from "@astrolabe/client/util/arrays";
import { SchemaFieldForm } from "./schemaSchemas";
import { useDroppable } from "@dnd-kit/core";
import { LayoutGroup, motion } from "framer-motion";
import update from "immutability-helper";
import {
  ActionControlDefinition,
  AlwaysVisible,
  ControlDefinitionType,
  DataControlDefinition,
  DisplayControlDefinition,
  DynamicPropertyType,
  FormEditHooks,
  getDefaultScalarControlProperties,
  GroupedControlsDefinition,
  useFormRendererComponents,
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
  const { selected } = usePreviewContext();
  const { renderAction } = useFormRendererComponents();

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
  const { renderDisplay } = useFormRendererComponents();
  const { selected } = usePreviewContext();
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
  const { selected, readonly, VisibilityIcon } = usePreviewContext();
  const fieldDetails = item.value;
  const schemaField = useFindScalarField(fields, fieldDetails.field!);
  const isCollection = Boolean(schemaField?.collection);
  const fc = useMemo(
    () => newControl(isCollection ? [] : undefined),
    [isCollection],
  );
  const hasVisibilityScripting =
    !isNullOrEmpty(schemaField?.onlyForTypes) ||
    fieldDetails.dynamic?.some((x) => x.type === DynamicPropertyType.Visible);

  const renderer = useFormRendererComponents();
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
      }}
    >
      <>
        {/*<div style={{ position: "relative" }}>*/}
        {/*  <div style={{ position: "absolute", right: "100px" }}>*/}
        {/*    {isOver ? "O -" : ""}*/}
        {/*    {item.uniqueId} - {dropIndex}*/}
        {/*  </div>*/}
        {/*</div>*/}
        {hasVisibilityScripting && (
          <div style={{ position: "relative" }}>{VisibilityIcon}</div>
        )}

        {schemaField ? (
          renderer.renderData(
            getDefaultScalarControlProperties(
              fieldDetails as DataControlDefinition,
              schemaField,
              AlwaysVisible,
              undefined,
              fc,
              { fields: [], data: newControl({}), readonly },
            ),
          )
        ) : (
          <div>No schema field: {fieldDetails.field}</div>
        )}
      </>
    </motion.div>
  );
}

function GroupedControlPreview({ item, fields }: FormControlPreviewDataProps) {
  const { treeDrag, dropSuccess, selected, hooks } = usePreviewContext();
  const { renderGroup } = useFormRendererComponents();

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
    >
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
