import { Control, RenderForm, useControlEffect } from "@react-typed-forms/core";
import { useState } from "react";
import clsx from "clsx";

export function Numberfield({
	control,
	label,
	required = false,
	className,
}: {
	control: Control<number | null | undefined>;
	label: string;
	required?: boolean;
	className?: string;
}) {
	const id = "c" + control.uniqueId;

	const [field, setField] = useState(makeTextAndValue(control.current.value));

	useControlEffect(
		() => control.value,
		(v) => setField((fv) => (fv[1] === v ? fv : makeTextAndValue(v)))
	);

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
						type="number"
						className="input-field"
						{...props}
						value={field[0]}
						onChange={(e) => {
							const textValue = e.target.value;
							const v = parseFloat(textValue);
							const newValue = isNaN(v) ? null : v;
							setField([textValue, v]);
							control.value = newValue;
						}}
					/>
				)}
			/>
		</div>
	);

	function makeTextAndValue(
		value?: number | null
	): [string | number, number | null | undefined] {
		return [typeof value === "number" ? value : "", value];
	}
}
