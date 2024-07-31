"use client";
import {
  basicEnv,
  evaluate,
  flatmapEnv,
  parseEval,
  resolve,
} from "@astroapps/evaluator";
import {
  Fcheckbox,
  useControl,
  useControlEffect,
  useDebounced,
} from "@react-typed-forms/core";
import React from "react";
import sample from "./sample.json";
import { useApiClient } from "@astroapps/client/hooks/useApiClient";
import { Client } from "../../client";

export default function EvalPage() {
  const client = useApiClient(Client);
  const serverMode = useControl(false);
  const input = useControl("");
  const data = useControl(sample);
  const dataText = useControl(() => JSON.stringify(sample, null, 2));
  const output = useControl<any>();
  useControlEffect(
    () => dataText.value,
    (v) => (data.value = JSON.parse(v)),
  );
  useControlEffect(
    () => [input.value, data.value, serverMode.value] as const,
    useDebounced(async ([v, dv, sm]: [string, any, any]) => {
      try {
        if (sm) {
          const result = await client.eval({ expression: v, data: dv });
          console.log(result);
          output.value = result;
        } else {
          const exprTree = parseEval(v);
          let result;
          try {
            result = flatmapEnv(
              resolve(basicEnv(sample), exprTree),
              evaluate,
            )[1];
          } catch (e) {
            console.error(e);
            result = e?.toString();
          }
          output.value = result;
        }
      } catch (e) {
        console.error(e);
      }
    }, 1000),
  );
  return (
    <div className="h-screen flex flex-col">
      <div>
        <Fcheckbox control={serverMode} /> Server Mode
      </div>
      <div className="flex grow">
        <textarea
          className="grow"
          value={input.value}
          onChange={(e) => (input.value = e.target.value)}
        />
        <textarea
          className="grow"
          value={dataText.value}
          onChange={(e) => (dataText.value = e.target.value)}
        />
        <textarea
          className="grow"
          value={JSON.stringify(output.value, null, 2)}
        />
      </div>
    </div>
  );
}
