import React, { ReactElement, useEffect } from "react";
import {
	ColumnDef,
	DataTableView,
	GridItemsRenderer,
	GridItemsSpan,
	TableViewClasses,
	useClientSideFilter,
	useDataTableState,
} from "./index";

export interface DataTableProps<T, D> extends TableViewClasses {
	columns: ColumnDef<T, D>[];
	data: T[];
	loading: boolean;
	rowId?: (row: T, index: number) => string | number;
	pageSize?: number;
	query?: string;
	paged?: boolean;
	renderHeader?: (
		renderer: GridItemsRenderer,
		childrenRowSpan: number
	) => GridItemsSpan;
}

export function DataTable<T, D = undefined>({
	data,
	loading,
	pageSize,
	...props
}: DataTableProps<T, D>) {
	const state = useDataTableState({ perPage: pageSize ?? 10, loading });

	useEffect(() => {
		state.fields.loading.value = loading;
		state.fields.perPage.value = pageSize ?? 10;
	}, [loading, pageSize]);

	const [pageProps] = useClientSideFilter(
		state,
		props.columns,
		data,
		props.paged ?? true
	);
	return <DataTableView {...props} {...pageProps} state={state} />;
}
