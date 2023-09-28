import { useCallback, useRef, useEffect } from "react";

type IntersectionCallback = (entries: IntersectionObserverEntry[]) => void;

interface IntersectionOptions extends IntersectionObserverInit {
	callback: IntersectionCallback;
}

/**
 * A hook that uses the IntersectionObserver API to observe changes in the intersection of a target element with an ancestor element or with a top-level document's viewport.
 * @param options - An object containing the IntersectionObserver options and a callback function to be called when the intersection changes.
 * @returns A ref object that should be attached to the target element to be observed.
 * @example A simple example that sets the value of a boolean ref to true when the target element is not fully visible.
 * ```tsx
const isPinned = useControl(false);
const titleRef = useIntersectionObserver({
    callback: ([entry]) => {
        isPinned.value = entry.intersectionRatio < 1;
    },
    threshold: [1],
});

return (
    <>
        <h1
            ref={titleRef}
            className={clsx("sticky top-[-1px] z-10 bg-white py-2", {
                "border-primary-900 border-b-2": isPinned.value,
            })}
        >
            Routes
        </h1>
    </>
);
```
 */
export function useIntersectionObserver(options: IntersectionOptions) {
	const { callback, ...observerOptions } = options;
	const targetRef = useRef(null);

	useEffect(() => {
		const observer = new IntersectionObserver(callback, observerOptions);
		if (targetRef.current) {
			observer.observe(targetRef.current);
		}

		return () => {
			if (targetRef.current) {
				observer.unobserve(targetRef.current);
			}
		};
	}, [callback, observerOptions]);

	return targetRef;
}
