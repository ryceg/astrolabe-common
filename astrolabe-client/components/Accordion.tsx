"use client";

import * as React from "react";
import * as AccordionPrimitive from "@radix-ui/react-accordion";

import { cn } from "../util/utils";

const AccordionRoot = AccordionPrimitive.Root;

const AccordionItem = React.forwardRef<
	React.ElementRef<typeof AccordionPrimitive.Item>,
	React.ComponentPropsWithoutRef<typeof AccordionPrimitive.Item>
>(({ className, ...props }, ref) => (
	<AccordionPrimitive.Item
		ref={ref}
		className={cn("border-b", className)}
		{...props}
	/>
));
AccordionItem.displayName = "AccordionItem";

const AccordionTrigger = React.forwardRef<
	React.ElementRef<typeof AccordionPrimitive.Trigger>,
	React.ComponentPropsWithoutRef<typeof AccordionPrimitive.Trigger>
>(({ className, children, ...props }, ref) => (
	<AccordionPrimitive.Header className="flex">
		<AccordionPrimitive.Trigger
			ref={ref}
			className={cn(
				"hover:bg-surface-50 flex flex-1 items-center justify-between rounded-t-2xl p-3 font-medium transition-all [&[data-state=open]>i]:rotate-180",
				className
			)}
			{...props}
		>
			{children}
			<i className="fa-solid fa-chevron-down h-4 w-4 shrink-0 transition-transform duration-200" />
		</AccordionPrimitive.Trigger>
	</AccordionPrimitive.Header>
));
AccordionTrigger.displayName = AccordionPrimitive.Trigger.displayName;

const AccordionContent = React.forwardRef<
	React.ElementRef<typeof AccordionPrimitive.Content>,
	React.ComponentPropsWithoutRef<typeof AccordionPrimitive.Content>
>(({ className, children, ...props }, ref) => (
	<AccordionPrimitive.Content
		ref={ref}
		className={cn(
			"data-[state=closed]:animate-accordion-up data-[state=open]:animate-accordion-down overflow-hidden text-sm transition-all",
			className
		)}
		{...props}
	>
		<div className="pb-4 pt-0">{children}</div>
	</AccordionPrimitive.Content>
));
AccordionContent.displayName = AccordionPrimitive.Content.displayName;

function Accordion({
	children,
	type = "single",
	collapsible = true,
	...props
}: {
	children: {
		contents: React.ReactNode;
		title: React.ReactNode;
	}[];
	collapsible?: boolean;
	type?: "single" | "multiple";
}) {
	return (
		<AccordionRoot collapsible={collapsible} type={type} {...props}>
			{children.map((child, i) => (
				<AccordionItems key={i} title={child.title} children={child.contents} />
			))}
		</AccordionRoot>
	);
}

function AccordionItems({
	children,
	title,
}: {
	children: React.ReactNode;
	title: React.ReactNode;
}) {
	return (
		<AccordionItem value={Math.random().toString()}>
			<AccordionTrigger>{title}</AccordionTrigger>
			<AccordionContent>{children}</AccordionContent>
		</AccordionItem>
	);
}

export {
	Accordion,
	AccordionRoot,
	AccordionItem,
	AccordionTrigger,
	AccordionContent,
};
