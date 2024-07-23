export type Path = [] | [number | string, Path];

export type EvalExpr = string | number | null | Path | ArrayExpr | CallExpr;
export interface ArrayExpr {
  type: "array";
  values: EvalExpr[];
}

export interface CallExpr {
  type: "call";
  function: string;
  args: EvalExpr[];
}

export interface ValueExpr {
  type: "value";
  value?: any;
}
