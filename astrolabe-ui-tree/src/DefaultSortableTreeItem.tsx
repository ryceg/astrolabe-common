import clsx from "clsx";
import { SortableTreeItem } from "./ControlTree";

export function DefaultSortableTreeItem({
  clone,
  isDragging,
  setDraggableNodeRef,
  setDroppableNodeRef,
  paddingLeft,
  isSelected,
  itemProps,
  handleProps,
  canHaveChildren,
  onCollapse,
  expanded,
  title,
  actions,
  handleIcon,
}: SortableTreeItem) {
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
          {handleIcon || <i className="fa fa-grip-vertical cursor-grabbing" />}
        </div>
        {canHaveChildren && (
          <div
            onClick={() => onCollapse?.()}
            className={clsx("transition-transform", expanded && "rotate-90")}
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
