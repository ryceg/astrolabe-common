import { Control, newControl } from "@react-typed-forms/core";
import { SchemaField } from "@react-typed-forms/schemas";
import {
  ControlDefinitionForm,
  SchemaFieldForm,
  defaultSchemaFieldForm,
} from "./schemaSchemas";
import { ReactElement } from "react";

export type ControlForm = Control<ControlDefinitionForm>;

export interface ControlDragState {
  draggedFrom?: [Control<any>, number];
  targetIndex: number;
  draggedControl: ControlForm;
  targetParent: ControlForm;
  dragFields?: Control<SchemaFieldForm[]>;
}

export interface DragData {
  overlay: (dd: DragData) => ReactElement;
}

export interface DropData {
  success: (drag: DragData, drop: DropData) => void;
}

export interface ControlDropData extends DropData {
  parent?: ControlForm;
  dropIndex: number;
}

export const NonExistantField = newControl<SchemaFieldForm>(
  defaultSchemaFieldForm,
);

export function useFieldLookup(
  fields: Control<SchemaFieldForm[] | undefined>,
  field: string | undefined | null,
): Control<SchemaFieldForm> {
  return (
    fields.elements?.find((x) => x.fields.field.value === field) ??
    NonExistantField
  );
}

export function useFindScalarField(
  fields: Control<SchemaFieldForm[]>,
  field: string,
): SchemaField | undefined {
  const fc = useFieldLookup(fields, field);
  return fc === NonExistantField ? undefined : fc.value;
}

export function controlDropData(
  parent: ControlForm | undefined,
  dropIndex: number,
  dropSuccess: (drag: DragData, drop: DropData) => void,
): ControlDropData {
  return {
    dropIndex,
    parent,
    success: dropSuccess,
  };
}
