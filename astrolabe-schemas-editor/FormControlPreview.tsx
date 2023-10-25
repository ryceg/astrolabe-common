import { ControlForm, useFindScalarField, useSnippetDroppable } from "./index";
import { Control, newControl, useControlValue } from "@react-typed-forms/core";
import React, { Key, ReactElement, useMemo } from "react";
import { Visibility } from "@mui/icons-material";
import { isNullOrEmpty } from "../../arrayUtils";
import { useIsSelected } from "./tree";
import { SchemaFieldForm } from "./schemaSchemas";
import { useDroppable } from "@dnd-kit/core";
import { LayoutGroup, motion } from "framer-motion";
import update from "immutability-helper";
import {
  ActionControlDefinition,
  ControlDefinitionType,
  DataControlDefinition,
  DisplayControlDefinition,
  DynamicPropertyType,
  getDefaultScalarControlProperties,
  GroupedControlsDefinition,
  useFormRendererComponents,
} from "@react-typed-forms/schemas";
import { defaultFormEditHooks, useFieldLookup } from "../../internalForm";
import { DragData, DropData } from "./dragndrop";
import { useScrollIntoView } from "./useScrollIntoView";

export interface ControlDragState {
  draggedFrom?: [Control<any>, number];
  targetIndex: number;
  draggedControl: ControlForm;
  targetParent: ControlForm;
  dragFields?: Control<SchemaFieldForm[]>;
}

export interface FormControlPreviewContext {
  item: ControlForm;
  parent?: ControlForm;
  dropIndex: number;
  selected: Control<Control<any> | undefined>;
  fields: Control<SchemaFieldForm[]>;
  treeDrag: Control<ControlDragState | undefined>;
  noDrop?: boolean;
  dropSuccess: (drag: DragData, drop: DropData) => void;
  readonly?: boolean;
}

export interface FormControlPreviewData {
  isSelected: boolean;
  isOver: boolean;
}

const defaultLayoutChange = "position";

export function FormControlPreview({
  context,
}: {
  context: FormControlPreviewContext;
}) {
  const { item, selected, parent, dropIndex, noDrop, dropSuccess } = context;
  const type = item.fields.type.value;
  const isSelected = useIsSelected(selected, item);
  const scrollRef = useScrollIntoView(isSelected);
  const controlDrop = useSnippetDroppable(parent, dropIndex, dropSuccess);
  const { setNodeRef, isOver } = useDroppable({
    id: item.uniqueId,
    disabled: Boolean(noDrop),
    data: controlDrop,
  });
  const data: FormControlPreviewData = {
    isSelected,
    isOver,
  };
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
    switch (type) {
      case ControlDefinitionType.Data:
        return <DataControlPreview context={context} data={data} />;
      case ControlDefinitionType.Display:
        return <DisplayControlPreview context={context} data={data} />;
      case ControlDefinitionType.Group:
        return <GroupedControlPreview context={context} data={data} />;
      case ControlDefinitionType.Action:
        return <ActionControlPreview context={context} data={data} />;
      default:
        return <h1>Unknown {type}</h1>;
    }
  }
}

function ActionControlPreview({
  context: { item, fields, selected },
  data: { isSelected },
}: {
  context: FormControlPreviewContext;
  data: FormControlPreviewData;
}) {
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
        properties: { visible: true, onClick: () => {} },
      })}
    </motion.div>
  );
}

function DisplayControlPreview({
  context: { item, selected },
  data: { isSelected },
}: {
  context: FormControlPreviewContext;
  data: FormControlPreviewData;
}) {
  const { renderDisplay } = useFormRendererComponents();
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
      {renderDisplay({
        definition: item.value as DisplayControlDefinition,
        properties: { visible: true },
      })}
    </motion.div>
  );
}

function DataControlPreview({
  context: { item, fields, selected, dropIndex, readonly },
  data: { isSelected, isOver },
}: {
  context: FormControlPreviewContext;
  data: FormControlPreviewData;
}) {
  const fieldDetails = item.value;
  const schemaField = useFindScalarField(fields, fieldDetails.field!);
  const isCollection = Boolean(schemaField?.collection);
  const fc = useMemo(
    () => newControl(isCollection ? [] : undefined),
    [isCollection]
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
          <div style={{ position: "relative" }}>
            <Visibility
              fontSize={"small"}
              style={{ position: "absolute", right: "0px" }}
            />
          </div>
        )}

        {schemaField ? (
          renderer.renderData(
            {
              definition: fieldDetails as DataControlDefinition,
              field: schemaField,
              properties: getDefaultScalarControlProperties(
                fieldDetails as DataControlDefinition,
                schemaField,
                true,
                undefined,
                fc,
                readonly
              ),
            },
            fc,
            false,
            renderer
          )
        ) : (
          <div>No schema field: {fieldDetails.field}</div>
        )}
      </>
    </motion.div>
  );
}

function GroupedControlPreview({
  context: { item, fields, selected, treeDrag, dropSuccess, readonly },
  data: { isSelected },
}: {
  data: FormControlPreviewData;
  context: FormControlPreviewContext;
}) {
  const { renderGroup } = useFormRendererComponents();

  const children = useControlValue(() => item.fields.children.elements ?? []);

  const groupData = useControlValue(() => {
    const { compoundField, type, title, dynamic, adornments, groupOptions } =
      item.fields;
    return {
      compoundField: compoundField.value,
      type: type.value,
      title: title.value,
      dynamic: dynamic.value,
      adornments: adornments.value,
      groupOptions: groupOptions.value,
    };
  });

  const { dragChildIndex, hasTarget, isActive } = useControlValue(() => {
    const v = treeDrag.value;
    const dragChildIndex =
      v && v.draggedFrom && v.draggedFrom[0] === item
        ? v.draggedFrom[1]
        : undefined;
    const hasTarget = v && v.targetParent === item ? v : undefined;
    return {
      dragChildIndex,
      hasTarget,
      isActive: v && v.draggedControl === item,
    };
  });
  const { compoundField } = groupData;
  const cf = useFieldLookup(fields, compoundField);
  const childFields = compoundField ? cf.fields.children : fields;
  const controlDrop = useSnippetDroppable(item, children.length, dropSuccess);

  const { setNodeRef, isOver } = useDroppable({
    id: item.uniqueId + "_bottom",
    data: controlDrop,
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
          properties: { visible: true, hooks: defaultFormEditHooks },
          renderChild,
        })}
      </LayoutGroup>
      <div ref={setNodeRef} style={{ height: 5, marginTop: 20 }} />
    </motion.div>
  );

  function renderChild(
    i: number,
    _wrapChild: (key: Key, db: ReactElement) => ReactElement
  ) {
    const [child, fields] = actualChildren[i];
    return _wrapChild(
      child.uniqueId,
      <FormControlPreview
        context={{
          selected,
          treeDrag,
          item: child,
          parent: item,
          dropIndex: i,
          fields,
          noDrop: isActive,
          dropSuccess,
          readonly,
        }}
      />
    );
  }
}
