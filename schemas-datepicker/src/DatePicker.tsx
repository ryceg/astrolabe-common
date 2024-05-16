import React, { useRef } from "react";
import { Control } from "@react-typed-forms/core";
import { useDatePicker } from "react-aria";
import { useDatePickerState } from "react-stately";
import { DateField } from "./DateParts/DateField";
import { Calendar } from "./DateParts/Calendar";
import { CalendarDate, parseDate } from "@internationalized/date";
import { AriaPopover } from "./DateParts/AriaPopover";
import { AriaDialog } from "./DateParts/AriaDialog";
import { AriaButton } from "./DateParts/AriaButton";

import clsx from "clsx";
export function DatePicker({
  control,
  readonly,
  className,
}: {
  control: Control<string>;
  className?: string;
  readonly?: boolean;
  id?: string;
}) {
  const disabled = control.disabled;
  let state = useDatePickerState({
    value: control.isNull ? null : parseDate(control.value),
    onChange: (c) => {
      control.touched = true;
      control.value = c?.toString();
    },
  });
  let ref = useRef<HTMLDivElement | null>(null);
  let {
    groupProps,
    labelProps,
    fieldProps,
    buttonProps,
    dialogProps,
    calendarProps,
  } = useDatePicker<CalendarDate>(
    {
      label: "HAI",
      isReadOnly: readonly,
      isDisabled: disabled,
    },
    state,
    ref,
  );
  return (
    <div className="relative">
      <div
        {...groupProps}
        ref={ref}
        className={clsx(className, disabled && "border-opacity-25 opacity-70")}
      >
        {!readonly && (
          <AriaButton {...buttonProps}>
            <i className="fa fa-calendar" />
          </AriaButton>
        )}
        <DateField {...fieldProps} />
      </div>
      {state.isOpen && (
        <AriaPopover state={state} triggerRef={ref} placement="bottom start">
          <AriaDialog {...dialogProps}>
            <Calendar {...calendarProps} />
          </AriaDialog>
        </AriaPopover>
      )}
    </div>
  );
}
