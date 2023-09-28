import { useControl } from "@react-typed-forms/core";
import { useEffect } from "react";

export function useScreenWidth() {
	const screenWidth = useControl(window.innerWidth);
	useEffect(() => {
		const handleResize = () => {
			screenWidth.value = window.innerWidth;
		};
		window.addEventListener("resize", handleResize);
		handleResize();
		// Remove event listener on cleanup
		return () => {
			window.removeEventListener("resize", handleResize);
		};
	}, []);
	return screenWidth;
}
