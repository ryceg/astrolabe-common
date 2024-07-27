export interface EmptyPath {
  segment: null;
}
export interface SegmentPath {
  segment: string | number;
  parent: Path;
}
export type Path = EmptyPath | SegmentPath;

export const EmptyPath: EmptyPath = { segment: null };

export function pathExpr(path: Path): PathExpr {
  return { type: "path", path };
}
export function segmentPath(segment: string | number, parent?: Path) {
  return { segment, parent: parent ?? EmptyPath };
}
export type EvalExpr =
  | LetExpr
  | ArrayExpr
  | CallExpr
  | VarExpr
  | ValueExpr
  | FunctionExpr
  | OptionalExpr
  | PathExpr;

export interface VarExpr {
  type: "var";
  variable: string;
}

export interface LetExpr {
  type: "let";
  variables: [string, EvalExpr][];
  expr: EvalExpr;
}
export interface ArrayExpr {
  type: "array";
  values: EvalExpr[];
}

export interface OptionalExpr {
  type: "optional";
  value: EvalExpr;
  condition: EvalExpr;
}

export interface CallExpr {
  type: "call";
  function: string;
  args: EvalExpr[];
}

export interface ValueExpr {
  type: "value";
  value: any;
}

export interface PathExpr {
  type: "path";
  path: Path;
}

export interface LambdaExpr {
  type: "lambda";
  variable: string;
  expr: EvalExpr;
}

export interface FunctionExpr {
  type: "func";
  resolve: (env: EvalEnv, call: CallExpr) => EnvValue<EvalExpr>;
  evaluate: (env: EvalEnv, args: unknown[]) => EnvValue<unknown>;
}

export type EnvValue<T> = [EvalEnv, T];

export abstract class EvalEnv {
  abstract basePath: Path;
  abstract getVariable(name: string): EvalExpr;
  abstract getData(path: Path): any;
  abstract withVariables(vars: [string, EvalExpr][]): EvalEnv;
  abstract withBasePath(path: Path): EvalEnv;
}

export function concatPath(path1: Path, path2: Path): Path {
  if (path2.segment == null) return path1;
  return { ...path2, parent: concatPath(path1, path2.parent!) };
}

export function varExpr(variable: string): VarExpr {
  return { type: "var", variable };
}

export function valueExpr(value: any): ValueExpr {
  return { type: "value", value };
}

export function arrayExpr(values: EvalExpr[]): ArrayExpr {
  return { type: "array", values };
}

export function optionalExpr(
  value: EvalExpr,
  condition: EvalExpr,
): OptionalExpr {
  return { type: "optional", value, condition };
}

export function callExpr(name: string, args: EvalExpr[]): CallExpr {
  return { type: "call", function: name, args };
}
export function resolve(env: EvalEnv, expr: EvalExpr): EnvValue<EvalExpr> {
  switch (expr.type) {
    case "array":
      return mapEnv(mapAllEnv(env, expr.values, resolve), (x) => arrayExpr(x));
    case "var":
      return resolve(env, env.getVariable(expr.variable));
    case "value":
      return [env, expr];
    case "let":
      return resolve(env.withVariables(expr.variables), expr.expr);
    case "call":
      return (env.getVariable(expr.function) as FunctionExpr).resolve(
        env,
        expr,
      );
    case "path":
      const fullPath = concatPath(env.basePath, expr.path);
      const pathData = env.getData(fullPath);
      if (Array.isArray(pathData)) {
        return [
          env,
          arrayExpr(
            pathData.map((x, i) => pathExpr({ segment: i, parent: fullPath })),
          ),
        ];
      }
      return [env, pathExpr(fullPath)];
    default:
      return [env, expr];
  }
}

export function evaluateOptional(
  env: EvalEnv,
  expr: EvalExpr,
  index: number,
): EnvValue<unknown> {
  switch (expr.type) {
    case "optional":
      const cond = evaluate(env, expr.condition);
      const condValue = cond[1];
      return (typeof condValue == "number" && condValue == index) ||
        condValue === true
        ? flatmapEnv(cond, (e) => evaluate(e, expr.value))
        : mapEnv(cond, (_) => undefined);
    default:
      return evaluate(env, expr);
  }
}

export function evaluate(env: EvalEnv, expr: EvalExpr): EnvValue<unknown> {
  if (!env) debugger;
  switch (expr.type) {
    case "value":
      return [env, expr.value];
    case "call":
      const args = mapAllEnv(env, expr.args, evaluate);
      return (env.getVariable(expr.function) as FunctionExpr).evaluate(
        args[0],
        args[1],
      );
    case "path":
      return [env, env.getData(expr.path)];
    case "array":
      return mapEnv(mapAllEnv(env, expr.values, evaluateOptional), (a) =>
        a.filter((x) => x !== undefined),
      );
    default:
      throw "Can't evaluate this";
  }
}

export function mapEnv<T, T2>(
  envVal: EnvValue<T>,
  func: (v: T) => T2,
  envFunc?: (e: EvalEnv) => EvalEnv,
): EnvValue<T2> {
  const [e, v] = envVal;
  return [envFunc?.(e) ?? e, func(v)];
}

export function flatmapEnv<T, T2>(
  envVal: EnvValue<T>,
  func: (env: EvalEnv, v: T) => EnvValue<T2>,
): EnvValue<T2> {
  return func(envVal[0], envVal[1]);
}

function envEffect<T>(env: EnvValue<T>, func: (t: T) => any): EvalEnv {
  func(env[1]);
  return env[0];
}
function mapAllEnv<T, T2>(
  env: EvalEnv,
  array: T[],
  func: (env: EvalEnv, value: T, ind: number) => EnvValue<T2>,
): EnvValue<T2[]> {
  const accArray: T2[] = [];
  const outEnv = array.reduce(
    (acc, x, ind) => envEffect(func(acc, x, ind), (nx) => accArray.push(nx)),
    env,
  );
  return [outEnv, accArray];
}

class BasicEvalEnv extends EvalEnv {
  constructor(
    private data: any,
    public basePath: Path,
    private vars: Record<string, EvalExpr>,
  ) {
    super();
  }

  getVariable(name: string): EvalExpr {
    return this.vars[name]!;
  }
  getData(path: Path): any {
    if (path.segment == null) return this.data;
    return this.getData(path.parent)[path.segment];
  }
  withVariables(vars: [string, EvalExpr][]): EvalEnv {
    return new BasicEvalEnv(
      this.data,
      this.basePath,
      Object.fromEntries(Object.entries(this.vars).concat(vars)),
    );
  }
  withBasePath(path: Path): EvalEnv {
    return new BasicEvalEnv(this.data, path, this.vars);
  }
}

export function basicEnv(data: any): EvalEnv {
  return new BasicEvalEnv(
    data,
    { segment: null },
    {
      "?": condFunction,
      "+": binFunction((a, b) => a + b),
      "-": binFunction((a, b) => a - b),
      "*": binFunction((a, b) => a * b),
      "/": binFunction((a, b) => a / b),
      ">": binFunction((a, b) => a > b),
      "<": binFunction((a, b) => a < b),
      "<=": binFunction((a, b) => a <= b),
      ">=": binFunction((a, b) => a >= b),
      "=": binFunction((a, b) => a == b),
      "!=": binFunction((a, b) => a != b),
      string: stringFunction,
      sum: sumFunction,
      count: countFunction,
      ".": mapFunction,
      "[": filterFunction,
    },
  );
}

function resolveCall(env: EvalEnv, callExpr: CallExpr): EnvValue<CallExpr> {
  return mapEnv(mapAllEnv(env, callExpr.args, resolve), (args) => ({
    ...callExpr,
    args,
  }));
}

export function binFunction(func: (a: any, b: any) => unknown): FunctionExpr {
  return {
    type: "func",
    resolve: resolveCall,
    evaluate: (env, args) => {
      const [a, b] = args;
      if (a == null || b == null) return [env, null];
      return [env, func(a, b)];
    },
  };
}

const mapFunction: FunctionExpr = {
  type: "func",
  resolve: (env: EvalEnv, call: CallExpr) => {
    const [left, right] = call.args;
    return resolveElem(resolve(env, left));

    function resolveElem(elem: EnvValue<EvalExpr>): EnvValue<EvalExpr> {
      const [nextEnv, firstArg] = elem;
      if (firstArg.type === "optional") {
        const resolvedVal = resolveElem([elem[0], firstArg.value]);
        return mapEnv(resolvedVal, (x) => ({ ...firstArg, value: x }));
      }
      if (firstArg.type === "path") {
        const pathData = nextEnv.getData(firstArg.path);
        if (pathData == null) return [nextEnv, valueExpr(null)];
        if (typeof pathData === "object") {
          return mapEnv(
            resolve(nextEnv.withBasePath(firstArg.path), right),
            (x) => x,
            (e) => e.withBasePath(nextEnv.basePath),
          );
        }
        throw new Error("Data can't be mapped");
      }
      if (firstArg.type === "array") {
        const elems = mapAllEnv(nextEnv, firstArg.values, (e, x) =>
          resolveElem([e, x]),
        );
        return mapEnv(elems, (x) => arrayExpr(x));
      }
      throw new Error("Can't map:" + JSON.stringify(firstArg));
    }
  },
  evaluate: (env: EvalEnv, args: any[]) => {
    throw new Error("Function not implemented.");
  },
};

const filterFunction: FunctionExpr = {
  type: "func",
  resolve: (env: EvalEnv, call: CallExpr) => {
    const [left, right] = call.args;
    const [nextEnv, leftValue] = resolve(env, left);
    if (leftValue.type === "array") {
      const filteredArray = mapAllEnv(nextEnv, leftValue.values, (e, v) => {
        const value = resolve(e, v);
        const cond = resolve(value[0], callExpr(".", [v, right]));
        return mapEnv(cond, (x) => optionalExpr(value[1], x));
      });
      return mapEnv(filteredArray, (x) => arrayExpr(x));
    }
    throw new Error(
      "Function not implemented." + JSON.stringify(leftValue.type),
    );
  },
  evaluate: (env: EvalEnv, args: unknown[]) => {
    throw new Error("Should have been resolved");
  },
};

const condFunction: FunctionExpr = {
  type: "func",
  resolve: (env: EvalEnv, call: CallExpr) => {
    return mapEnv(mapAllEnv(env, call.args, resolve), (x) => ({
      ...call,
      args: x,
    }));
  },
  evaluate: (env: EvalEnv, args: any[]) => {
    return [env, args[0] ? args[1] : args[2]];
  },
};

function asArray(v: unknown): unknown[] {
  return Array.isArray(v) ? v : [v];
}

function aggFunction<A>(
  v: unknown[],
  init: A,
  op: (acc: A, x: unknown) => A,
): any {
  function recurse(v: unknown[]): any {
    if (v.some(Array.isArray)) {
      return v.map((x) => recurse(asArray(x)));
    }
    return v.reduce(op, init);
  }
  if (v.length == 1) return recurse(v[0] as unknown[]);
  return recurse(v);
}
const sumFunction: FunctionExpr = {
  type: "func",
  resolve: resolveCall,
  evaluate: (e, vals) => [
    e,
    aggFunction(vals, 0, (acc, b) => acc + (b as number)),
  ],
};

const countFunction: FunctionExpr = {
  type: "func",
  resolve: resolveCall,
  evaluate: (e, vals) => [e, aggFunction(vals, 0, (acc, b) => acc + 1)],
};

function toString(v: unknown): string {
  switch (typeof v) {
    case "string":
      return v;
    case "boolean":
      return v ? "true" : "false";
    case "undefined":
      return "null";
    case "object":
      if (Array.isArray(v)) return v.map(toString).join("");
      if (v == null) return "null";
      return JSON.stringify(v);
    default:
      return (v as any).toString();
  }
}

const stringFunction: FunctionExpr = {
  type: "func",
  resolve: resolveCall,
  evaluate: (env, vals) => [env, toString(vals)],
};
