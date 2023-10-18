import { Control, RenderForm } from "@react-typed-forms/core";
import clsx from "clsx";

export function Textfield({
	control,
	label,
	required = false,
	className,
	...externalProps
}: React.InputHTMLAttributes<HTMLInputElement> & {
	control: Control<string | null | undefined>;
	label: string;
	required?: boolean;
	className?: string;
}) {
	const id = "c" + control.uniqueId;
	return (
		<div className={clsx("flex flex-col", className)}>
			<label htmlFor={id} className="font-bold">
				{label}
				{required ? " *" : ""}
			</label>
			<RenderForm
				control={control}
				children={({ errorText, value, ...props }) => (
					<input
						type="text"
						className="input-field"
						{...props}
						{...externalProps}
						value={value == null ? "" : value}
					/>
				)}
			/>
		</div>
	);
}
