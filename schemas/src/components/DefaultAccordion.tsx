import React, { CSSProperties, Fragment, ReactElement, ReactNode } from "react";
import { AccordionAdornment } from "../types";
import { Control, useControl } from "@react-typed-forms/core";
import clsx from "clsx";

export function DefaultAccordion({
  children,
  accordion,
  contentStyle,
  contentClassName,
  designMode,
  togglerOpenClass,
  togglerClosedClass,
  className,
  renderTitle = (x) => <span>{x}</span>,
  renderToggler,
}: {
  children: ReactElement;
  accordion: Partial<AccordionAdornment>;
  contentStyle?: CSSProperties;
  contentClassName?: string;
  designMode?: boolean;
  className?: string;
  togglerOpenClass?: string;
  togglerClosedClass?: string;
  renderTitle?: (
    title: string | undefined,
    current: Control<boolean>,
  ) => ReactNode;
  renderToggler?: (current: Control<boolean>) => ReactNode;
}) {
  const open = useControl(!!accordion.defaultExpanded);
  const isOpen = open.value;
  const fullContentStyle =
    isOpen || designMode ? contentStyle : { ...contentStyle, display: "none" };
  const toggler = renderToggler ? (
    renderToggler(open)
  ) : (
    <button onClick={() => open.setValue((x) => !x)}>
      <i className={clsx(isOpen ? togglerOpenClass : togglerClosedClass)} />
    </button>
  );

  return (
    <>
      <div className={className}>
        {renderTitle(accordion.title, open)}
        {toggler}
      </div>
      <div style={fullContentStyle} className={contentClassName}>
        {children}
      </div>
    </>
  );
}
