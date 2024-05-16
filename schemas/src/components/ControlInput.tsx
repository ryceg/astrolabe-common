import { FieldType } from "../types";
import React from "react";
import { Control, formControlProps } from "@react-typed-forms/core";

export function ControlInput({
  control,
  convert,
  ...props
}: React.InputHTMLAttributes<HTMLInputElement> & {
  control: Control<any>;
  convert: InputConversion;
}) {
  const { errorText, value, onChange, ...inputProps } =
    formControlProps(control);
  return (
    <input
      {...inputProps}
      type={convert[0]}
      value={value == null ? "" : convert[2](value)}
      onChange={(e) => {
        control.value = convert[1](e.target.value);
      }}
      {...props}
    />
  );
}

type InputConversion = [string, (s: any) => any, (a: any) => string | number];

export function createInputConversion(ft: string): InputConversion {
  switch (ft) {
    case FieldType.String:
      return ["text", (a) => a, (a) => a];
    case FieldType.Bool:
      return ["text", (a) => a === "true", (a) => a?.toString() ?? ""];
    case FieldType.Int:
      return [
        "number",
        (a) => (a !== "" ? parseInt(a) : null),
        (a) => (a == null ? "" : a),
      ];
    case FieldType.Date:
      return ["date", (a) => a, (a) => a];
    case FieldType.Double:
      return ["number", (a) => parseFloat(a), (a) => a];
    default:
      return ["text", (a) => a, (a) => a];
  }
}
