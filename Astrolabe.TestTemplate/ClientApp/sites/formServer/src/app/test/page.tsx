"use client";
import {
  DatePicker,
  DateTimePicker,
  TimeField,
} from "@astroapps/aria-datepicker";
import { useControl } from "@react-typed-forms/core";
import {
  now,
  getLocalTimeZone,
  ZonedDateTime,
  CalendarDateTime,
} from "@internationalized/date";
import { useState, useEffect } from "react";
export default function Page() {
  const date = useControl<ZonedDateTime>(now(getLocalTimeZone()));
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);
  return isClient ? (
    <div className="container">
      <h1>Page</h1>
      <DatePicker
        shouldForceLeadingZeros={true}
        value={date.value}
        onChange={(v) => (date.value = v)}
        label="Time"
        buttonClass="px-2"
        calenderClasses={{
          cellClass: "md:size-10 text-center",
          dayClass:
            "aspect-square size-8 grid place-items-center hover:bg-primary-400 hover:text-white rounded-full",
          className:
            "p-6 pt-2 rounded-b shadow-lg border border-t-0 border-surface-300",
        }}
        time={{
          shouldForceLeadingZeros: true,
          fieldClass: "px-2 pl-8 flex border border-surface-300 rounded-t",
        }}
      />
    </div>
  ) : (
    <>TEST</>
  );
}
