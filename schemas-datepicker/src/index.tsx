import {
  createDataRenderer,
  DataRenderType,
  FieldType,
} from "@react-typed-forms/schemas";
import { DatePicker } from "./DatePicker";
import React from "react";
import { rendererClass } from "@react-typed-forms/schemas/lib";

export { DatePicker };

export const DefaultDatePickerClass =
  "flex border w-full text-2xl pl-3 py-2 space-x-4";
export function createDatePickerRenderer(
  className: string = DefaultDatePickerClass,
) {
  return createDataRenderer(
    (p) => (
      <DatePicker
        dateTime={p.field.type == FieldType.DateTime}
        className={rendererClass(p.className, className)}
        control={p.control}
        readonly={p.readonly}
      />
    ),
    {
      schemaType: FieldType.Date,
      renderType: DataRenderType.DateTime,
    },
  );
}
