import {
  CalendarProps as AriaCalendarProps,
  useCalendar,
  useLocale,
} from "react-aria";
import { useCalendarState } from "react-stately";
import { createCalendar, DateValue } from "@internationalized/date";
import React from "react";

// Reuse the Button from your component library. See below for details.
import { Button } from "@astroapps/aria-base";
import {
  CalendarGrid,
  CalendarGridClasses,
  DefaultCalendarGridClasses,
} from "./CalendarGrid";

export interface CalendarClasses extends CalendarGridClasses {
  className?: string;
  headerClass?: string;
  titleClass?: string;
  navButtonClass?: string;
  navButtonContainerClass?: string;
}
export interface CalendarProps<T extends DateValue = DateValue>
  extends AriaCalendarProps<T>,
    CalendarClasses {
  prevButton?: React.ReactNode;
  nextButton?: React.ReactNode;
}

export const DefaultCalendarClasses = {
  className: "border border-black",
  titleClass: "text-xl ml-2 font-bold",
  headerClass: "flex items-center justify-between pb-4",
  navButtonClass: "w-8 h-8",
  navButtonContainerClass: "flex gap-2",
  ...DefaultCalendarGridClasses,
} satisfies CalendarClasses;

export function Calendar<T extends DateValue = DateValue>(
  props: CalendarProps<T>,
) {
  const {
    className,
    headerClass,
    navButtonClass,
    titleClass,
    navButtonContainerClass,
    ...otherProps
  } = {
    ...DefaultCalendarClasses,
    ...props,
  };
  let { locale } = useLocale();
  let state = useCalendarState({
    ...props,
    locale,
    createCalendar,
  });
  const prevButton = otherProps.prevButton;
  const nextButton = otherProps.nextButton;
  let { calendarProps, prevButtonProps, nextButtonProps, title } = useCalendar(
    props,
    state,
  );

  return (
    <div {...calendarProps} className={className}>
      <div className={headerClass}>
        <h2 className={titleClass}>{title}</h2>
        <div className={navButtonContainerClass}>
          <Button
            aria-label="Previous Month"
            {...prevButtonProps}
            className={navButtonClass}
          >
            {prevButton ?? <i aria-hidden className="fa fa-arrow-left" />}
          </Button>
          <Button
            aria-label="Next Month"
            {...nextButtonProps}
            className={navButtonClass}
          >
            {nextButton ?? <i aria-hidden className="fa fa-arrow-right" />}
          </Button>
        </div>
      </div>
      <CalendarGrid {...otherProps} state={state} />
    </div>
  );
}
