import { ControlTreeItemProps, useSortableTreeItem } from "./ControlTree";
import { clsx } from "clsx";

export function DefaultTreeItem(props: ControlTreeItemProps) {
  const { title, indentationWidth, clone, actions, onCollapse, node } = props;
  const {
    itemProps,
    handleProps,
    setDroppableNodeRef,
    setDraggableNodeRef,
    paddingLeft,
    canHaveChildren,
    isDragging,
    isSelected,
  } = useSortableTreeItem(props);

  return (
    <div
      className={clsx(
        clone && "inline-block",
        isDragging && "opacity-50",
        "cursor-pointer",
      )}
      ref={setDroppableNodeRef}
      style={{ paddingLeft }}
    >
      <div
        className={clsx(
          "flex gap-2 relative border items-center",
          isSelected && "bg-surface-200",
        )}
        {...itemProps}
        ref={setDraggableNodeRef}
      >
        <div {...handleProps}>
          <i className="fa fa-grip-vertical cursor-grabbing" />
        </div>
        {canHaveChildren && (
          <div
            onClick={() => onCollapse?.()}
            className={clsx(
              "transition-transform",
              node.expanded && "rotate-90",
            )}
          >
            <i className="fa fa-chevron-right" />
          </div>
        )}
        <div className="grow-1 truncate">{title}</div>
        {actions}
      </div>
    </div>
  );
}
