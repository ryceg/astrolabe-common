import React, {
  CSSProperties,
  FC,
  Fragment,
  Key,
  ReactNode,
  useCallback,
  useEffect,
} from "react";
import {
  addElement,
  Control,
  ControlChange,
  newControl,
  removeElement,
  trackControlChange,
  useComponentTracking,
  useControl,
  useControlEffect,
} from "@react-typed-forms/core";
import {
  AdornmentPlacement,
  ControlAdornment,
  ControlDefinition,
  DataControlDefinition,
  DisplayData,
  DynamicPropertyType,
  FieldOption,
  GroupRenderOptions,
  isActionControlsDefinition,
  isDataControlDefinition,
  isDisplayControlsDefinition,
  isGroupControlsDefinition,
  RenderOptions,
  SchemaField,
  SchemaInterface,
} from "./types";
import {
  ControlDataContext,
  elementValueForField,
  fieldDisplayName,
  findField,
  isCompoundField,
  useUpdatedRef,
} from "./util";
import { dataControl } from "./controlBuilder";
import {
  defaultUseEvalExpressionHook,
  useEvalAllowedOptionsHook,
  useEvalDefaultValueHook,
  useEvalDisabledHook,
  useEvalDisplayHook,
  UseEvalExpressionHook,
  useEvalReadonlyHook,
  useEvalStyleHook,
  useEvalVisibilityHook,
} from "./hooks";
import { useValidationHook } from "./validators";
import { cc, useCalculatedControl } from "./internal";
import { defaultSchemaInterface } from "./schemaInterface";

export interface FormRenderer {
  renderData: (
    props: DataRendererProps,
  ) => (layout: ControlLayoutProps) => ControlLayoutProps;
  renderGroup: (
    props: GroupRendererProps,
  ) => (layout: ControlLayoutProps) => ControlLayoutProps;
  renderDisplay: (props: DisplayRendererProps) => ReactNode;
  renderAction: (props: ActionRendererProps) => ReactNode;
  renderArray: (props: ArrayRendererProps) => ReactNode;
  renderAdornment: (props: AdornmentProps) => AdornmentRenderer;
  renderLabel: (
    props: LabelRendererProps,
    labelStart: ReactNode,
    labelEnd: ReactNode,
  ) => ReactNode;
  renderLayout: (props: ControlLayoutProps) => RenderedControl;
  renderVisibility: (props: VisibilityRendererProps) => ReactNode;
}

export interface AdornmentProps {
  adornment: ControlAdornment;
}

export const AppendAdornmentPriority = 0;
export const WrapAdornmentPriority = 1000;

export interface AdornmentRenderer {
  apply(children: RenderedLayout): void;
  adornment?: ControlAdornment;
  priority: number;
}

export interface ArrayRendererProps {
  addAction?: ActionRendererProps;
  required: boolean;
  removeAction?: (elemIndex: number) => ActionRendererProps;
  elementCount: number;
  renderElement: (elemIndex: number) => ReactNode;
  elementKey: (elemIndex: number) => Key;
  arrayControl?: Control<any[] | undefined | null>;
  className?: string;
  style?: React.CSSProperties;
}
export interface Visibility {
  visible: boolean;
  showing: boolean;
}

export interface RenderedLayout {
  labelStart?: ReactNode;
  labelEnd?: ReactNode;
  controlStart?: ReactNode;
  controlEnd?: ReactNode;
  label?: ReactNode;
  children?: ReactNode;
  errorControl?: Control<any>;
  className?: string;
  style?: React.CSSProperties;
}

export interface RenderedControl {
  children: ReactNode;
  className?: string;
  style?: React.CSSProperties;
  divRef?: (cb: HTMLElement | null) => void;
}

export interface VisibilityRendererProps extends RenderedControl {
  visibility: Control<Visibility | undefined>;
}

export interface ControlLayoutProps {
  label?: LabelRendererProps;
  errorControl?: Control<any>;
  adornments?: AdornmentRenderer[];
  children?: ReactNode;
  processLayout?: (props: ControlLayoutProps) => ControlLayoutProps;
  className?: string | null;
  style?: React.CSSProperties;
}

export enum LabelType {
  Control,
  Group,
}
export interface LabelRendererProps {
  type: LabelType;
  hide?: boolean | null;
  label: ReactNode;
  required?: boolean | null;
  forId?: string;
}
export interface DisplayRendererProps {
  data: DisplayData;
  display?: Control<string | undefined>;
  className?: string;
  style?: React.CSSProperties;
}

export interface GroupRendererProps {
  renderOptions: GroupRenderOptions;
  childCount: number;
  renderChild: (child: number) => ReactNode;
  className?: string;
  style?: React.CSSProperties;
}

export interface DataRendererProps {
  definition: DataControlDefinition;
  renderOptions: RenderOptions;
  field: SchemaField;
  id: string;
  control: Control<any>;
  readonly: boolean;
  required: boolean;
  options: FieldOption[] | undefined | null;
  hidden: boolean;
  className?: string;
  style?: React.CSSProperties;
  dataContext: ControlDataContext;
  childCount: number;
  renderChild: ChildRenderer;
  toArrayProps?: () => ArrayRendererProps;
}

export interface ActionRendererProps {
  actionId: string;
  actionText: string;
  onClick: () => void;
  className?: string;
  style?: React.CSSProperties;
}

export interface ControlRenderProps {
  control: Control<any>;
  parentPath?: JsonPath[];
}

export interface FormContextOptions {
  readonly?: boolean | null;
  hidden?: boolean | null;
  disabled?: boolean | null;
}

export interface DataControlProps {
  definition: DataControlDefinition;
  field: SchemaField;
  dataContext: ControlDataContext;
  control: Control<any>;
  options: FormContextOptions;
  style: React.CSSProperties | undefined;
  childCount: number;
  renderChild: ChildRenderer;
  allowedOptions?: Control<any[] | undefined>;
  elementRenderer?: (elemIndex: number) => ReactNode;
}
export type CreateDataProps = (
  controlProps: DataControlProps,
) => DataRendererProps;

export type JsonPath = string | number;

export interface DataContext {
  data: Control<any>;
  path: JsonPath[];
}
export interface ControlRenderOptions extends FormContextOptions {
  useDataHook?: (c: ControlDefinition) => CreateDataProps;
  useEvalExpressionHook?: UseEvalExpressionHook;
  clearHidden?: boolean;
  schemaInterface?: SchemaInterface;
}
export function useControlRenderer(
  definition: ControlDefinition,
  fields: SchemaField[],
  renderer: FormRenderer,
  options: ControlRenderOptions = {},
): FC<ControlRenderProps> {
  const dataProps = options.useDataHook?.(definition) ?? defaultDataProps;
  const schemaInterface = options.schemaInterface ?? defaultSchemaInterface;
  const useExpr = options.useEvalExpressionHook ?? defaultUseEvalExpressionHook;

  const schemaField = lookupSchemaField(definition, fields);
  const useDefaultValue = useEvalDefaultValueHook(
    useExpr,
    definition,
    schemaField,
  );
  const useIsVisible = useEvalVisibilityHook(useExpr, definition, schemaField);
  const useIsReadonly = useEvalReadonlyHook(useExpr, definition);
  const useIsDisabled = useEvalDisabledHook(useExpr, definition);
  const useAllowedOptions = useEvalAllowedOptionsHook(useExpr, definition);
  const useCustomStyle = useEvalStyleHook(
    useExpr,
    DynamicPropertyType.Style,
    definition,
  );
  const useLayoutStyle = useEvalStyleHook(
    useExpr,
    DynamicPropertyType.LayoutStyle,
    definition,
  );
  const useDynamicDisplay = useEvalDisplayHook(useExpr, definition);
  const useValidation = useValidationHook(definition);
  const r = useUpdatedRef({ options, definition, fields, schemaField });

  const Component = useCallback(
    ({ control: rootControl, parentPath = [] }: ControlRenderProps) => {
      const stopTracking = useComponentTracking();
      try {
        const { definition: c, options, fields, schemaField } = r.current;
        const parentDataContext: ControlDataContext = {
          fields,
          schemaInterface,
          data: rootControl,
          path: parentPath,
        };
        const readonlyControl = useIsReadonly(parentDataContext);
        const disabledControl = useIsDisabled(parentDataContext);
        const visibleControl = useIsVisible(parentDataContext);
        const displayControl = useDynamicDisplay(parentDataContext);
        const customStyle = useCustomStyle(parentDataContext).value;
        const layoutStyle = useLayoutStyle(parentDataContext).value;
        const visible = visibleControl.current.value;
        const visibility = useControl<Visibility | undefined>(() =>
          visible != null
            ? {
                visible,
                showing: visible,
              }
            : undefined,
        );
        useControlEffect(
          () => visibleControl.value,
          (visible) => {
            if (visible != null)
              visibility.setValue((ex) => ({
                visible,
                showing: ex ? ex.showing : visible,
              }));
          },
        );

        const allowedOptions = useAllowedOptions(parentDataContext);
        const defaultValueControl = useDefaultValue(parentDataContext);
        const [parentControl, control, controlDataContext] = getControlData(
          schemaField,
          parentDataContext,
        );
        useControlEffect(
          () => [
            visibility.value,
            defaultValueControl.value,
            control,
            isDataControlDefinition(definition) && definition.dontClearHidden,
            parentControl?.isNull,
          ],
          ([vc, dv, cd, dontClear, parentNull]) => {
            if (vc && cd && vc.visible === vc.showing) {
              if (!vc.visible) {
                if (options.clearHidden && !dontClear) {
                  cd.value = undefined;
                }
              } else if (cd.value == null) {
                cd.value = dv;
              }
            }
            if (parentNull && parentControl?.isNull) {
              parentControl.value = {};
            }
          },
          true,
        );
        const myOptions = useCalculatedControl<FormContextOptions>(() => ({
          hidden: options.hidden || !visibility.fields?.showing.value,
          readonly: options.readonly || readonlyControl.value,
          disabled: options.disabled || disabledControl.value,
        })).value;
        useValidation(
          control ?? newControl(null),
          !!myOptions.hidden,
          parentDataContext,
        );
        const childRenderers: FC<ControlRenderProps>[] =
          c.children?.map((cd) =>
            useControlRenderer(cd, controlDataContext.fields, renderer, {
              ...options,
              ...myOptions,
            }),
          ) ?? [];

        useEffect(() => {
          if (control && typeof myOptions.disabled === "boolean")
            control.disabled = myOptions.disabled;
        }, [control, myOptions.disabled]);
        if (parentControl?.isNull) return <></>;

        const adornments =
          definition.adornments?.map((x) =>
            renderer.renderAdornment({ adornment: x }),
          ) ?? [];
        const labelAndChildren = renderControlLayout({
          definition: c,
          renderer,
          childCount: childRenderers.length,
          renderChild: (k, i, props) => {
            const RenderChild = childRenderers[i];
            return <RenderChild key={k} {...props} />;
          },
          createDataProps: dataProps,
          formOptions: myOptions,
          dataContext: controlDataContext,
          control: displayControl ?? control,
          schemaField,
          displayControl,
          style: customStyle,
          allowedOptions,
        });
        const renderedControl = renderer.renderLayout({
          ...labelAndChildren,
          adornments,
          className: c.layoutClass,
          style: layoutStyle,
        });
        return renderer.renderVisibility({ visibility, ...renderedControl });
      } finally {
        stopTracking();
      }
    },
    [
      r,
      dataProps,
      useIsVisible,
      useDefaultValue,
      useIsReadonly,
      useIsDisabled,
      useCustomStyle,
      useLayoutStyle,
      useAllowedOptions,
      useDynamicDisplay,
      useValidation,
      renderer,
      schemaInterface,
    ],
  );
  (Component as any).displayName = "RenderControl";
  return Component;
}
export function lookupSchemaField(
  c: ControlDefinition,
  fields: SchemaField[],
): SchemaField | undefined {
  const fieldName = isGroupControlsDefinition(c)
    ? c.compoundField
    : isDataControlDefinition(c)
      ? c.field
      : undefined;
  return fieldName ? findField(fields, fieldName) : undefined;
}
export function getControlData(
  schemaField: SchemaField | undefined,
  parentContext: ControlDataContext,
): [Control<any> | undefined, Control<any> | undefined, ControlDataContext] {
  const { data, path } = parentContext;
  const parentControl = data.lookupControl(path);
  const childPath = schemaField ? [...path, schemaField.field] : path;
  const childControl =
    schemaField && parentControl
      ? parentControl.fields?.[schemaField.field]
      : undefined;
  return [
    parentControl,
    childControl,
    schemaField
      ? {
          ...parentContext,
          path: childPath,
          fields: isCompoundField(schemaField)
            ? schemaField.children
            : parentContext.fields,
        }
      : parentContext,
  ];
}

function groupProps(
  renderOptions: GroupRenderOptions = { type: "Standard" },
  childCount: number,
  renderChild: ChildRenderer,
  data: DataContext,
  className: string | null | undefined,
  style: React.CSSProperties | undefined,
): GroupRendererProps {
  return {
    childCount,
    renderChild: (i) =>
      renderChild(i, i, { control: data.data, parentPath: data.path }),
    renderOptions,
    className: cc(className),
    style,
  };
}

export function defaultDataProps({
  definition,
  field,
  control,
  options,
  elementRenderer,
  style,
  allowedOptions,
  ...props
}: DataControlProps): DataRendererProps {
  const className = cc(definition.styleClass);
  const required = !!definition.required;
  const fieldOptions =
    (field.options?.length ?? 0) === 0 ? null : field.options;
  const allowed = allowedOptions?.value ?? [];
  return {
    definition,
    control,
    field,
    id: "c" + control.uniqueId,
    options:
      fieldOptions && allowed.length > 0
        ? fieldOptions.filter((x) => allowed.includes(x.value))
        : fieldOptions,
    readonly: !!options.readonly,
    renderOptions: definition.renderOptions ?? { type: "Standard" },
    required,
    hidden: !!options.hidden,
    className,
    style,
    ...props,
    toArrayProps: elementRenderer
      ? () =>
          defaultArrayProps(
            control,
            field,
            required,
            style,
            className,
            elementRenderer,
          )
      : undefined,
  };
}

export function defaultArrayProps(
  arrayControl: Control<any[] | undefined | null>,
  field: SchemaField,
  required: boolean,
  style: CSSProperties | undefined,
  className: string | undefined,
  renderElement: (elemIndex: number) => ReactNode,
): ArrayRendererProps {
  const noun = field.displayName ?? field.field;
  const elems = arrayControl.elements ?? [];
  return {
    arrayControl,
    elementCount: elems.length,
    required,
    addAction: {
      actionId: "add",
      actionText: "Add " + noun,
      onClick: () => addElement(arrayControl, elementValueForField(field)),
    },
    elementKey: (i) => elems[i].uniqueId,
    removeAction: (i: number) => ({
      actionId: "",
      actionText: "Remove",
      onClick: () => removeElement(arrayControl, i),
    }),
    renderElement: (i) => renderElement(i),
    className: cc(className),
    style,
  };
}

export type ChildRenderer = (
  k: Key,
  childIndex: number,
  props: ControlRenderProps,
) => ReactNode;

export interface RenderControlProps {
  definition: ControlDefinition;
  renderer: FormRenderer;
  childCount: number;
  renderChild: ChildRenderer;
  createDataProps: CreateDataProps;
  formOptions: FormContextOptions;
  dataContext: ControlDataContext;
  control?: Control<any>;
  schemaField?: SchemaField;
  displayControl?: Control<string | undefined>;
  style?: React.CSSProperties;
  allowedOptions?: Control<any[] | undefined>;
}
export function renderControlLayout({
  definition: c,
  renderer,
  childCount,
  renderChild: childRenderer,
  control: childControl,
  schemaField,
  dataContext,
  formOptions: dataOptions,
  createDataProps: dataProps,
  displayControl,
  style,
  allowedOptions,
}: RenderControlProps): ControlLayoutProps {
  if (isDataControlDefinition(c)) {
    return renderData(c);
  }
  if (isGroupControlsDefinition(c)) {
    if (c.compoundField) {
      return renderData(
        dataControl(c.compoundField, c.title, {
          children: c.children,
          hideTitle: c.groupOptions?.hideTitle,
        }),
      );
    }
    return {
      processLayout: renderer.renderGroup(
        groupProps(
          c.groupOptions,
          childCount,
          childRenderer,
          dataContext,
          c.styleClass,
          style,
        ),
      ),
      label: {
        label: c.title,
        type: LabelType.Group,
        hide: c.groupOptions?.hideTitle,
      },
    };
  }
  if (isActionControlsDefinition(c)) {
    return {
      children: renderer.renderAction({
        actionText: c.title ?? c.actionId,
        actionId: c.actionId,
        onClick: () => {},
        className: cc(c.styleClass),
        style,
      }),
    };
  }
  if (isDisplayControlsDefinition(c)) {
    return {
      children: renderer.renderDisplay({
        data: c.displayData ?? {},
        className: cc(c.styleClass),
        style,
        display: displayControl,
      }),
    };
  }
  return {};

  function renderData(c: DataControlDefinition, elemIndex?: number) {
    if (!schemaField) return { children: "No schema field for: " + c.field };
    if (!childControl) return { children: "No control for: " + c.field };
    const props = dataProps({
      definition: c,
      field: schemaField,
      dataContext:
        elemIndex != null
          ? { ...dataContext, path: [...dataContext.path, elemIndex] }
          : dataContext,
      control:
        elemIndex != null ? childControl!.elements[elemIndex] : childControl,
      options: dataOptions,
      style,
      childCount,
      allowedOptions,
      renderChild: childRenderer,
      elementRenderer:
        elemIndex == null && schemaField.collection
          ? (ei) => renderLayoutParts(renderData(c, ei), renderer).children
          : undefined,
    });

    const labelText = !c.hideTitle
      ? controlTitle(c.title, schemaField)
      : undefined;
    return {
      processLayout: renderer.renderData(props),
      label: {
        type: LabelType.Control,
        label: labelText,
        forId: props.id,
        required: c.required,
        hide: c.hideTitle,
      },
      errorControl: childControl,
    };
  }
}

export function appendMarkup(
  k: keyof Omit<RenderedLayout, "errorControl" | "style" | "className">,
  markup: ReactNode,
): (layout: RenderedLayout) => void {
  return (layout) =>
    (layout[k] = (
      <>
        {layout[k]}
        {markup}
      </>
    ));
}

export function wrapMarkup(
  k: keyof Omit<RenderedLayout, "errorControl" | "style" | "className">,
  wrap: (ex: ReactNode) => ReactNode,
): (layout: RenderedLayout) => void {
  return (layout) => (layout[k] = wrap(layout[k]));
}

export function layoutKeyForPlacement(
  pos: AdornmentPlacement,
): keyof Omit<RenderedLayout, "errorControl" | "style" | "className"> {
  switch (pos) {
    case AdornmentPlacement.ControlEnd:
      return "controlEnd";
    case AdornmentPlacement.ControlStart:
      return "controlStart";
    case AdornmentPlacement.LabelStart:
      return "labelStart";
    case AdornmentPlacement.LabelEnd:
      return "labelEnd";
  }
}

export function appendMarkupAt(
  pos: AdornmentPlacement,
  markup: ReactNode,
): (layout: RenderedLayout) => void {
  return appendMarkup(layoutKeyForPlacement(pos), markup);
}

export function wrapMarkupAt(
  pos: AdornmentPlacement,
  wrap: (ex: ReactNode) => ReactNode,
): (layout: RenderedLayout) => void {
  return wrapMarkup(layoutKeyForPlacement(pos), wrap);
}

export function renderLayoutParts(
  props: ControlLayoutProps,
  renderer: FormRenderer,
): RenderedLayout {
  const { className, children, style, errorControl, label, adornments } =
    props.processLayout?.(props) ?? props;
  const layout: RenderedLayout = {
    children,
    errorControl,
    style,
    className: cc(className),
  };
  (adornments ?? [])
    .sort((a, b) => a.priority - b.priority)
    .forEach((x) => x.apply(layout));
  layout.label =
    label && !label.hide
      ? renderer.renderLabel(label, layout.labelStart, layout.labelEnd)
      : undefined;
  return layout;
}

export function controlTitle(
  title: string | undefined | null,
  field: SchemaField,
) {
  return title ? title : fieldDisplayName(field);
}

function lookupControl(
  base: Control<any> | undefined,
  path: (string | number)[],
): Control<any> | undefined {
  let index = 0;
  while (index < path.length && base) {
    const childId = path[index];
    const c = base.current;
    if (typeof childId === "string") {
      const next = c.fields?.[childId];
      if (!next) trackControlChange(base, ControlChange.Structure);
      base = next;
    } else {
      base = c.elements?.[childId];
    }
    index++;
  }
  return base;
}