import {
  ChangeListenerFunc,
  Control,
  ControlChange,
  useControl,
  useControlEffect,
} from "@react-typed-forms/core";

export function useCalculatedControl<V>(calculate: () => V): Control<V> {
  const c = useControl(calculate);
  useControlEffect(calculate, (v) => (c.value = v));
  return c;
}

export function cc(n: string | null | undefined): string | undefined {
  return n ? n : undefined;
}

export function trackedStructure<A>(
  c: Control<A>,
  tracker: ChangeListenerFunc<any>,
): A {
  const cc = c.current;
  const cv = cc.value;
  if (cv == null) {
    tracker(c, ControlChange.Structure);
    return cv;
  }
  if (typeof cv !== "object") {
    tracker(c, ControlChange.Value);
    return cv;
  }
  return new Proxy(cv, {
    get(target: object, p: string | symbol, receiver: any): any {
      if (Array.isArray(cv)) {
        tracker(c, ControlChange.Structure);
        if (typeof p === "symbol" || p[0] >= "9" || p[0] < "0")
          return Reflect.get(cv, p);
        const nc = (cc.elements as any)[p];
        if (typeof nc === "function") return nc;
        if (nc == null) return null;
        return trackedStructure(nc, tracker);
      }
      if (p in (cc.fields as any) || p in cv)
        return trackedStructure((cc.fields as any)[p], tracker);
      return undefined;
    },
  }) as A;
}
