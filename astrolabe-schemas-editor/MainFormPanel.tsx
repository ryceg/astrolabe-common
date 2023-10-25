import { LayoutGroup, motion } from "framer-motion";
import { Box, Button, Container, Paper } from "@mui/material";
import { ControlDragState, FormControlPreview } from "./FormControlPreview";
import React from "react";
import {
  ControlDefinitionForm,
  defaultControlDefinitionForm,
  SchemaFieldForm,
} from "./schemaSchemas";
import { ControlDefinitionType } from "@react-typed-forms/schemas";
import { addElement, Control, RenderElements } from "@react-typed-forms/core";
import { DragData, DropData } from "./dragndrop";

export function MainFormPanel({
  controls,
  singlePage,
  schemaFields,
  selected,
  controlDragState,
  dropSuccess,
  readonly,
}: {
  controls: Control<ControlDefinitionForm[]>;
  singlePage?: boolean;
  schemaFields: Control<SchemaFieldForm[]>;
  selected: Control<Control<any> | undefined>;
  controlDragState: Control<ControlDragState | undefined>;
  dropSuccess: (drag: DragData, drop: DropData) => void;
  readonly?: boolean;
}) {
  return (
    <motion.div
      layoutScroll
      style={{ overflow: "auto", width: "100%", height: "100%" }}
    >
      <Container maxWidth="lg">
        <LayoutGroup>
          <RenderElements
            control={controls}
            container={(children) => (
              <Box {...(singlePage ? { component: Paper, p: 2 } : {})}>
                {children}
              </Box>
            )}
          >
            {(n, i) => (
              <Box
                {...(singlePage
                  ? { my: 2 }
                  : { my: 2, p: 2, component: Paper })}
              >
                <FormControlPreview
                  context={{
                    item: n,
                    treeDrag: controlDragState,
                    fields: schemaFields,
                    selected: selected,
                    dropIndex: i,
                    dropSuccess,
                    readonly,
                  }}
                />
              </Box>
            )}
          </RenderElements>
        </LayoutGroup>
        <Button
          variant="contained"
          onClick={() =>
            addElement(controls, {
              ...defaultControlDefinitionForm,
              type: ControlDefinitionType.Group,
            })
          }
        >
          Add Page
        </Button>
      </Container>
    </motion.div>
  );
}
