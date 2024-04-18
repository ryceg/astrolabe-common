import {
  ColumnDefInit,
  columnDefinitions,
  DataGrid,
} from "@astroapps/datagrid";
import { CSSProperties, Key, ReactElement } from "react";
import { Control } from "@react-typed-forms/core";
import { Button } from "@astrolabe/ui/Button";

export type ParameterHeaders = "a" | "b";
export interface ParametersColumnData {
  tooltip: React.ReactElement;
  title: string;
  units?: string;
  step?: number;
  digits?: number;
  max?: number;
}

export interface AxleEditForm {}
export interface AxleGroupEditForm {
  axles: AxleEditForm[];
}

export interface VehicleComponentEditForm {
  axleGroups: AxleGroupEditForm[];
}

export interface FlattenedRow {
  components: Control<VehicleComponentEditForm[]>;
  component: Control<VehicleComponentEditForm>;
  componentIndex: number;
  componentSpan: number;
  group: Control<AxleGroupEditForm>;
  groupIndex: number;
  groupSpan: number;
  lastGroup: boolean;
  axle: Control<AxleEditForm>;
  axleIndex: number;
  axleSpan: number;
  lastAxle: boolean;
}

export enum ConstraintType {
  GCW = "GCW",
  TyreSize = "TyreSize",
  TareMass = "TareMass",
  GroupOperatingMass = "GroupOperatingMass",
  AxleOperatingMass = "AxleOperatingMass",
  AxleSpacing = "AxleSpacing",
  Startability = "Startability",
  GradeabilityA = "GradeabilityA",
  TrackingAbilityOnStraightPath = "TrackingAbilityOnStraightPath",
  LowSpeedSweptPath = "LowSpeedSweptPath",
  HighSpeedTransientOfftracking = "HighSpeedTransientOfftracking",
}

export interface VehicleColumnData<T> {
  constraint?: ConstraintType;
  getNumber?: (c: T) => Control<number | null>;
  getBool?: (c: T) => Control<boolean | null>;
  getNumberSelections?: (row: FlattenedRow) => number[];
  columnData: ParametersColumnData;
}

export function parametersColumn<T>(
  id: ParameterHeaders,
  render: (row: T, data: ParametersColumnData) => ReactElement,
  options?: Partial<ColumnDefInit<T, VehicleColumnData<T>>>,
  renderHeader?: (
    id: Key,
    p: ParametersColumnData,
    className: string,
    style?: CSSProperties,
  ) => ReactElement,
  columnData: Partial<VehicleColumnData<T>> = {},
): ColumnDefInit<T, VehicleColumnData<T>> {
  const p: ParametersColumnData = undefined as any;
  return {
    id,
    title: p.title,
    render: (x) => render(x, p),
    renderHeader: (hp) => <></>,
    bodyCellClass: "border-t border-l",
    ...options,
    data: { ...columnData, columnData: p },
  };
}

export interface AdminUserRouteIssue {
  id: string;
  routeId: string;
  note: string;
  createdAt: string;
  travelMode: string;
  comment: string;
}

export function TestThis() {
  const columns = columnDefinitions<AdminUserRouteIssue>(
    {
      title: "#",
      id: "tableRow",
      render: (row, rowIndex, col) => (
        <div className="px-2">{rowIndex + 1}</div>
      ),
      columnTemplate: "min-content",
      cellClass: "justify-center",
      headerCellClass: "justify-center",
    },
    {
      title: "Route Code",
      render: (r) => <div>A BUTTON</div>,
      columnTemplate: "min-content",
    },
    {
      title: "User Note",
      render: (r) => <div className="px-2"> {r.note}</div>,
      columnTemplate: "2fr",
    },
    {
      title: "Date Reported",
      sortField: "createdAt",
      defaultSort: "desc",
      getter: (r) => new Date(r.createdAt).getTime(),
      render: (r) => <div className="px-2">A DATE</div>,
    },
  );
  const rows: AdminUserRouteIssue[] = [
    {
      id: "ok",
      comment: "asd",
      routeId: "asd",
      note: "sadfa",
      createdAt: "sadfsdf",
      travelMode: "sadfsdf",
    },
    {
      id: "ok",
      comment: "asd",
      routeId: "asd",
      note: "sadfa",
      createdAt: "sadfsdf",
      travelMode: "sadfsdf",
    },
  ];
  return (
    <DataGrid
      columns={columns}
      bodyRows={rows.length}
      getBodyRow={(i) => rows[i]}
    />
  );
}
