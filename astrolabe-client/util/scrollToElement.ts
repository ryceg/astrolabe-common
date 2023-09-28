export function scrollToElement(element?: HTMLElement | null, focus?: boolean) {
	if (!window || !element) return;

	const elementRect = element.getBoundingClientRect();
	const absoluteElementTop = elementRect.top + window.scrollY;
	const middle = absoluteElementTop - window.innerHeight / 2;
	window.scrollTo({ top: middle, behavior: "smooth" });

	if (focus) {
		window.setTimeout(() => element.focus(), 0);
	}
}
