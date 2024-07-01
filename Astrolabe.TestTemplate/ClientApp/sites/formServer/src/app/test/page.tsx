"use client";
import { DateTimePicker, TimeField } from "@astroapps/aria-datepicker";
import { useControl } from "@react-typed-forms/core";
import { useState, useEffect } from "react";
export default function Page() {
  type Time = Parameters<typeof DateTimePicker>[0]["value"];
  const time = useControl<Time>(null);
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);
  return isClient ? (
    <div className="container">
      <h1>Page</h1>
      <TimeField
        hideTimeZone={true}
        shouldForceLeadingZeros={true}
        value={time.value}
        onChange={(v) => (time.value = v)}
        label="Time"
        fieldClasses="flex gap-1 border border-surface-300"
      />
      <DateTimePicker
        hideTimeZone={true}
        shouldForceLeadingZeros={true}
        value={time.value}
        onChange={(v) => (time.value = v)}
        label="Time"
        fieldClasses="flex gap-1 border border-surface-300"
      />
      {JSON.stringify(time.value)} !
    </div>
  ) : (
    <>TEST</>
  );
}
