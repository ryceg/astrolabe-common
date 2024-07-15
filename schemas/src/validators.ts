import {
  ControlDefinition,
  DataControlDefinition,
  DateComparison,
  DateValidator,
  isDataControlDefinition,
  JsonataValidator,
  LengthValidator,
  SchemaField,
  SchemaInterface,
  ValidatorType,
} from "./types";
import {
  Control,
  ControlChange,
  trackControlChange,
  useControlEffect,
  useValidator,
  useValueChangeEffect,
} from "@react-typed-forms/core";
import { useCallback } from "react";
import {
  ControlDataContext,
  DynamicHookGenerator,
  makeHook,
  useUpdatedRef,
} from "./util";
import { useJsonataExpression } from "./hooks";

export interface ValidationContext {
  control: Control<any>;
  hidden: Control<boolean>;
  dataContext: ControlDataContext;
}

export function useDefaultValidation(
  ctx: ValidationContext,
  state: { definition },
);

export function useControlValidation(
  definition: ControlDefinition,
  field: SchemaField | undefined,
): DynamicHookGenerator<void, ValidationContext> {
  const dd = isDataControlDefinition(definition) ? definition : undefined;
  return makeHook(
    doValidation,
    { definition, field },
    dd?.validators?.map((x) => x.type).join("_") ?? undefined,
  );
  function doValidation() {}
  //   const dd = isDataControlDefinition(definition)
  //     ? { required: definition.required, validators: definition.validators }
  //     : {};
  //   const validatorTypes = dd ? dd.validators?.map((x) => x.type) ?? [] : null;
  //   if (!validatorTypes) return;
  //
  //   makeHook(())
  //   const schemaInterface = dataContext.schemaInterface;
  //
  //   useValueChangeEffect(control, () => control.setError("default", ""));
  //   useValidator(
  //     control,
  //     (v) =>
  //       !hidden && dd.required && field && schemaInterface.isEmptyValue(field, v)
  //         ? "Please enter a value"
  //         : null,
  //     "required",
  //     undefined,
  //     [hidden, dd.required, !!field],
  //   );
  //   (dd.validators ?? []).forEach((x, i) => {
  //     switch (x.type) {
  //       case ValidatorType.Length:
  //         const lv = x as LengthValidator;
  //         useControlEffect(
  //           () => {
  //             trackControlChange(control, ControlChange.Validate);
  //             return [
  //               field ? schemaInterface.controlLength(field, control) : 0,
  //               hidden,
  //             ] as const;
  //           },
  //           ([len, hidden]) => {
  //             if (hidden) {
  //               control.setError("length", undefined);
  //               return;
  //             }
  //             if (lv.min != null && len < lv.min) {
  //               if (field?.collection) {
  //                 control.setValue((v) =>
  //                   Array.isArray(v)
  //                     ? v.concat(Array.from({ length: lv.min! - v.length }))
  //                     : Array.from({ length: lv.min! }),
  //                 );
  //               } else {
  //                 control.setError("length", "Length must be at least " + lv.min);
  //               }
  //             } else if (lv.max != null && len > lv.max) {
  //               control.setError("length", "Length must be less than " + lv.max);
  //             }
  //           },
  //           true,
  //         );
  //         break;
  //       case ValidatorType.Jsonata:
  //         return useJsonataValidator(
  //           control,
  //           groupContext,
  //           x as JsonataValidator,
  //           hidden,
  //           i,
  //         );
  //       case ValidatorType.Date:
  //         return useDateValidator(control, x as DateValidator, i);
  //     }
  //   });
  // }
  //
  // function useJsonataValidator(
  //   control: Control<any>,
  //   context: ControlDataContext,
  //   expr: JsonataValidator,
  //   hidden: boolean,
  //   i: number,
  // ) {
  //   const errorMsg = useJsonataExpression(expr.expression, context);
  //   useValidator(control, () => (!hidden ? errorMsg.value : null), "jsonata" + i);
  // }
  //
  // function useDateValidator(
  //   control: Control<string | null | undefined>,
  //   dv: DateValidator,
  //   i: number,
  // ) {
  //   let comparisonDate: number;
  //   if (dv.fixedDate) {
  //     comparisonDate = Date.parse(dv.fixedDate);
  //   } else {
  //     const nowDate = new Date();
  //     comparisonDate = Date.UTC(
  //       nowDate.getFullYear(),
  //       nowDate.getMonth(),
  //       nowDate.getDate(),
  //     );
  //     if (dv.daysFromCurrent) {
  //       comparisonDate += dv.daysFromCurrent * 86400000;
  //     }
  //   }
  //   useValidator(
  //     control,
  //     (v) => {
  //       if (v) {
  //         const selDate = Date.parse(v);
  //         const notAfter = dv.comparison === DateComparison.NotAfter;
  //         if (notAfter ? selDate > comparisonDate : selDate < comparisonDate) {
  //           return `Date must not be ${notAfter ? "after" : "before"} ${new Date(
  //             comparisonDate,
  //           ).toDateString()}`;
  //         }
  //       }
  //       return null;
  //     },
  //     "date" + i,
  //   );
}
