import {useEffect, useRef} from "react";
import {useInView} from "framer-motion";

export function useScrollIntoView<E extends HTMLElement = HTMLDivElement>(
    shouldBeInView: boolean
) { 
    const itemRef = useRef<E | null>(null);
    const inView = useInView(itemRef);
    const inViewRef = useRef(inView);
    inViewRef.current = inView;
    useEffect(() => {
        if (shouldBeInView && !inViewRef.current) {
            itemRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }, [shouldBeInView]);
    return itemRef;
}
