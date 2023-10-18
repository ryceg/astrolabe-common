import React, { ReactNode, useMemo } from "react";
import {
	addElement,
	Control,
	removeElement,
	RenderElements,
	useControl,
} from "@react-typed-forms/core";
import * as ToastPrim from "@radix-ui/react-toast";
import { Toast, ToastService, ToastSettings } from "@astrolabe/client/service/toast";
import { AppProvider } from "@astrolabe/client/service";
import { cva } from "class-variance-authority";
import { Button } from "./Button";

type ToastState = Toast[];

export function useToastsRenderer(): [
	ToastService,
	AppProvider<ToastPrim.ToastProviderProps>
] {
	const state = useControl<ToastState>([]);

	function addToast(title: ReactNode, settings?: ToastSettings): void {
		addElement(state, { title, settings });
	}

	function Toasts({ children }: { children?: ReactNode }) {
		return (
			<ToastPrim.ToastProvider>
				{children}
				<ToastPrim.Viewport
					className={
						"fixed top-0 z-[100] flex max-h-screen w-full flex-col-reverse p-4 sm:right-0 sm:top-0 sm:flex-col md:max-w-[420px]"
					}
				/>
				<RenderToasts control={state} />
			</ToastPrim.ToastProvider>
		);
	}

	return [{ addToast }, useMemo(() => [Toasts, {}], [])];
}

const toastVariants = cva(
	"group pointer-events-auto relative flex w-full items-center justify-between space-x-4 overflow-auto rounded-md border p-6 pr-8 shadow-lg transition-all data-[swipe=cancel]:translate-x-0 data-[swipe=end]:translate-x-[var(--radix-toast-swipe-end-x)] data-[swipe=move]:translate-x-[var(--radix-toast-swipe-move-x)] data-[swipe=move]:transition-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[swipe=end]:animate-out data-[state=closed]:fade-out-80 data-[state=closed]:slide-out-to-right-full data-[state=open]:slide-in-from-bottom-full data-[state=open]:sm:slide-in-from-top-full",
	{
		variants: {
			variant: {
				info: "bg-blue-500 text-white border-blue-800",
				warning: "bg-warning-500 text-warning-950 border-warning-800",
				success: "bg-success-500 text-success-950 border-success-800",
				error: "bg-danger-500 text-danger-950 border-danger-800",
			},
		},
		defaultVariants: {
			variant: "info",
		},
	}
);

function RenderToasts({ control }: { control: Control<ToastState> }) {
	return <RenderElements control={control} children={renderToast} />;

	function renderToast(t: Control<Toast>) {
		const { settings, title } = t.value;
		const { type, message, action } = settings ?? {};
		return (
			<ToastPrim.Root
				onOpenChange={() => removeElement(control, t)}
				className={toastVariants({ variant: type })}
			>
				<div className="grid gap-1">
					<ToastPrim.Title className="text-sm font-semibold">
						{title}
					</ToastPrim.Title>
					<ToastPrim.Close
						className="absolute right-4 top-4 rounded p-2 transition"
						aria-label="Close"
					>
						<i className="fa-light fa-xmark" aria-hidden></i>
					</ToastPrim.Close>
					{message && (
						<ToastPrim.Description className="overflow-auto text-sm">
							{message}
						</ToastPrim.Description>
					)}
				</div>
				{action && (
					<ToastPrim.Action altText={action.label}>
						<Button variant="outline" onClick={action.response}>
							{action.label}
						</Button>
					</ToastPrim.Action>
				)}
			</ToastPrim.Root>
		);
	}
}
