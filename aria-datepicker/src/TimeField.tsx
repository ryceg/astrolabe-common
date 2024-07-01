"use client";

import {
  type DateSegment,
  TimeFieldState,
  useTimeFieldState,
} from "react-stately";
import {
  useDateSegment,
  useLocale,
  useTimeField,
  AriaTimeFieldProps,
  TimeValue,
} from "react-aria";
import {
  createCalendar,
  parseZonedDateTime,
  Time,
} from "@internationalized/date";
import React, { useRef } from "react";
import clsx from "clsx";

export function TimeField(
  props: AriaTimeFieldProps<TimeValue> & {
    containerProps?: React.HTMLAttributes<HTMLDivElement>;
    labelClasses?: string;
    fieldClasses?: string;
    invalidClasses?: string;
    segmentClasses?: string;
  },
) {
  let { locale } = useLocale();
  let state = useTimeFieldState({
    ...props,

    locale,
  });

  let ref = React.useRef(null);
  let { labelProps, fieldProps } = useTimeField(props, state, ref);

  return (
    <div {...props.containerProps}>
      <span {...labelProps} className={clsx(props.labelClasses)}>
        {props.label}
      </span>
      <div
        {...fieldProps}
        ref={ref}
        className={clsx("field", props.fieldClasses)}
      >
        {state.segments.map((segment, i) => (
          <DateSegment
            segmentClasses={props.segmentClasses}
            key={i}
            segment={segment}
            state={state}
          />
        ))}
        {state.isInvalid && (
          <span className={props.invalidClasses} aria-hidden="true">
            🚫
          </span>
        )}
      </div>
    </div>
  );
}

function DateSegment({
  segment,
  state,
  segmentClasses,
}: {
  segment: DateSegment;
  state: TimeFieldState;
  segmentClasses?: string;
}) {
  let ref = React.useRef(null);
  let { segmentProps } = useDateSegment(segment, state, ref);

  return (
    <div
      {...segmentProps}
      style={{
        ...segmentProps.style,
        minWidth: segment.maxValue && String(segment.maxValue).length + "ch",
      }}
      ref={ref}
      aria-hidden={segment.isPlaceholder}
      className={clsx(
        `segment`,
        segment.isPlaceholder && "placeholder",
        segment.isPlaceholder && "text-center italic",

        // segment.isPlaceholder && "invisible pointer-events-none",
        segmentClasses,
      )}
    >
      {segment.isPlaceholder ? <>{segment.placeholder}</> : <>{segment.text}</>}
    </div>
  );
}
