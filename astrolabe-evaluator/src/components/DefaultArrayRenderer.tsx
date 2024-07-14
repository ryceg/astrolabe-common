import { ArrayRendererRegistration } from "../renderers";
import {
  ActionRendererProps,
  applyArrayLengthRestrictions,
  ArrayRendererProps,
} from "../controlRender";
import clsx from "clsx";
import React, { Fragment, ReactNode } from "react";
import { RenderElements } from "@react-typed-forms/core";
import { applyLengthRestrictions } from "../util";

export interface DefaultArrayRendererOptions {
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
  return {
    render: (props, { renderAction }) => (
      <DefaultArrayRenderer
        {...props}
        {...options}
        renderAction={renderAction}
      />
    ),
    type: "array",
  };
}

export interface DefaultArrayRendererProps
  extends DefaultArrayRendererOptions,
    ArrayRendererProps {
  renderAction: (props: ActionRendererProps) => ReactNode;
}

export function DefaultArrayRenderer(props: DefaultArrayRendererProps) {
  const {
    renderElement,
    className,
    removableClass,
    childClass,
    removableChildClass,
    removeActionClass,
    addActionClass,
    arrayControl,
    renderAction,
    style,
  } = props;
  const { addAction, removeAction } = applyArrayLengthRestrictions(props);
  return (
    <div style={style}>
      <div className={clsx(className, removeAction && removableClass)}>
        <RenderElements control={arrayControl}>
          {(_, x) =>
            removeAction ? (
              <>
                <div className={clsx(childClass, removableChildClass)}>
                  {renderElement(x)}
                </div>
                <div className={removeActionClass}>
                  {renderAction(removeAction(x))}
                </div>
              </>
            ) : (
              <div className={childClass}>{renderElement(x)}</div>
            )
          }
        </RenderElements>
      </div>
      {addAction && (
        <div className={addActionClass}>{renderAction(addAction)}</div>
      )}
    </div>
  );
}
