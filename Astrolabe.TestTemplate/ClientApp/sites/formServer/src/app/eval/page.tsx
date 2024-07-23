"use client";
import { parser } from "@astroapps/evaluator";
import { useControl, useControlEffect } from "@react-typed-forms/core";
import React from "react";

export default function EvalPage() {
  const input = useControl("");
  const output = useControl<any>();
  useControlEffect(
    () => input.value,
    (v) => (output.value = parser.parse(v).topNode.firstChild),
  );
  return (
    <div className="flex h-screen">
      <textarea
        className="grow"
        value={input.value}
        onChange={(e) => (input.value = e.target.value)}
      />
      <textarea
        className="grow"
        value={JSON.stringify(output.value, null, 2)}
      />
    </div>
  );
}
