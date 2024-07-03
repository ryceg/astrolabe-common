import React, { useState } from "react";
import { TimeValue, useDatePicker } from "react-aria";
import {
  DatePickerStateOptions,
  useDatePickerState,
  useTimeFieldState,
} from "react-stately";
import { DateField } from "./DateField";
import { Calendar, CalendarClasses } from "./Calendar";
import {
  CalendarDateTime,
  DateValue,
  getLocalTimeZone,
  now,
  parseAbsoluteToLocal,
  parseZonedDateTime,
  toCalendarDateTime,
  today,
  ZonedDateTime,
} from "@internationalized/date";
import { Button, Dialog, Popover } from "@astroapps/aria-base";
import { DialogClasses, PopoverClasses } from "@astroapps/aria-base";
import { DatePickerProps, DefaultDatePickerClasses } from "./DatePicker";
import { TimeField, TimeFieldProps } from "./TimeField";

type DateValueWithTime = CalendarDateTime | ZonedDateTime | DateValue;
export function DateTimePicker<T extends DateValueWithTime = CalendarDateTime>(
  props: DatePickerProps<T> & {
    time: TimeFieldProps;
  },
) {
  const {
    isReadOnly,
    buttonClass,
    calenderClasses,
    popoverClasses,
    dialogClasses,
    iconClass,
  } = {
    ...DefaultDatePickerClasses,
    ...props,
  };
  let state = useDatePickerState({
    ...props,
  });
  let ref = React.useRef(null);
  let { groupProps, fieldProps, buttonProps, dialogProps, calendarProps } =
    useDatePicker<T>(props, state, ref);

  return (
    <div style={{ display: "inline-flex", flexDirection: "column" }}>
      <div {...groupProps} ref={ref} className={props.className}>
        <DateField {...fieldProps} />
        {!isReadOnly && (
          <Button {...buttonProps} className={buttonClass}>
            <i aria-hidden className={iconClass} />
          </Button>
        )}
      </div>
      {state.isOpen && (
        <Popover
          state={state}
          triggerRef={ref}
          placement="bottom start"
          {...popoverClasses}
        >
          <Dialog {...dialogProps} {...dialogClasses}>
            <TimeField {...props.time} />
            <Calendar {...calendarProps} {...calenderClasses} />
          </Dialog>
        </Popover>
      )}
    </div>
  );
}
