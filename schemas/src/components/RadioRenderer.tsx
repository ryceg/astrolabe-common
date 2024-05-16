import {
  Control,
  formControlProps,
  RenderArrayElements,
} from "@react-typed-forms/core";
import React from "react";
import { createDataRenderer } from "../renderers";
import { DataRenderType, FieldOption } from "../types";
import { rendererClass } from "../util";

export interface RadioRendererOptions {
  className?: string;
  entryClass?: string;
  radioClass?: string;
  labelClass?: string;
}

export function createRadioRenderer(options: RadioRendererOptions = {}) {
  return createDataRenderer(
    (p) => (
      <RadioButtons
        {...options}
        {...p}
        className={rendererClass(p.className, options.className)}
        control={p.control}
      />
    ),
    {
      renderType: DataRenderType.Radio,
    },
  );
}

export function RadioButtons({
  control,
  options,
  labelClass,
  radioClass,
  readonly,
  entryClass,
  className,
  id,
}: {
  id?: string;
  className?: string;
  options?: FieldOption[] | null;
  control: Control<any>;
  entryClass?: string;
  radioClass?: string;
  labelClass?: string;
  readonly?: boolean;
}) {
  const { disabled } = control;
  const canChange = !disabled && !readonly;
  const name = "r" + control.uniqueId;
  return (
    <div className={className} id={id}>
      <RenderArrayElements array={options?.filter((x) => x.value != null)}>
        {(o, i) => (
          <div key={i} className={entryClass}>
            <input
              id={name + "_" + i}
              className={radioClass}
              type="radio"
              name={name}
              readOnly={readonly}
              disabled={disabled}
              checked={control.value == o.value}
              onChange={(x) => (control.value = o.value)}
            />
            <label className={labelClass} htmlFor={name + "_" + i}>
              {o.name}
            </label>
          </div>
        )}
      </RenderArrayElements>
    </div>
  );
}
