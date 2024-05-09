import {
  ControlDefinition,
  DataExpression,
  DataMatchExpression,
  DynamicPropertyType,
  EntityExpression,
  ExpressionType,
  isDataControlDefinition,
  JsonataExpression,
  SchemaField,
  SchemaInterface,
} from "./types";
import React, {
  MutableRefObject,
  useCallback,
  useEffect,
  useMemo,
  useRef,
} from "react";
import {
  addAfterChangesCallback,
  collectChanges,
  Control,
  makeChangeTracker,
  useComputed,
  useControl,
  useRefState,
} from "@react-typed-forms/core";

import {
  ControlDataContext,
  defaultValueForField,
  DynamicHookGenerator,
  findField,
  getDisplayOnlyOptions,
  getTypeField,
  HookDep,
  isControlReadonly,
  jsonPathString,
  lookupChildControl,
  makeHook,
  toDepString,
  useUpdatedRef,
} from "./util";
import jsonata from "jsonata";
import { trackedStructure, useCalculatedControl } from "./internal";
import { DataContext } from "./controlRender";

export type EvalExpressionHook<A = any> = DynamicHookGenerator<
  Control<A | undefined>,
  ControlDataContext
>;

export type UseEvalExpressionHook = (
  expr: EntityExpression | undefined,
) => DynamicHookGenerator<Control<any> | undefined, ControlDataContext>;

export function useEvalVisibilityHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
  schemaField?: SchemaField,
): EvalExpressionHook<boolean> {
  const dynamicVisibility = useEvalDynamicHook(
    definition,
    DynamicPropertyType.Visible,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicVisibility,
    (ctx, { schemaField, definition }) =>
      useComputed(() => {
        return (
          matchesType(ctx, schemaField?.onlyForTypes) &&
          (!schemaField ||
            !hideDisplayOnly(ctx, schemaField, definition, ctx.schemaInterface))
        );
      }),
    { schemaField, definition },
  );
}

export function useEvalReadonlyHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
): EvalExpressionHook<boolean> {
  const dynamicReadonly = useEvalDynamicHook(
    definition,
    DynamicPropertyType.Readonly,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicReadonly,
    (ctx, { definition }) =>
      useCalculatedControl(() => isControlReadonly(definition)),
    { definition },
  );
}

export function useEvalStyleHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  property: DynamicPropertyType,
  definition: ControlDefinition,
): EvalExpressionHook<React.CSSProperties> {
  const dynamicStyle = useEvalDynamicHook(
    definition,
    property,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicStyle,
    () => useControl(undefined),
    undefined,
  );
}

export function useEvalAllowedOptionsHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
): EvalExpressionHook<any[]> {
  const dynamicAllowed = useEvalDynamicHook(
    definition,
    DynamicPropertyType.AllowedOptions,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicAllowed,
    () => useControl([]),
    undefined,
  );
}

export function useEvalDisabledHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
): EvalExpressionHook<boolean> {
  const dynamicDisabled = useEvalDynamicHook(
    definition,
    DynamicPropertyType.Disabled,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicDisabled,
    () => useControl(false),
    undefined,
  );
}

export function useEvalDisplayHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
): DynamicHookGenerator<
  Control<string | undefined> | undefined,
  ControlDataContext
> {
  return useEvalDynamicHook(
    definition,
    DynamicPropertyType.Display,
    useEvalExpressionHook,
  );
}
export function useEvalDefaultValueHook(
  useEvalExpressionHook: UseEvalExpressionHook,
  definition: ControlDefinition,
  schemaField?: SchemaField,
): EvalExpressionHook {
  const dynamicValue = useEvalDynamicHook(
    definition,
    DynamicPropertyType.DefaultValue,
    useEvalExpressionHook,
  );
  return makeDynamicPropertyHook(
    dynamicValue,
    (ctx, { definition, schemaField }) => {
      return useComputed(calcDefault);
      function calcDefault() {
        const [required, dcv] = isDataControlDefinition(definition)
          ? [definition.required, definition.defaultValue]
          : [false, undefined];
        return (
          dcv ??
          (schemaField
            ? defaultValueForField(schemaField, required)
            : undefined)
        );
      }
    },
    { definition, schemaField },
  );
}

function useDataExpression(
  fvExpr: DataExpression,
  fields: SchemaField[],
  data: DataContext,
) {
  const refField = findField(fields, fvExpr.field);
  const otherField = refField
    ? lookupChildControl(data, refField.field)
    : undefined;
  return useCalculatedControl(() => otherField?.value);
}

function useDataMatchExpression(
  fvExpr: DataMatchExpression,
  fields: SchemaField[],
  data: DataContext,
) {
  const refField = findField(fields, fvExpr.field);
  const otherField = refField
    ? lookupChildControl(data, refField.field)
    : undefined;
  return useComputed(() => {
    const fv = otherField?.value;
    return Array.isArray(fv) ? fv.includes(fvExpr.value) : fv === fvExpr.value;
  });
}

export function defaultEvalHooks(
  expr: EntityExpression,
  context: ControlDataContext,
) {
  switch (expr.type) {
    case ExpressionType.Jsonata:
      return useJsonataExpression(
        (expr as JsonataExpression).expression,
        context,
      );
    case ExpressionType.Data:
      return useDataExpression(expr as DataExpression, context.fields, context);
    case ExpressionType.DataMatch:
      return useDataMatchExpression(
        expr as DataMatchExpression,
        context.fields,
        context,
      );
    default:
      return useControl(undefined);
  }
}

export const defaultUseEvalExpressionHook =
  makeEvalExpressionHook(defaultEvalHooks);

export function makeEvalExpressionHook(
  f: (expr: EntityExpression, context: ControlDataContext) => Control<any>,
): UseEvalExpressionHook {
  return (expr) => ({
    deps: expr?.type,
    state: expr,
    runHook: (ctx: ControlDataContext, state: EntityExpression | undefined) => {
      return state ? f(state, ctx) : undefined;
    },
  });
}

export function useEvalDynamicHook(
  definition: ControlDefinition,
  type: DynamicPropertyType,
  useEvalExpressionHook: UseEvalExpressionHook,
): DynamicHookGenerator<Control<any> | undefined, ControlDataContext> {
  const expression = definition.dynamic?.find((x) => x.type === type);
  return useEvalExpressionHook(expression?.expr);
}

export function matchesType(
  context: ControlDataContext,
  types?: string[] | null,
) {
  if (types == null || types.length === 0) return true;
  const typeField = getTypeField(context);
  return typeField && types.includes(typeField.value);
}

export function hideDisplayOnly(
  context: ControlDataContext,
  field: SchemaField,
  definition: ControlDefinition,
  schemaInterface: SchemaInterface,
) {
  const displayOptions = getDisplayOnlyOptions(definition);
  return (
    displayOptions &&
    !displayOptions.emptyText &&
    schemaInterface.isEmptyValue(
      field,
      lookupChildControl(context, field.field)?.value,
    )
  );
}

export function useJsonataExpression(
  jExpr: string,
  dataContext: DataContext,
  bindings?: () => Record<string, any>,
): Control<any> {
  const pathString = jsonPathString(dataContext.path, (x) => `#$i[${x}]`);
  const fullExpr = pathString ? pathString + ".(" + jExpr + ")" : jExpr;
  const compiledExpr = useMemo(() => {
    try {
      return jsonata(fullExpr);
    } catch (e) {
      console.error(e);
      return jsonata("null");
    }
  }, [fullExpr]);
  const control = useControl();
  const listenerRef = useRef<() => void>();
  const [ref] = useRefState(() =>
    makeChangeTracker(() => {
      const l = listenerRef.current;
      if (l) {
        listenerRef.current = undefined;
        addAfterChangesCallback(() => {
          l();
          listenerRef.current = l;
        });
      }
    }),
  );
  useEffect(() => {
    listenerRef.current = apply;
    apply();
    async function apply() {
      const [collect, updateSubscriptions] = ref.current;
      try {
        const bindingData = bindings
          ? collectChanges(collect, bindings)
          : undefined;
        control.value = await compiledExpr.evaluate(
          trackedStructure(dataContext.data, collect),
          bindingData,
        );
      } finally {
        updateSubscriptions();
      }
    }
    return () => ref.current[1](true);
  }, [compiledExpr]);
  return control;
}

export function useEvalLabelText(
  useExpr: UseEvalExpressionHook,
  definition: ControlDefinition,
): EvalExpressionHook<string | null> {
  const dynamicValue = useEvalDynamicHook(
    definition,
    DynamicPropertyType.Label,
    useExpr,
  );
  return makeDynamicPropertyHook(
    dynamicValue,
    () => useControl(null),
    undefined,
  );
}

function makeDynamicPropertyHook<A, S = undefined>(
  dynamicValue: DynamicHookGenerator<
    Control<any> | undefined,
    ControlDataContext
  >,
  makeDefault: (ctx: ControlDataContext, s: S) => Control<A | undefined>,
  state: S,
  deps?: HookDep,
): EvalExpressionHook<A> {
  return {
    deps:
      deps !== undefined
        ? dynamicValue.deps
        : [deps, dynamicValue.deps].map(toDepString).join(),
    runHook: (ctx, s) => {
      return dynamicValue.runHook(ctx, s[0]) ?? makeDefault(ctx, s[1]);
    },
    state: [dynamicValue.state, state],
  };
}
