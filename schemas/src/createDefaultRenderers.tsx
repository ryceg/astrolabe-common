import {
  createDefaultDisplayRenderer,
  DefaultDisplayRendererOptions,
} from "./components/DefaultDisplay";
import {
  DefaultLayout,
  DefaultLayoutRendererOptions,
} from "./components/DefaultLayout";
import {
  ActionRendererRegistration,
  AdornmentRendererRegistration,
  ArrayRendererRegistration,
  createDataRenderer,
  createLayoutRenderer,
  DataRendererRegistration,
  DefaultRenderers,
  GroupRendererRegistration,
  isIconAdornment,
  LabelRendererRegistration,
} from "./renderers";
import { createDefaultVisibilityRenderer } from "./components/DefaultVisibility";
import React, { CSSProperties, Fragment, ReactElement, ReactNode } from "react";
import { hasOptions, rendererClass } from "./util";
import clsx from "clsx";
import {
  ActionRendererProps,
  appendMarkupAt,
  ArrayRendererProps,
  ControlLayoutProps,
  FormRenderer,
  GroupRendererProps,
  LabelType,
  renderLayoutParts,
} from "./controlRender";
import {
  AdornmentPlacement,
  DataRenderType,
  FieldOption,
  FieldType,
  FlexRenderer,
  GridRenderer,
  isDataGroupRenderer,
  isDisplayOnlyRenderer,
  isFlexRenderer,
  isGridRenderer,
  isTextfieldRenderer,
} from "./types";
import {
  createSelectRenderer,
  SelectRendererOptions,
} from "./components/SelectDataRenderer";
import { DefaultDisplayOnly } from "./components/DefaultDisplayOnly";
import { Fcheckbox } from "@react-typed-forms/core";
import { ControlInput, createInputConversion } from "./components/ControlInput";
import {
  createRadioRenderer,
  RadioRendererOptions,
} from "./components/RadioRenderer";

export interface DefaultRendererOptions {
  data?: DefaultDataRendererOptions;
  display?: DefaultDisplayRendererOptions;
  action?: DefaultActionRendererOptions;
  array?: DefaultArrayRendererOptions;
  group?: DefaultGroupRendererOptions;
  label?: DefaultLabelRendererOptions;
  adornment?: DefaultAdornmentRendererOptions;
  layout?: DefaultLayoutRendererOptions;
}

interface StyleProps {
  className?: string;
  style?: CSSProperties;
}

interface DefaultActionRendererOptions {
  className?: string;
}

export function createDefaultActionRenderer(
  options: DefaultActionRendererOptions = {},
): ActionRendererRegistration {
  function render({ onClick, actionText }: ActionRendererProps) {
    return (
      <button className={options.className} onClick={onClick}>
        {actionText}
      </button>
    );
  }

  return { render, type: "action" };
}

interface DefaultArrayRendererOptions {
  className?: string;
  removableClass?: string;
  childClass?: string;
  removableChildClass?: string;
  removeActionClass?: string;
  addActionClass?: string;
}

export function createDefaultArrayRenderer(
  options?: DefaultArrayRendererOptions,
): ArrayRendererRegistration {
  const {
    className,
    removableClass,
    childClass,
    removableChildClass,
    removeActionClass,
    addActionClass,
  } = options ?? {};

  function render(
    {
      elementCount,
      renderElement,
      addAction,
      removeAction,
      elementKey,
      required,
    }: ArrayRendererProps,
    { renderAction }: FormRenderer,
  ) {
    const showRemove = !required || elementCount > 1;
    return (
      <div>
        <div className={clsx(className, removeAction && removableClass)}>
          {Array.from({ length: elementCount }, (_, x) =>
            removeAction ? (
              <Fragment key={elementKey(x)}>
                <div className={clsx(childClass, removableChildClass)}>
                  {renderElement(x)}
                </div>
                <div className={removeActionClass}>
                  {showRemove && renderAction(removeAction(x))}
                </div>
              </Fragment>
            ) : (
              <div key={elementKey(x)} className={childClass}>
                {renderElement(x)}
              </div>
            ),
          )}
        </div>
        {addAction && (
          <div className={addActionClass}>{renderAction(addAction)}</div>
        )}
      </div>
    );
  }

  return { render, type: "array" };
}

interface DefaultGroupRendererOptions {
  className?: string;
  standardClassName?: string;
  gridStyles?: (columns: GridRenderer) => StyleProps;
  gridClassName?: string;
  defaultGridColumns?: number;
  flexClassName?: string;
  defaultFlexGap?: string;
}

export function createDefaultGroupRenderer(
  options?: DefaultGroupRendererOptions,
): GroupRendererRegistration {
  const {
    className,
    gridStyles = defaultGridStyles,
    defaultGridColumns = 2,
    gridClassName,
    standardClassName,
    flexClassName,
    defaultFlexGap,
  } = options ?? {};

  function defaultGridStyles({
    columns = defaultGridColumns,
  }: GridRenderer): StyleProps {
    return {
      className: gridClassName,
      style: {
        display: "grid",
        gridTemplateColumns: `repeat(${columns}, 1fr)`,
      },
    };
  }

  function flexStyles(options: FlexRenderer): StyleProps {
    return {
      className: flexClassName,
      style: {
        display: "flex",
        gap: options.gap ? options.gap : defaultFlexGap,
        flexDirection: options.direction
          ? (options.direction as any)
          : undefined,
      },
    };
  }

  function render(props: GroupRendererProps) {
    const { renderChild, renderOptions, childDefinitions } = props;

    const { style, className: gcn } = isGridRenderer(renderOptions)
      ? gridStyles(renderOptions)
      : isFlexRenderer(renderOptions)
        ? flexStyles(renderOptions)
        : ({ className: standardClassName } as StyleProps);

    return (cp: ControlLayoutProps) => {
      return {
        ...cp,
        children: (
          <div
            className={rendererClass(props.className, clsx(className, gcn))}
            style={style}
          >
            {childDefinitions?.map((c, i) => renderChild(i, i))}
          </div>
        ),
      };
    };
  }

  return { type: "group", render };
}

export const DefaultBoolOptions: FieldOption[] = [
  { name: "Yes", value: true },
  { name: "No", value: false },
];

interface DefaultDataRendererOptions {
  inputClass?: string;
  displayOnlyClass?: string;
  selectOptions?: SelectRendererOptions;
  radioOptions?: RadioRendererOptions;
  booleanOptions?: FieldOption[];
  optionRenderer?: DataRendererRegistration;
}

export function createDefaultDataRenderer(
  options: DefaultDataRendererOptions = {},
): DataRendererRegistration {
  const selectRenderer = createSelectRenderer(options.selectOptions);
  const radioRenderer = createRadioRenderer(options.radioOptions);
  const { inputClass, booleanOptions, optionRenderer, displayOnlyClass } = {
    optionRenderer: selectRenderer,
    booleanOptions: DefaultBoolOptions,
    ...options,
  };
  return createDataRenderer((props, renderers) => {
    const fieldType = props.field.type;
    if (props.toArrayProps) {
      return (p) => ({
        ...p,
        children: renderers.renderArray(props.toArrayProps!()),
      });
    }
    const renderOptions = props.renderOptions;
    if (fieldType === FieldType.Compound) {
      const groupOptions = isDataGroupRenderer(renderOptions)
        ? renderOptions.groupOptions
        : undefined;
      const {
        style,
        className,
        childDefinitions,
        renderChild,
        dataContext,
        useChildVisibility,
      } = props;
      return renderers.renderGroup({
        style,
        className,
        childDefinitions,
        renderOptions: groupOptions ?? { type: "Standard", hideTitle: true },
        renderChild,
        dataContext,
        useChildVisibility,
      });
    }
    let renderType = renderOptions.type;
    if (fieldType == FieldType.Any) return <>No control for Any</>;
    if (isDisplayOnlyRenderer(renderOptions))
      return (p) => ({
        ...p,
        className: displayOnlyClass,
        children: (
          <DefaultDisplayOnly
            field={props.field}
            schemaInterface={props.dataContext.schemaInterface}
            control={props.control}
            className={props.className}
            style={props.style}
            emptyText={renderOptions.emptyText}
          />
        ),
      });
    const isBool = fieldType === FieldType.Bool;
    if (booleanOptions != null && isBool && props.options == null) {
      return renderers.renderData({ ...props, options: booleanOptions });
    }
    if (renderType === DataRenderType.Standard && hasOptions(props)) {
      return optionRenderer.render(props, renderers);
    }
    switch (renderType) {
      case DataRenderType.Dropdown:
        return selectRenderer.render(props, renderers);
      case DataRenderType.Radio:
        return radioRenderer.render(props, renderers);
      case DataRenderType.Checkbox:
        return (
          <Fcheckbox
            style={props.style}
            className={props.className}
            control={props.control}
          />
        );
    }
    const placeholder = isTextfieldRenderer(renderOptions)
      ? renderOptions.placeholder
      : undefined;
    return (
      <ControlInput
        className={rendererClass(props.className, inputClass)}
        style={props.style}
        id={props.id}
        readOnly={props.readonly}
        control={props.control}
        placeholder={placeholder ?? undefined}
        convert={createInputConversion(props.field.type)}
      />
    );
  });
}

export interface DefaultAdornmentRendererOptions {}

export function createDefaultAdornmentRenderer(
  options: DefaultAdornmentRendererOptions = {},
): AdornmentRendererRegistration {
  return {
    type: "adornment",
    render: ({ adornment }) => ({
      apply: (rl) => {
        if (isIconAdornment(adornment)) {
          return appendMarkupAt(
            adornment.placement ?? AdornmentPlacement.ControlStart,
            <i className={adornment.iconClass} />,
          )(rl);
        }
      },
      priority: 0,
      adornment,
    }),
  };
}

function createDefaultLayoutRenderer(
  options: DefaultLayoutRendererOptions = {},
) {
  return createLayoutRenderer((props, renderers) => {
    const layout = renderLayoutParts(
      {
        ...props,
        className: rendererClass(props.className, options.className),
      },
      renderers,
    );
    return {
      children: <DefaultLayout layout={layout} {...options} />,
      className: layout.className,
      style: layout.style,
      divRef: (e) =>
        e && props.errorControl
          ? (props.errorControl.meta.scrollElement = e)
          : undefined,
    };
  });
}

interface DefaultLabelRendererOptions {
  className?: string;
  groupLabelClass?: string;
  controlLabelClass?: string;
  requiredElement?: ReactNode;
  labelContainer?: (children: ReactElement) => ReactElement;
}

export function createDefaultLabelRenderer(
  options?: DefaultLabelRendererOptions,
): LabelRendererRegistration {
  const {
    className,
    groupLabelClass,
    controlLabelClass,
    requiredElement,
    labelContainer,
  } = {
    requiredElement: <span> *</span>,
    labelContainer: (c: ReactElement) => c,
    ...options,
  };
  return {
    render: (props, labelStart, labelEnd) => {
      return labelContainer(
        <>
          {labelStart}
          <label
            htmlFor={props.forId}
            className={rendererClass(
              props.className,
              clsx(
                className,
                props.type === LabelType.Group && groupLabelClass,
                props.type === LabelType.Control && controlLabelClass,
              ),
            )}
          >
            {props.label}
            {props.required && requiredElement}
          </label>
          {labelEnd}
        </>,
      );
    },
    type: "label",
  };
}

export function createDefaultRenderers(
  options: DefaultRendererOptions = {},
): DefaultRenderers {
  return {
    data: createDefaultDataRenderer(options.data),
    display: createDefaultDisplayRenderer(options.display),
    action: createDefaultActionRenderer(options.action),
    array: createDefaultArrayRenderer(options.array),
    group: createDefaultGroupRenderer(options.group),
    label: createDefaultLabelRenderer(options.label),
    adornment: createDefaultAdornmentRenderer(options.adornment),
    renderLayout: createDefaultLayoutRenderer(options.layout),
    visibility: createDefaultVisibilityRenderer(),
  };
}
