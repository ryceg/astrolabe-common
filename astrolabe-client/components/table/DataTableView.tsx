import React, {
	CSSProperties,
	Fragment,
	HTMLAttributes,
	Key,
	ReactElement,
	useMemo,
} from "react";
import { RenderControl } from "@react-typed-forms/core";
import {
	ColumnDef,
	GridItemsSpan,
	ColumnHeaderRenderer,
	defaultTableClasses,
	TableBaseData,
	TableViewClasses,
	GridItemsRenderer,
	ChildColumns,
	RowPlacementProps,
} from "./index";
import clsx from "clsx";
import { Pagination } from "../Pagination";
import { SortableHeader } from "./SortableHeader";
import { FilterPopover } from "./FilterPopover";

export interface DataTableViewProps<T, D = unknown>
	extends TableBaseData<T, D>,
		TableViewClasses {
	paged?: boolean;
	renderHeader?: (
		renderer: GridItemsRenderer,
		childrenRowSpan: number
	) => GridItemsSpan;
}

export function defaultCellAttributes(
	column: ColumnDef<any, any>,
	content: GridItemsRenderer,
	placement: RowPlacementProps
): { style: React.CSSProperties; children: ReactElement } {
	return {
		style: column.children
			? { display: "contents" }
			: gridAreaStyles(column, placement.startRow, placement.rowSpan),
		children: content(placement),
	};
}
export function defaultRenderCellElement(
	column: ColumnDef<any, any>,
	content: GridItemsRenderer,
	divProps: HTMLAttributes<HTMLDivElement> | undefined,
	placement: RowPlacementProps
) {
	const attrs = defaultCellAttributes(column, content, placement);
	if (column.children && !divProps) return attrs.children;
	return (
		<div
			key={column.id}
			{...attrs}
			{...divProps}
			style={{ ...attrs.style, ...divProps?.style }}
		/>
	);
}

export function DataTableView<T, D = unknown>(props: DataTableViewProps<T, D>) {
	const {
		className,
		headerCellClass,
		cellClass,
		bodyCellClass,
		columns,
		rowId,
		defaultColumnTemplate = "auto",
		state,
		pageRows,
		paged = true,
		getRow,
		lastRowClass,
		useFilterValues,
		renderHeader,
	} = { ...defaultTableClasses, ...props };

	const visibleColumns = useMemo(
		() => columns.filter((c) => !c.hidden),
		[columns]
	);

	const divRenderer: ColumnHeaderRenderer<any> = ({
		column,
		className: cn,
		content,
		rowSpan,
	}) => {
		return {
			render: (p) => {
				const className = clsx(cn, p.lastRow ? lastRowClass : "");
				return defaultRenderCellElement(
					column,
					content,
					className ? { className } : undefined,
					p
				);
			},
			rowSpan,
		};
	};

	function renderHeaderItems(col: ColumnDef<T, D>): GridItemsSpan {
		if (col.children) {
			const childrenItemSpan = renderGridItemRow(
				col.id,
				visibleChildren(col.children).map((cc) => renderHeaderItems(cc))
			);
			return (col.renderHeaderElement ?? divRenderer)({
				column: col,
				className: clsx(col.headerCellClass, col.cellClass),
				lastRow: true,
				content: getItemsRenderer(childrenItemSpan),
				rowSpan: rowsForSpan(childrenItemSpan),
			});
		}
		const { sortField, filterField } = col;

		const childrenWithFilter = (
			<>
				{col.title}
				{filterField && (
					<FilterPopover
						state={state}
						filterField={filterField}
						column={col}
						useFilterValues={useFilterValues}
					/>
				)}
			</>
		);

		const children = sortField ? (
			<SortableHeader
				state={state}
				sortField={sortField}
				column={col}
				children={childrenWithFilter}
			/>
		) : (
			childrenWithFilter
		);

		const className = clsx(
			col.headerCellClass,
			col.cellClass,
			headerCellClass,
			cellClass
		);
		return (col.renderHeaderElement ?? divRenderer)({
			column: col,
			className,
			lastRow: true,
			content: () => children,
			rowSpan: 1,
		});
	}

	function renderGridItemRow(
		key: Key,
		allColumns: GridItemsSpan[],
		disallowLastRow?: boolean
	): GridItemsSpan {
		const maxSpan = allColumns.reduce(
			(acc, c) => Math.max(acc, rowsForSpan(c)),
			0
		);
		return {
			render: ({ startRow, lastRow }) => (
				<Fragment key={key}>
					{allColumns.map((x) =>
						getItemsRenderer(x)({
							startRow,
							rowSpan: maxSpan,
							lastRow: lastRow && !disallowLastRow,
						})
					)}
				</Fragment>
			),
			rowSpan: maxSpan,
		};
	}

	function columnRender(
		c: ColumnDef<T, D>,
		row: T,
		rowIndex: number,
		lastRow: boolean
	): GridItemsSpan {
		const children = c.children;
		if (children) {
			const childRows = children.getChildRows?.(row) ?? [row];
			const rowElements = childRows.map((cr, cri) =>
				renderGridItemRow(
					cri,
					visibleChildren(children).map((cc) =>
						columnRender(cc, cr, cri, lastRow)
					),
					cri !== childRows.length - 1
				)
			);
			return (c.renderBodyElement ?? divRenderer)({
				column: c,
				className: clsx(c.cellClass, c.bodyCellClass),
				lastRow,
				rowIndex,
				content: (p) => (
					<Fragment key={c.id}>
						{rowElements.map((r, ri) =>
							getItemsRenderer(r)({ ...p, startRow: p.startRow + ri })
						)}
					</Fragment>
				),
				rowSpan: rowElements.reduce((acc, re) => acc + rowsForSpan(re), 0),
				row,
			});
		}
		return (c.renderBodyElement ?? divRenderer)({
			column: c,
			className: clsx(c.cellClass, c.bodyCellClass, cellClass, bodyCellClass),
			lastRow,
			rowIndex,
			content: () => (
				<RenderControl key={c.id}>
					{() => c.render(row, rowIndex, c)}
				</RenderControl>
			),
			rowSpan: 1,
			row,
		});
	}

	const { columnTemplate, headerElements, containerStyles } = useMemo(
		() => ({
			columnTemplate: getColumnTemplate(visibleColumns, defaultColumnTemplate),
			headerElements: visibleColumns.map((x) => renderHeaderItems(x)),
			containerStyles: getContainerStyles(visibleColumns),
		}),
		[visibleColumns]
	);

	const fields = state.fields;
	const loading = fields.loading.value;
	const totalRows = fields.totalRows.value;
	const headerItems = renderGridItemRow("header", headerElements);
	const headerItemsSpan = renderHeader
		? renderHeader(getItemsRenderer(headerItems), rowsForSpan(headerItems))
		: headerItems;
	const headerRowCount = rowsForSpan(headerItemsSpan);
	let rowOffset = headerRowCount + 1;
	const body = Array.from({ length: pageRows }, (_, rowNum) => {
		const r = getRow(rowNum);
		const key = rowId?.(r, rowNum) ?? rowNum;
		const lastRow = rowNum === pageRows - 1;
		const allColumns = visibleColumns.map((x) =>
			columnRender(x, r, rowNum, lastRow)
		);
		const rowItemsSpan = renderGridItemRow(key, allColumns);
		const rows = rowsForSpan(rowItemsSpan);
		const elem = getItemsRenderer(rowItemsSpan)({
			startRow: rowOffset,
			rowSpan: rows,
			lastRow,
		});
		rowOffset += rows;
		return elem;
	});
	const dataGrid = (
		<div
			className={className}
			style={{
				display: "grid",
				gridTemplateColumns: columnTemplate,
				...containerStyles,
			}}
		>
			{getItemsRenderer(headerItemsSpan)({
				startRow: 1,
				rowSpan: headerRowCount,
				lastRow: true,
			})}
			{body}
		</div>
	);

	return !paged ? (
		dataGrid
	) : (
		<>
			{dataGrid}
			<RenderControl
				render={() => (
					<Pagination
						total={totalRows}
						perPage={fields.perPage.value}
						page={fields.page.value}
						onPageChange={(p) => (fields.page.value = p)}
					/>
				)}
			/>
		</>
	);
}

export function gridAreaStyles(
	col: ColumnDef<any, any>,
	rowNum: number,
	rowSpan: number
) {
	return {
		...gridColumnStyle(col.id, col.id, true),
		gridRowStart: rowNum,
		gridRowEnd: "span " + rowSpan,
	};
}

export function gridColumnAreaStyles(
	colLineStart: string,
	colLineEnd: string,
	rowNum: number,
	rowSpan: number,
	inclusive?: boolean
) {
	return {
		...gridColumnStyle(colLineStart, colLineEnd, inclusive),
		gridRowStart: rowNum,
		gridRowEnd: "span " + rowSpan,
	};
}

export function gridColumnStyle(
	startCol: string,
	endCol: string,
	inclusive?: boolean
): {
	gridColumnStart: string;
	gridColumnEnd: string;
} {
	return {
		gridColumnStart: getColumnLineName(startCol, true),
		gridColumnEnd: getColumnLineName(endCol, !inclusive),
	};
}

function getColumnLineName(id: string, start: boolean) {
	return `c${id.replaceAll(" ", "_")}_${start ? "s" : "e"}`;
}

function getColumnTemplate(
	cols: ColumnDef<any, any>[],
	defaultColumnTemplate: string
): string {
	const [columnTemplate, lines] = cols.reduce(
		(acc, c) => addColumnTemplate(c, defaultColumnTemplate, acc),
		["", [] as string[]]
	);
	return columnTemplate + " " + writeLineNames(lines);
}

function getContainerStyles(cols: ColumnDef<any, any>[]): CSSProperties {
	return cols.reduce((acc, c) => {
		const childStyles = c.children
			? getContainerStyles(visibleChildren(c.children))
			: {};
		return mergeCss(mergeCss(acc, c.columnContainerStyles), childStyles);
	}, {} as CSSProperties);
}

function mergeCss(acc: CSSProperties, other?: CSSProperties) {
	if (other) {
		const overrideCounter =
			other.counterReset && acc.counterReset
				? other.counterReset + " " + acc.counterReset
				: "";
		Object.assign(acc, other);
		if (overrideCounter) {
			acc.counterReset = overrideCounter;
		}
	}
	return acc;
}

function writeLineNames(names: string[]) {
	return "[" + names.join(" ") + "]";
}
function addColumnTemplate<T>(
	col: ColumnDef<T, any>,
	defaultColumnTemplate: string,
	[current, lineNames]: [string, string[]]
): [string, string[]] {
	if (col.children) {
		const [nextCurrent, nextLines] = visibleChildren(col.children).reduce(
			(acc, c) => addColumnTemplate(c, defaultColumnTemplate, acc),
			[current, [...lineNames, getColumnLineName(col.id, true)]]
		);
		return [nextCurrent, [...nextLines, getColumnLineName(col.id, false)]];
	}
	const templateString = col.columnTemplate ?? defaultColumnTemplate;
	return [
		current +
			" " +
			writeLineNames([...lineNames, getColumnLineName(col.id, true)]) +
			" " +
			templateString,
		[getColumnLineName(col.id, false)],
	];
}

function rowsForSpan(r: GridItemsSpan) {
	return typeof r === "function" ? 1 : r.rowSpan;
}

function getItemsRenderer(r: GridItemsSpan) {
	return typeof r === "function" ? r : r.render;
}

function visibleChildren<T, D>(c: ChildColumns<T, D>): ColumnDef<any, D>[] {
	return c.columns.filter((x) => !x.hidden);
}
