import { RenderedLayout } from "../controlRender";
import React from "react";

export interface DefaultLayoutRendererOptions {
  className?: string;
  errorClass?: string;
}

export function DefaultLayout({
  errorClass,
  layout: { controlEnd, controlStart, label, children, errorControl },
}: DefaultLayoutRendererOptions & {
  layout: RenderedLayout;
}) {
  const ec = errorControl;
  const errorText = ec && ec.touched ? ec.error : undefined;
  return (
    <>
      {label}
      {controlStart}
      {children}
      {errorText && <div className={errorClass}>{errorText}</div>}
      {controlEnd}
    </>
  );
}
