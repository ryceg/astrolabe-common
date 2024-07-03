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
import React from "react";
import clsx from "clsx";

export interface TimeFieldClasses {
  containerProps?: React.HTMLAttributes<HTMLDivElement>;
  labelClass?: string;
  fieldClass?: string;
  invalidClass?: string;
  segmentClass?: string;
}

export const DefaultTimeFieldClasses: TimeFieldClasses = {
  fieldClass: "flex gap-1",
};

export type TimeFieldProps = AriaTimeFieldProps<TimeValue> & TimeFieldClasses;
export function TimeField(props: TimeFieldProps) {
  let { locale } = useLocale();
  let state = useTimeFieldState({
    ...props,

    locale,
  });

  const { containerProps, labelClass, fieldClass, invalidClass, segmentClass } =
    {
      ...DefaultTimeFieldClasses,
      ...props,
    };

  let ref = React.useRef(null);
  let { labelProps, fieldProps } = useTimeField(props, state, ref);

  return (
    <div {...containerProps}>
      <span {...labelProps} className={clsx(labelClass)}>
        {props.label}
      </span>
      <div {...fieldProps} ref={ref} className={clsx("field", fieldClass)}>
        {state.segments.map((segment, i) => (
          <DateSegment
            segmentClass={segmentClass}
            key={i}
            segment={segment}
            state={state}
          />
        ))}
        {state.isInvalid && (
          <span className={invalidClass} aria-hidden="true">
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
  segmentClass,
}: {
  segment: DateSegment;
  state: TimeFieldState;
  segmentClass?: string;
}) {
  let ref = React.useRef(null);
  let { segmentProps } = useDateSegment(segment, state, ref);

  return (
    <span
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
        segmentClass,
      )}
    >
      {segment.isPlaceholder ? <>{segment.placeholder}</> : <>{segment.text}</>}
    </span>
  );
}
