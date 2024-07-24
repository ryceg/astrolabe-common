"use client";
import {
  basicEnv,
  evaluate,
  flatmapEnv,
  mapEnv,
  parseEval,
  parser,
  resolve,
} from "@astroapps/evaluator";
import { useControl, useControlEffect } from "@react-typed-forms/core";
import React from "react";

export default function EvalPage() {
  const input = useControl("");
  const output = useControl<any>();
  useControlEffect(
    () => input.value,
    (v) => {
      try {
        const exprTree = parseEval(v);
        let result;
        try {
          result = flatmapEnv(
            resolve(
              basicEnv({
                simple: { child: 12 },
                hai: [{ cool: { again: "yo" } }, { cool: { again: "yep" } }],
              }),
              exprTree,
            ),
            evaluate,
          )[1];
        } catch (e) {
          console.error(e);
          result = e?.toString();
        }
        output.value = {
          exprTree,
          result,
        };
      } catch (e) {
        console.error(e);
      }
    },
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
