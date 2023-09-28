import { useIntersectionObserver } from "./useIntersectionObserver";
import { useControl } from "@react-typed-forms/core";

/**
 * A hook that returns a reference to a sticky title element and a control that stores a boolean
 * indicating whether the title is pinned or not based on its intersection with the viewport.
 * @returns `isOffscreen` - A control that stores a boolean indicating whether the title is pinned or not.
 * @returns `titleRef` - A reference to the sticky title element.
 */
export function useIsOffscreen() {
	const isOffscreen = useControl(false);

	const titleRef = useIntersectionObserver({
		callback: ([entry]) => {
			isOffscreen.value = entry.intersectionRatio < 1;
		},
		threshold: [1],
	});

	return { titleRef, isOffscreen };
}
