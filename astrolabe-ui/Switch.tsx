import { Control, Fcheckbox } from "@react-typed-forms/core";
import React from "react";
import { cva, VariantProps } from "class-variance-authority";

const switchVariants = cva(
	"bg-surface-300 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border after:rounded-full after:transition-all ",
	{
		variants: {
			variant: {
				primary: "peer-focus:ring-primary-400 peer-checked:bg-primary-500",
				secondary:
					"peer-focus:ring-secondary-400 peer-checked:bg-secondary-500",
			},
			size: {
				md: "w-9 h-5 after:h-4 after:w-4 peer-focus:ring-2",
				sm: "w-7 h-3 after:h-2 after:w-3 peer-focus:ring-1",
			},
		},
		defaultVariants: {
			size: "md",
			variant: "primary",
		},
	}
);

export interface SwitchProps extends VariantProps<typeof switchVariants> {
	control: Control<boolean>;
}

export function Switch({ control, size, variant }: SwitchProps) {
	return (
		<div className="relative">
			<Fcheckbox
				control={control}
				className="peer sr-only"
				type="checkbox"
				value=""
			/>
			<div className={switchVariants({ size, variant })} />
		</div>
	);
}
