import React, { useEffect, useState } from "react";
import { useDndContext } from "@dnd-kit/core";

export function TreeDebug() {
  const [reload, setReload] = useState(0);
  const ctx = useDndContext();
  // console.log(Array.from(ctx.droppableRects.values()));
  useEffect(() => {
    setInterval(() => {
      setReload((x) => x + 1);
    }, 1000);
  }, []);
  return (
    <>
      {Array.from(ctx.droppableRects.entries()).map(([k, r]) => (
        <div
          style={{
            position: "fixed",
            top: r.top,
            left: r.left,
            width: r.width,
            height: r.height,
            borderColor: "green",
            opacity: 1,
            border: "1px solid",
          }}
        >
          <div style={{ position: "absolute", right: "0px" }}>{k}</div>
        </div>
      ))}
    </>
  );
}
