import {
  CompoundField,
  ControlDefinition,
  ControlDefinitionType,
  DataControlDefinition,
  DataMatchExpression,
  DisplayControlDefinition,
  DisplayDataType,
  DynamicProperty,
  DynamicPropertyType,
  EntityExpression,
  ExpressionType,
  GroupedControlsDefinition,
  GroupRenderType,
  HtmlDisplay,
  JsonataExpression,
  SchemaField,
  TextDisplay,
} from "./types";
import { ActionRendererProps } from "./controlRender";
import { useMemo } from "react";
import { addMissingControls, isCompoundField } from "./util";
import { mergeField } from "./schemaBuilder";

export function dataControl(
  field: string,
  title?: string | null,
  options?: Partial<DataControlDefinition>,
): DataControlDefinition {
  return { type: ControlDefinitionType.Data, field, title, ...options };
}

export function textDisplayControl(
  text: string,
  options?: Partial<DisplayControlDefinition>,
): DisplayControlDefinition {
  return {
    type: ControlDefinitionType.Display,
    displayData: { type: DisplayDataType.Text, text } as TextDisplay,
    ...options,
  };
}

export function htmlDisplayControl(
  html: string,
  options?: Partial<DisplayControlDefinition>,
): DisplayControlDefinition {
  return {
    type: ControlDefinitionType.Display,
    displayData: { type: DisplayDataType.Html, html } as HtmlDisplay,
    ...options,
  };
}

export function dynamicDefaultValue(expr: EntityExpression): DynamicProperty {
  return { type: DynamicPropertyType.DefaultValue, expr };
}

export function dynamicReadonly(expr: EntityExpression): DynamicProperty {
  return { type: DynamicPropertyType.Readonly, expr };
}

export function dynamicVisibility(expr: EntityExpression): DynamicProperty {
  return { type: DynamicPropertyType.Visible, expr };
}

export function dynamicDisabled(expr: EntityExpression): DynamicProperty {
  return { type: DynamicPropertyType.Disabled, expr };
}

export function fieldEqExpr(field: string, value: any): DataMatchExpression {
  return { type: ExpressionType.DataMatch, field, value };
}
export function jsonataExpr(expression: string): JsonataExpression {
  return { type: ExpressionType.Jsonata, expression };
}

export function groupedControl(
  children: ControlDefinition[],
  title?: string,
  options?: Partial<GroupedControlsDefinition>,
): GroupedControlsDefinition {
  return {
    type: ControlDefinitionType.Group,
    children,
    title,
    groupOptions: { type: "Standard", hideTitle: !title },
    ...options,
  };
}
export function compoundControl(
  field: string,
  title: string | undefined,
  children: ControlDefinition[],
  options?: Partial<DataControlDefinition>,
): DataControlDefinition {
  return {
    type: ControlDefinitionType.Data,
    field,
    children,
    title,
    renderOptions: { type: "Standard" },
    ...options,
  };
}

export function createAction(
  actionId: string,
  onClick: () => void,
  actionText?: string,
): ActionRendererProps {
  return { actionId, onClick, actionText: actionText ?? actionId };
}

export const emptyGroupDefinition: GroupedControlsDefinition = {
  type: ControlDefinitionType.Group,
  children: [],
  groupOptions: { type: GroupRenderType.Standard, hideTitle: true },
};

export function useControlDefinitionForSchema(
  sf: SchemaField[],
  definition: GroupedControlsDefinition = emptyGroupDefinition,
): GroupedControlsDefinition {
  return useMemo<GroupedControlsDefinition>(
    () => ({
      ...definition,
      children: addMissingControls(sf, definition.children ?? []),
    }),
    [sf, definition],
  );
}

export interface CustomRenderOptions {
  value: string;
  name: string;
  fields: SchemaField[];
}

export function addCustomDataRenderOptions(
  controlFields: SchemaField[],
  customRenderOptions: CustomRenderOptions[],
): SchemaField[] {
  return controlFields.map((x) =>
    x.field === "renderOptions" && isCompoundField(x) ? addRenderOptions(x) : x,
  );

  function addRenderOptions(roField: CompoundField): CompoundField {
    const children = roField.children;
    const withTypes = children.map((x) =>
      x.field === "type" ? addRenderOptionType(x) : x,
    );
    return {
      ...roField,
      children: customRenderOptions.reduce(
        (renderOptionFields, ro) =>
          ro.fields
            .map((x) => ({ ...x, onlyForTypes: [ro.value] }))
            .reduce((af, x) => mergeField(x, af), renderOptionFields),
        withTypes,
      ),
    };
  }

  function addRenderOptionType(typeField: SchemaField): SchemaField {
    const options = typeField.options ?? [];
    return {
      ...typeField,
      options: [
        ...options,
        ...customRenderOptions.map(({ name, value }) => ({ name, value })),
      ],
    };
  }
}
