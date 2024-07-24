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
  evaluate: (env: EvalEnv, args: any[]) => EnvValue<any>;
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

export function valueExpr(value: any): ValueExpr {
  return { type: "value", value };
}

export function arrayExpr(values: EvalExpr[]): ArrayExpr {
  return { type: "array", values };
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
      return [env, pathExpr(concatPath(env.basePath, expr.path))];
    default:
      return [env, expr];
  }
}

export function evaluate(env: EvalEnv, expr: EvalExpr): EnvValue<any> {
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
      return mapAllEnv(env, expr.values, evaluate);
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
    { "+": binFunction((a, b) => a + b), ".": mapFunction },
  );
}

export function binFunction(func: (a: any, b: any) => any): FunctionExpr {
  return {
    type: "func",
    resolve: (env, callExpr) => {
      return mapEnv(mapAllEnv(env, callExpr.args, resolve), (args) => ({
        ...callExpr,
        args,
      }));
    },
    evaluate: (env, args) => {
      const [a, b] = args;
      if (a == null || b == null) return null;
      return func(a, b);
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
      if (firstArg.type === "path") {
        const pathData = nextEnv.getData(firstArg.path);
        if (pathData == null) return [nextEnv, valueExpr(null)];
        if (Array.isArray(pathData)) {
          const elems = mapAllEnv(nextEnv, pathData, (e, _, i) =>
            resolve(
              e.withBasePath({ segment: i, parent: firstArg.path }),
              right,
            ),
          );
          return mapEnv(
            elems,
            (x) => arrayExpr(x),
            (e) => e.withBasePath(nextEnv.basePath),
          );
        }
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
      throw new Error("Function not implemented." + firstArg.type);
    }
  },
  evaluate: (env: EvalEnv, args: any[]) => {
    throw new Error("Function not implemented.");
  },
};
