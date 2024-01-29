import {Control} from "@react-typed-forms/core";
import React, {useRef} from "react";
import {ReactQuillProps} from "react-quill";
import {createDataRendererLabelled, DataRenderType,} from "@react-typed-forms/schemas";

export function createQuillEditor(
  ReactQuill: React.ComponentType<ReactQuillProps>,
) {
  return createDataRendererLabelled(
    ({ control }) => <HtmlEditor state={control} ReactQuill={ReactQuill} />,
    { renderType: DataRenderType.HtmlEditor },
  );
}

export function HtmlEditor({
  state,
  ReactQuill,
  className,
}: {
  className?: string;
  state: Control<string | null | undefined>;
  ReactQuill: React.ComponentType<ReactQuillProps>;
}) {
  const stateRef = useRef(state);
  return (
    <div>
      {state.disabled ? (
        <div
          className="ql-container ql-snow ql-editor"
          dangerouslySetInnerHTML={{ __html: state.value || "" }}
        />
      ) : (
        <ReactQuill
          modules={{
            toolbar: {
              container: [
                ["bold", "italic", "underline", "strike"], // toggled buttons
                ["blockquote", "code-block"],

                [{ header: 1 }, { header: 2 }],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ script: "sub" }, { script: "super" }],
                [{ indent: "-1" }, { indent: "+1" }],
                [{ direction: "rtl" }],

                [{ size: ["small", false, "large", "huge"] }],
                [{ header: [1, 2, 3, 4, 5, 6, false] }],
                ["image"],
                ["link"],
                [{ color: [] }, { background: [] }],
                [{ font: [] }],
                [{ align: [] }],
              ],
            },
          }}
          theme="snow"
          value={state.value || ""}
          onChange={(v) => {
            stateRef.current.value = v;
          }}
          className={className}
        />
      )}
    </div>
  );
}
