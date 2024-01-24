import { useControl } from "@react-typed-forms/core";
import { useEffect } from "react";

export function useScreenWidth() {
  if (typeof window === "undefined") return useControl(1024);
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
