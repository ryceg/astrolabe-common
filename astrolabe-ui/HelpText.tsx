import { ReactNode, useEffect, useRef, useState } from "react";
import { Popover } from "./Popover";
import { clsx } from "clsx";

export function HelpText({
	children,
	iconClass,
	className = "max-w-2xl",
}: {
	children: ReactNode;
	iconClass?: string;
	className?: string;
}) {
	const [open, setOpen] = useState({ open: false, hoverOpen: false });
	const hoverRef = useRef(false);
	const openTimerRef = useRef(0);
	const closeTimerRef = useRef(0);
	const contentRef = useRef(false);
	useEffect(() => {
		return clearTimeout;
	}, []);
	return (
		<Popover
			content={
				<div onMouseEnter={() => (contentRef.current = true)}>{children}</div>
			}
			side="top"
			open={open.open}
			onOpenChange={(o) => {
				setOpen((x) => (x.open != o ? { open: o, hoverOpen: x.hoverOpen } : x));
				clearTimeout();
			}}
			className={className}
			children={
				<i
					className={clsx(
						"text-primary-500 fa fa-question-circle cursor-help print:hidden",
						iconClass
					)}
					onMouseEnter={triggerEnter}
					onMouseLeave={triggerLeave}
				/>
			}
		/>
	);

	function clearTimeout() {
		window.clearTimeout(openTimerRef.current);
		window.clearTimeout(closeTimerRef.current);
	}

	function triggerEnter() {
		clearTimeout();
		openTimerRef.current = window.setTimeout(() => {
			contentRef.current = false;
			setOpen((c) => (!c.open ? { open: true, hoverOpen: true } : c));
		}, 1000);
	}

	function triggerLeave() {
		clearTimeout();
		closeTimerRef.current = window.setTimeout(() => {
			setOpen((x) =>
				x.hoverOpen ? { open: contentRef.current, hoverOpen: false } : x
			);
		}, 200);
	}
}
