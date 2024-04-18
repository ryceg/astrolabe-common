import type { MetaFunction } from "@remix-run/node";
import React from "react";
import { columnDefinitions, DataGrid, mapColumns } from "@astroapps/datagrid";
import {
  Control,
  RenderControl,
  useComputed,
  useControl,
  useControlValue,
} from "@react-typed-forms/core";
import { TestThis } from "~/routes/test";

export const meta: MetaFunction = () => {
  return [
    { title: "New Remix App" },
    { name: "description", content: "Welcome to Remix!" },
  ];
};

interface MyRow {
  la: string;
  dida: number;
}

export default function Index() {
  const data = useControl<MyRow[]>([
    { la: "did", dida: 1 },
    { la: "dada", dida: 3 },
  ]);
  const unwrappedColumns = columnDefinitions<MyRow>(
    { title: "LA", getter: (x) => x.la },
    { title: "Dida", getter: (x) => x.dida },
  );
  const columns = columnDefinitions<Control<MyRow>>({
    id: "Group",
    children: mapColumns(unwrappedColumns, (x) => x.value),
  });
  const bodyRows = useControlValue(useComputed(() => data.elements.length));
  return (
    <div style={{ fontFamily: "system-ui, sans-serif", lineHeight: "1.8" }}>
      <DataGrid
        style={{ background: "green" }}
        bodyRows={bodyRows}
        getBodyRow={(i) => data.current.elements[i]}
        columns={columns}
        extraHeaderRows={[<div key={1}>ok</div>, <div key={2}>what</div>]}
        renderExtraRows={(rowNum) => (
          <>
            <div style={{ gridRow: rowNum }}>END1</div>
            <div style={{ gridRow: rowNum + 1 }}>END2</div>
          </>
        )}
        wrapBodyContent={(r) => <RenderControl render={r} />}
        // wrapBodyRow={(i, render) => {
        //     const elem = data.current.elements[i];
        //     return <RenderControl key={elem.uniqueId} render={() => render(elem, elem.uniqueId)}/>
        //   }
        // }
      />
      <button
        onClick={() =>
          (data.value = [
            { la: "Changed", dida: 4 },
            { la: "Don't", dida: 5 },
            { la: "Extra", dida: 2 },
          ])
        }
      >
        Edit
      </button>
      <TestThis />
    </div>
  );
}
