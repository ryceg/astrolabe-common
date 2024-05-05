import {
  ChangeListenerFunc,
  Control,
  ControlChange,
  Subscription,
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
type TrackedSubscription = [
  Control<any>,
  Subscription | undefined,
  ControlChange,
];

export function makeChangeTracker(
  listen: ChangeListenerFunc<any>,
): [ChangeListenerFunc<any>, (destroy?: boolean) => void] {
  let subscriptions: TrackedSubscription[] = [];
  return [
    (c, change) => {
      const existing = subscriptions.find((x) => x[0] === c);
      if (existing) {
        existing[2] |= change;
      } else {
        subscriptions.push([c, c.subscribe(listen, change), change]);
      }
    },
    (destroy) => {
      if (destroy) {
        subscriptions.forEach((x) => x[0].unsubscribe(listen));
        subscriptions = [];
        return;
      }
      let removed = false;
      subscriptions.forEach((sub) => {
        const [c, s, latest] = sub;
        if (s) {
          if (s[0] !== latest) {
            if (!latest) {
              removed = true;
              c.unsubscribe(s);
              sub[1] = undefined;
            } else s[0] = latest;
          }
        } else {
          sub[1] = c.subscribe(listen, latest);
        }
        sub[2] = 0;
      });
      if (removed) subscriptions = subscriptions.filter((x) => x[1]);
    },
  ];
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
      if (p in cv) return trackedStructure((cc.fields as any)[p], tracker);
      return undefined;
    },
  }) as A;
}
