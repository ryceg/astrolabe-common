import {
  treeNode,
  TreeNodeConfigure,
  TreeNodeRenderProps,
  TreeNodeStructure,
} from "@astrolabe/ui-tree";
import { ControlDefinitionForm, SchemaFieldForm } from "./schemaSchemas";
import { Control, ControlSetup } from "@react-typed-forms/core";
import { ControlDefinitionType } from "@react-typed-forms/schemas";
import { ControlForm, useFieldLookup } from "./index";
import { createContext, ReactNode, useContext } from "react";

export const ControlDefNodeType = "Control";

export function isControlDefinitionNode(
  node: Control<any>,
): node is Control<ControlDefinitionForm> {
  return node.meta.nodeType === ControlDefNodeType;
}

export function makeControlTree(
  makeActions?: (node: ControlForm) => ReactNode | undefined,
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
          .withChildren((n) =>
            isControlDefinitionNode(n) &&
            n.fields.type.current.value === ControlDefinitionType.Group
              ? n.fields.children
              : undefined,
          )
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
          .withCustomRender((n, { children, renderItem }) => (
            <ControlDefTreeNode
              key={n.uniqueId}
              node={n}
              children={children}
              makeActions={makeActions}
              renderItem={renderItem}
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
  node: n,
  makeActions,
  children,
  renderItem,
}: {
  node: ControlForm;
  makeActions?: (n: ControlForm) => React.ReactNode | undefined;
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
  children =
    isGroup && compoundField ? (
      <SchemaFieldsProvider value={schemaField.fields.children}>
        {children}
      </SchemaFieldsProvider>
    ) : (
      children
    );
  const schemaFieldName = schemaField.fields.displayName.value;
  const title = _title ? _title : schemaFieldName ? schemaFieldName : field;
  return (
    <>
      {renderItem(title, makeActions?.(n))}
      {children}
    </>
  );
}
