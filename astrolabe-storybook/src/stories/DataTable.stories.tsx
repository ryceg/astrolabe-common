import { Meta, StoryObj } from "@storybook/react";
import { DataTable, DataTableProps } from "@astrolabe/ui/table/DataTable";
import { Button } from "@astrolabe/ui/Button";
import { columnDefinitions } from "@astrolabe/ui/table";
import { useControl } from "@react-typed-forms/core";

const meta: Meta = {
	component: DataTable,
	parameters: {
		layout: "centered",
	},
	decorators: [
		(Story, params) => {
			const loading = useControl(false);
			return (
				<Story
					args={{
						...params.args,
						loading,
					}}
				/>
			);
		},
	],
	args: {
		columns: [], // replace with actual column definitions
		data: [], // replace with actual data
		loading: false,
	},
};

export default meta;

const columns = columnDefinitions(
	{
		title: "ID",
		sortField: "id",
		getter: (v) => v.id,
		render: (v) => <Button className="flex items-center gap-2 ">{v.id}</Button>,
	},
	{
		title: "Name",
		render: (v) => v.name,
	}
);

const data = [
	{
		id: 1,
		name: "Bojack Horseman",
	},
	{
		id: 2,
		name: "Princess Carolyn",
	},
];

type Story = StoryObj<DataTable>;

export const Primary: Story = {
	render: (args) => {
		return (
			<DataTable
				{...args}
				data={data}
				columns={columns}
				cellClass="text-black"
				headerCellClass="sticky flex px-2 font-bold items-end top-0 text-black"
			/>
		);
	},
};
