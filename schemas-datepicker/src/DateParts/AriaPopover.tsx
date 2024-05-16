import type { AriaPopoverProps } from "react-aria";
import { DismissButton, Overlay, usePopover } from "react-aria";
import type { OverlayTriggerState } from "react-stately";
import React, { useRef } from "react";

interface PopoverProps extends Omit<AriaPopoverProps, "popoverRef"> {
  children: React.ReactNode;
  state: OverlayTriggerState;
}

export function AriaPopover({ children, state, ...props }: PopoverProps) {
  let popoverRef = useRef(null);
  let { popoverProps, underlayProps } = usePopover(
    {
      ...props,
      popoverRef,
    },
    state,
  );

  return (
    <Overlay>
      <div {...underlayProps} style={{ position: "fixed", inset: 0 }} />
      <div
        {...popoverProps}
        ref={popoverRef}
        className="bg-white"
        style={{
          ...popoverProps.style,
          border: "1px solid gray",
        }}
      >
        <DismissButton onDismiss={state.close} />
        {children}
        <DismissButton onDismiss={state.close} />
      </div>
    </Overlay>
  );
}
