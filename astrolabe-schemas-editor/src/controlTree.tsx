import {
  ControlTreeNode,
  treeNode,
  TreeNodeConfigure,
  TreeNodeRenderProps,
  TreeNodeStructure,
} from "@astroapps/ui-tree";
import { ControlDefinitionForm, SchemaFieldForm } from "./schemaSchemas";
import { Control, ControlSetup } from "@react-typed-forms/core";
import { ControlDefinitionType, FieldType } from "@react-typed-forms/schemas";
import { ControlForm, useFieldLookup } from "./index";
import React, { createContext, ReactNode, useContext } from "react";

export const ControlDefNodeType = "Control";
export const SchemaFieldNode = "Field";
export function isControlDefinitionNode(
  node: Control<any>,
): node is Control<ControlDefinitionForm> {
  return node.meta.nodeType === ControlDefNodeType;
}

export function isSchemaFieldNode(
  node: Control<any>,
): node is Control<SchemaFieldForm> {
  return node.meta.nodeType === SchemaFieldNode;
}

export function getGroupControlChildren(
  n: Control<any>,
): Control<ControlDefinitionForm[] | null> | undefined {
  return isControlDefinitionNode(n) ? n.fields.children : undefined;
}

export function makeControlTree(
  makeActions?: (
    node: ControlTreeNode,
    field: Control<SchemaFieldForm>,
  ) => ReactNode | undefined,
) {
  const ControlTreeStructure: (
    p?: TreeNodeConfigure<ControlDefinitionForm>,
  ) => ControlSetup<ControlDefinitionForm, TreeNodeStructure> = (p) => ({
    ...treeNode(
      ControlDefNodeType,
      (n) => n.fields.title,
      true,
      (b) =>
        b
          .withChildren(getGroupControlChildren)
          .withIcon((n) => {
            switch (n.fields.type.current.value) {
              case ControlDefinitionType.Group:
                return "folder";
              case ControlDefinitionType.Data:
                return "schema";
              case ControlDefinitionType.Action:
                return "bolt";
              case ControlDefinitionType.Display:
                return "wysiwyg";
              default:
                return "folder";
            }
          })
          .withDropping((nodeType) => nodeType === ControlDefNodeType)
          .withDragging()
          .withCustomRender((c, p) => (
            <ControlDefTreeNode
              key={c.uniqueId}
              control={c}
              {...p}
              makeActions={makeActions}
            />
          ))
          .and(p),
    ),
    fields: {
      children: { elems: ControlTreeStructure },
    },
  });
  return ControlTreeStructure;
}

const SchemaFieldContext = createContext<
  Control<SchemaFieldForm[]> | undefined
>(undefined);

export const SchemaFieldsProvider = SchemaFieldContext.Provider;

export function useSchemaFields(): Control<SchemaFieldForm[]> {
  const c = useContext(SchemaFieldContext);
  if (!c) throw "Need to wrap in <SchemaFieldsProvider/>";
  return c;
}

function ControlDefTreeNode({
  control: n,
  node,
  makeActions,
  children,
  renderItem,
}: {
  control: ControlForm;
  makeActions?: (
    n: ControlTreeNode,
    field: Control<SchemaFieldForm>,
  ) => React.ReactNode | undefined;
} & TreeNodeRenderProps) {
  const fields = useSchemaFields();
  const {
    title: { value: _title },
    type: { value: type },
    field: { value: field },
    compoundField: { value: compoundField },
  } = n.fields;
  const isGroup = type === "Group";
  const lookupField = isGroup ? compoundField : field;
  const schemaField = useFieldLookup(fields, lookupField);
  const isCompound = schemaField.fields.type.value === FieldType.Compound;
  const schemaFieldName = schemaField.fields.displayName.value;
  const title = _title ? _title : schemaFieldName ? schemaFieldName : field;
  return (
    <>
      {renderItem(
        title,
        makeActions?.(node, schemaField),
        isCompound || isGroup,
      )}
      {isCompound ? (
        <SchemaFieldsProvider value={schemaField.fields.children}>
          {children}
        </SchemaFieldsProvider>
      ) : (
        children
      )}
    </>
  );
}

export const SchemaFieldStructure: () => ControlSetup<
  SchemaFieldForm,
  TreeNodeStructure
> = () => ({
  ...treeNode<SchemaFieldForm>(
    SchemaFieldNode,
    (x) => x.fields.displayName,
    true,
    (b) =>
      b
        .withChildren((n) =>
          n.fields.type.value === "Compound" ? n.fields.children : undefined,
        )
        .withDragging()
        .withDropping((n) => n === SchemaFieldNode)
        .withCustomRender((n, p) => (
          <SchemaFieldTreeNode key={n.uniqueId} control={n} {...p} />
        ))
        .withIcon((n) => <SchemaFieldIcon state={n.fields.type} />),
  ),
  fields: {
    children: { elems: SchemaFieldStructure },
  },
});

function SchemaFieldIcon({ state }: { state: Control<string> }) {
  const type = state.value;
  return type;
}

function SchemaFieldTreeNode({
  control: n,
  children,
  renderItem,
}: {
  control: Control<SchemaFieldForm>;
} & TreeNodeRenderProps) {
  const _title = n.fields.displayName.value;
  const field = n.fields.field.value;
  const title = _title ? _title : field;
  return (
    <>
      {renderItem(title)}
      {children}
    </>
  );
}
