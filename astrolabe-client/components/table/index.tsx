import React, {
	CSSProperties,
	Key,
	ReactElement,
	ReactNode,
	useEffect,
	useMemo,
} from "react";
import { setIncluded } from "../../util/arrays";
import { Control, useControlValue } from "@react-typed-forms/core";
import {
	SearchFilters,
	SearchingState,
	SortDirection,
	SortField,
	sortFieldToString,
	stringToSortField,
	useSearchingState,
} from "../../app/searching";
import { LinearProgress } from "../LinearProgress";

export { DataTableView } from "./DataTableView";
export { DataTable } from "./DataTable";

export interface GridPlacementStyles {
	display?: string;
	gridColumnStart?: string;
	gridColumnEnd?: string;
	gridRowStart?: string;
}

export type Sortable = string | number | undefined | null | boolean | Date;

export function compareAny(first: any, second: any): number {
	return first === second ? 0 : first > second ? 1 : -1;
}

export type DataTableState = SearchingState;

export const useDataTableState = useSearchingState;

type IdOrTitle =
	| { title: string; id?: string }
	| { id: string; title?: string };

type FilterField<T> = {
	filterField: string;
	filterValue: (row: T) => [string, string];
};

interface GetterColumn<T, D> {
	getter: (row: T) => Sortable;
	render?: CellRenderer<T, D>;
	filterField?: string;
	filterValue?: (row: T) => [string, string];
}

type ChildRowsInit<T, T2, D> = {
	getRows: (row: T) => T2[];
	columns: ColumnDef<T2, D>[];
};

export function childRowColumns<T, T2, D>(
	getRows: (t: T) => T2[],
	...defs: ColumnDefInit<T2, D>[]
): ChildRowsInit<T, any, D> {
	return {
		getRows,
		columns: columnDefinitions(...defs),
	};
}

type ChildColumnsInit<T, D> = ColumnDefInit<T, D>[] | ChildRowsInit<T, any, D>;

type ColumnRenderInit<T, D> =
	| GetterColumn<T, D>
	| ({
			render: CellRenderer<T, D>;
	  } & (FilterField<T> | {}))
	| {
			children: ChildColumnsInit<T, D>;
			render?: CellRenderer<T, D>;
	  };

export interface ColumnHeader<D> {
	id: string;
	title: string;
	columnTemplate?: string;
	columnContainerStyles?: CSSProperties;
	hidden?: boolean;
	filterField?: string;
	sortField?: string;
	defaultSort?: SortDirection;
	cellClass?: string;
	headerCellClass?: string;
	bodyCellClass?: string;
	renderHeaderElement?: ColumnHeaderRenderer<D>;
}

/**
 * A function that takes a row of data, its index, and a function to render a cell, and returns a ReactNode representing the cell.
 * @template T The type of the data in the row.
 * @template D The type of the additional data passed to the column renderer.
 * @param {T} row The row of data to render.
 * @param {number} rowIndex The index of the row to render.
 * @param {(column: ColumnRender<T, D>) => ReactNode} renderCell A function that takes a `ColumnRender` object and returns a ReactNode representing the cell.
 * @returns {ReactNode} A ReactNode representing the cell.
 */
export type CellRenderer<T, D> = (
	row: T,
	rowIndex: number,
	col: ColumnDef<T, D>
) => ReactNode;

export interface RowPlacementProps {
	startRow: number;
	rowSpan: number;
	lastRow: boolean;
}

export type GridItemsRenderer = (placement: RowPlacementProps) => ReactElement;
export type GridItemsSpan =
	| GridItemsRenderer
	| {
			render: GridItemsRenderer;
			rowSpan: number;
	  };

export interface GridElementRendererProps<T, D> {
	column: ColumnDef<any, D>;
	className: string;
	lastRow: boolean;
	content: GridItemsRenderer;
	rowSpan: number;
}

export type ColumnHeaderRenderer<D> = (
	props: GridElementRendererProps<any, D>
) => GridItemsSpan;

export type ColumnBodyRenderer<T, D> = (
	props: GridElementRendererProps<T, D> & { row: T; rowIndex: number }
) => GridItemsSpan;

export interface ColumnRender<T, D = undefined> extends ColumnHeader<D> {
	render: CellRenderer<T, D>;
	data?: D;
	renderBodyElement?: ColumnBodyRenderer<T, D>;
}

export interface ChildColumns<T, D> {
	getChildRows?: (row: T) => any[];
	columns: ColumnDef<any, D>[];
}

export interface ColumnDef<T, D = undefined> extends ColumnRender<T, D> {
	compare?: (first: T, second: T) => number;
	getter?: (row: T) => Sortable;
	filterValue?: (row: T) => [string, string];
	children?: ChildColumns<T, D>;
}

/**
 * Represents the initialization properties for a `ColumnDef` object.
 *
 * @template T The type of the row data.
 * @template D The type of the additional data passed to the `CellRenderer`.
 */
export type ColumnDefInit<T, D = undefined> = Omit<
	ColumnHeader<D>,
	"id" | "title" | "filterField"
> &
	IdOrTitle &
	ColumnRenderInit<T, D> & {
		data?: D;
		compare?: (first: T, second: T) => number;
		renderBodyElement?: ColumnBodyRenderer<T, D>;
	};
/**
 * Returns an array of `ColumnDef` objects based on the provided `ColumnDefInit` objects.
 *
 * @template T The type of the row data.
 * @template D The type of the additional data passed to the `CellRenderer`.
 * @param {...ColumnDefInit<T, D>[]} defs The `ColumnDefInit` objects to use to create the `ColumnDef` objects.
 * @returns {ColumnDef<T, D>[]} An array of `ColumnDef` objects.
 */
export function columnDefinitions<T, D = undefined>(
	...defs: ColumnDefInit<T, D>[]
): ColumnDef<T, D>[] {
	return defs.map((x, i) => {
		const render: CellRenderer<T, D> =
			"render" in x
				? x.render!
				: "children" in x
				? () => <></>
				: (r: T) => (x as GetterColumn<T, D>).getter(r)?.toString();
		const columnDef: ColumnDef<T, D> = {
			...x,
			id: x.id ?? (x.title ? x.title : i.toString()),
			title: x.title ?? "",
			render,
			children:
				"children" in x
					? Array.isArray(x.children)
						? {
								columns: columnDefinitions(...x.children),
						  }
						: {
								getChildRows: x.children.getRows,
								columns: x.children.columns,
						  }
					: undefined,
		};
		if (columnDef.sortField && !columnDef.compare) {
			const getter = columnDef.getter;
			if (!getter)
				throw new Error("Must supply getter or compare for sortField");
			{
				columnDef.compare = (f: T, s: T) => compareAny(getter(f), getter(s));
			}
		}
		if (columnDef.filterField && !columnDef.filterValue) {
			const getter = columnDef.getter;
			if (!getter)
				throw new Error("Must supply getter or compare for filterField");
			{
				columnDef.filterValue = getterToFilter(getter);
			}
		}
		return columnDef;
	});
}

/**
 * Returns a function that filters rows based on the provided `columns` and `filters`.
 *
 * @template T The type of the row data.
 * @param {ColumnDef<T>[]} columns The columns to use for filtering.
 * @param {ColumnFilters} filters The filters to apply to the rows.
 * @returns {((f: T) => boolean) | undefined} A function that filters rows based on the provided `columns` and `filters`.
 */
function makeFilterFunc<T>(
	columns: ColumnDef<T>[],
	filters: ColumnFilters
): ((f: T) => boolean) | undefined {
	const fv: [(row: T) => [string, string], string[]][] = [];
	Object.keys(filters).forEach((ch) => {
		const vals = filters[ch];
		if (!vals || vals.length === 0) {
			return;
		}
		const col = columns.find((c) => c.filterField === ch);
		if (col && col.filterValue) {
			fv.push([col.filterValue, vals]);
		}
	});
	if (fv.length === 0) {
		return undefined;
	}
	return (row) => fv.every(([f, vals]) => vals.includes(f(row)[0]));
}

/**
 * Returns a function that sets the filter value for a given column in a `ColumnFilters` object.
 *
 * @param {string} column The column to set the filter value for.
 * @param {string} value The value to set the filter to.
 * @param {boolean} set Whether to set the filter to the provided value or remove it.
 * @returns {(f: ColumnFilters) => ColumnFilters} A function that sets the filter value for a given column in a `ColumnFilters` object.
 */
export function setFilterValue(
	column: string,
	value: string,
	set: boolean
): (f: ColumnFilters) => ColumnFilters {
	return (filters) => {
		let curValues = filters[column];
		if (!set && !curValues) {
			return filters;
		}
		const newValues = !curValues ? [value] : setIncluded(curValues, value, set);
		if (newValues === curValues) {
			return filters;
		}
		const newFilters = { ...filters };
		newFilters[column] = newValues;
		return newFilters;
	};
}

/**
 * Converts a getter function that returns a `Sortable` value to a filter function that returns a tuple of strings.
 *
 * @template T The type of the row data.
 * @param {(row: T) => Sortable} getter The getter function to convert to a filter function.
 * @returns {(row: T) => [string, string]} A filter function that returns a tuple of strings.
 */
export function getterToFilter<T>(
	getter: (row: T) => Sortable
): (row: T) => [string, string] {
	return (r) => {
		const v = getter(r);
		if (typeof v === "string") {
			return [v, v];
		}
		if (v === null || v === undefined) {
			return ["", "<Empty>"];
		}
		const sv = v.toString();
		return [sv, sv];
	};
}

/**
 * Returns a function that sets the sort direction for a given column in a list of sort fields.
 * If `dir` is not provided, it will remove the sort field for the given column.
 *
 * @param {ColumnHeader} column The column to set the sort direction for.
 * @param {SortDirection} [dir] The sort direction to set for the given column.
 * @returns {(existing: string[]) => string[]} A function that sets the sort direction for a given column in a list of sort fields.
 */
export function setColumnSort(
	column: ColumnHeader<any>,
	dir?: SortDirection
): (existing: string[]) => string[] {
	return (cols) => {
		const sortField = column.sortField!;
		const withoutExisting = cols.filter(
			(c) => stringToSortField(c)[0] !== sortField
		);
		return dir
			? [sortFieldToString([sortField, dir]), ...withoutExisting]
			: withoutExisting;
	};
}

/**
 * Returns a function that rotates the sort direction for a given column in a list of sort fields.
 * If the column is not currently being sorted, it will add it to the list with the default sort direction.
 * If the column is currently being sorted in ascending order, it will change the sort direction to descending.
 * If the column is currently being sorted in descending order, it will remove the sort field for the given column.
 *
 * @param {ColumnHeader} column The column to rotate the sort direction for.
 * @returns {(existing: string[]) => string[]} A function that rotates the sort direction for a given column in a list of sort fields.
 */
export function rotateSort(
	column: ColumnHeader<any>
): (existing: string[]) => string[] {
	return (cols) => {
		const sortField = column.sortField!;
		const currentSort = cols.find((c) => stringToSortField(c)[0] === sortField);
		const currentDirection = currentSort
			? stringToSortField(currentSort)[1]
			: undefined;
		const defaultSort = column.defaultSort ?? "asc";
		if (!currentDirection) {
			return [sortFieldToString([sortField, defaultSort]), ...cols];
		}
		let nextDir: SortDirection | undefined;
		switch (currentDirection) {
			case "asc":
				nextDir = defaultSort === "asc" ? "desc" : undefined;
				break;
			case "desc":
				nextDir = defaultSort === "desc" ? "asc" : undefined;
		}
		const withoutExisting = cols.filter(
			(c) => stringToSortField(c)[0] !== sortField
		);
		return nextDir
			? [sortFieldToString([sortField, nextDir]), ...withoutExisting]
			: withoutExisting;
	};
}

export type ColumnFilters = SearchFilters;

export interface TablePageData<T> {
	pageRows: number;
	getRow(index: number): T;
	useFilterValues(field: string): [string, string][];
}

export interface TableBaseData<T, D = unknown> extends TablePageData<T> {
	columns: ColumnDef<T, D>[];
	rowId?: (row: T, index: number) => string | number;
	state: DataTableState;
}

export interface TableViewClasses {
	className?: string;
	headerCellClass?: string;
	lastRowClass?: string;
	cellClass?: string;
	bodyCellClass?: string;
	defaultColumnTemplate?: string;
}

export const defaultTableClasses: TableViewClasses = {
	headerCellClass: "font-bold",
	cellClass: "px-1",
	bodyCellClass: "border-t py-1 flex items-center",
};

export interface TableRenderData<T> extends TableBaseData<T> {
	loading: boolean;
}

/**
 * Returns a function that generates an array of filter values for a given field.
 * @param columns The columns of the table.
 * @param data The data of the table.
 * @param maxFilterValues The maximum number of filter values to return.
 * @param refreshFilterDep An optional dependency to trigger a refresh of the filter values.
 * @returns A function that generates an array of filter values for a given field.
 */
export function createClientFilterValues<T>(
	columns: ColumnDef<T, any>[],
	data: T[],
	maxFilterValues: number = 100,
	refreshFilterDep?: Control<any>
): (field: string) => [string, string][] {
	return (field: string) => {
		const filterValue = columns.find((x) => x.filterField === field)!
			.filterValue!;
		const doRefresh = refreshFilterDep ? refreshFilterDep.value : undefined;
		return useMemo(() => {
			const allValues: { [k: string]: string } = {};
			let valueCount = 0;
			for (let i = 0; i < data.length; i++) {
				const row = data[i];
				const [v, n] = filterValue(row);
				if (allValues[v] === undefined) {
					allValues[v] = n;
					valueCount++;
					if (valueCount >= maxFilterValues) break;
				}
			}
			return Object.entries(allValues).sort((a, b) => a[0].localeCompare(b[0]));
		}, [data, field, doRefresh]);
	};
}

/**
 * Returns a tuple containing the page data and the sorted data based on the client-side filters, sorting, and pagination.
 * @param state The state of the data table.
 * @param columns The columns of the table.
 * @param data The data of the table.
 * @param isPaginated A boolean indicating whether the table is paginated.
 * @param maxFilterValues The maximum number of filter values to return.
 * @param refreshFilterDep An optional dependency to trigger a refresh of the filter values.
 * @param updateTotal A boolean indicating whether to update the total number of rows in the state.
 * @returns A tuple containing the page data and the sorted data based on the client-side filters, sorting, and pagination.
 */
export function useClientSideFilter<T>(
	state: DataTableState,
	columns: ColumnDef<T, any>[],
	data: T[],
	isPaginated: boolean,
	maxFilterValues: number = 100,
	refreshFilterDep?: Control<any>,
	updateTotal: boolean = true
): [TablePageData<T>, T[]] {
	const fields = state.fields;
	const query = fields.query.value;
	const filters = fields.filters.value;
	const sorts = fields.sort.value;
	const page = fields.page.value;
	const perPage = fields.perPage.value;

	const filteredData = useMemo(
		() =>
			filterByQuery(
				columns,
				data,
				query ?? "",
				makeFilterFunc(columns, filters)
			),
		[data, filters, columns, query]
	);

	const sortedData = useMemo(() => {
		return !sorts.length
			? filteredData
			: sortByColumns(filteredData, columns, sorts.map(stringToSortField));
	}, [sorts, columns, filteredData]);

	const pageData = useMemo(() => {
		const offset = page * perPage;
		return isPaginated
			? sortedData.slice(offset, offset + perPage)
			: sortedData;
	}, [page, perPage, sortedData]);

	useEffect(() => {
		if (updateTotal) {
			state.fields.totalRows.value = filteredData.length;
		}
	}, [updateTotal, filteredData.length]);

	return [
		{
			getRow: (index: number) => pageData[index],
			pageRows: pageData.length,
			useFilterValues: createClientFilterValues(
				columns,
				data,
				maxFilterValues,
				refreshFilterDep
			),
		},
		sortedData,
	];
}

/**
 * Sorts an array of data based on the specified columns and sort fields.
 * @param data The data to sort.
 * @param columns The columns to sort by.
 * @param sorts The sort fields to use.
 * @returns The sorted data.
 */
export function sortByColumns<T>(
	data: T[],
	columns: ColumnDef<T>[],
	sorts: SortField[]
) {
	return [...data].sort((first, second) => {
		for (const i in sorts) {
			const [s, order] = sorts[i];
			const c = findColumn(columns, (x) => x.sortField === s);
			const compared = c ? c.compare!(first, second) : 0;
			if (compared) {
				return compared * (order === "asc" ? 1 : -1);
			}
		}
		return 0;
	});
}

/**
 * Filters an array of data based on a search query and additional filter function.
 * @param columns The columns to search through.
 * @param rows The data to filter.
 * @param query The search query.
 * @param additionalFilter An optional function to further filter the data.
 * @returns The filtered data.
 */
export function filterByQuery<V>(
	columns: ColumnDef<V, any>[],
	rows: V[],
	query: string,
	additionalFilter?: (row: V) => boolean
): V[] {
	const lq = query.toLowerCase();
	if (!lq && !additionalFilter) {
		return rows;
	}
	return rows.filter((r) => {
		const allowed = additionalFilter ? additionalFilter(r) : true;
		if (!allowed || !lq) {
			return allowed;
		}
		return columns.some((c) => {
			const val = c.getter?.(r);
			return val && val.toString().toLowerCase().includes(lq);
		});
	});
}

export function sortFilterValues(vals: [string, string][]) {
	return vals.sort((a, b) => a[1].localeCompare(b[1]));
}

export function findColumnRecurse<T, D>(
	m: ColumnDef<T, D>,
	f: (column: ColumnDef<T, D>) => boolean
): ColumnDef<T, D> | undefined {
	if (f(m)) return m;
	if (m.children && !m.children.getChildRows)
		return findColumn(m.children.columns, f);
	return undefined;
}

export function findColumn<T, D>(
	columns: ColumnDef<T, D>[],
	f: (column: ColumnDef<T, D>) => boolean
): ColumnDef<T, D> | undefined {
	for (const child of columns) {
		const found = findColumnRecurse(child, f);
		if (found) return found;
	}
	return undefined;
}
