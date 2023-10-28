import {
  Control,
  groupedChanges,
  updateElements,
  useControl,
  useControlEffect,
  useValueChangeEffect,
} from "@react-typed-forms/core";
import React, {
  CSSProperties,
  FC,
  HTMLAttributes,
  ReactElement,
  ReactNode,
} from "react";
import {
  closestCenter,
  defaultDropAnimation,
  DndContext,
  DragEndEvent,
  DragMoveEvent,
  DragOverEvent,
  DragOverlay,
  DragStartEvent,
  DropAnimation,
  MeasuringStrategy,
  Modifier,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import { CSS } from "@dnd-kit/utilities";
import {
  AnimateLayoutChanges,
  SortableContext,
  useSortable,
  UseSortableArguments,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { setIncluded } from "@astrolabe/client/util/arrays";
import update from "immutability-helper";
import {
  ControlTreeNode,
  findAllTreeParentsInArray,
  toTreeNode,
  TreeDragState,
  TreeInsertState,
  TreeState,
} from "./index";

export interface ControlTreeItemProps {
  node: ControlTreeNode;
  clone?: boolean;
  selected: Control<Control<any> | undefined>;
  title: string;
  indentationWidth: number;
  indicator: boolean;
  insertState: Control<TreeInsertState | undefined>;
  active: Control<any>;
  onCollapse?: () => void;
  actions?: ReactNode;
  onRemove?: () => void;
}

export function ControlTree<V>({
  controls,
  canDropAtRoot,
  indentationWidth = 50,
  indicator = true,
  treeState,
  TreeItem,
}: {
  treeState: Control<TreeState>;
  controls: Control<V[]>;
  canDropAtRoot: (c: Control<any>) => boolean;
  indentationWidth?: number;
  indicator?: boolean;
  TreeItem: FC<ControlTreeItemProps>;
}) {
  const sensors = useSensors(useSensor(PointerSensor));
  const dragState = useControl<TreeDragState>({ offsetLeft: 0 });
  const dragFields = dragState.fields;
  const { active } = dragFields;
  const treeFields = treeState.fields;

  const expanded = treeFields.expanded.value;

  const currentActive = active.value;
  const rootItems = controls.elements.map(
    toTreeNode(
      expanded,
      currentActive,
      {
        childIndex: 0,
        expanded: true,
        childrenNodes: [],
        render: () => {
          throw "";
        },
        children: controls,
        control: controls,
        canDropChild: canDropAtRoot,
        indent: -1,
        parent: undefined,
      },
      0,
    ),
  );
  const flatten: (x: ControlTreeNode) => ControlTreeNode[] = (x) => [
    x,
    ...x.childrenNodes.flatMap(flatten),
  ];
  const treeItems = rootItems.flatMap(flatten);
  const activeIndex = currentActive
    ? treeItems.findIndex((x) => x.control === currentActive)
    : -1;

  useValueChangeEffect(treeFields.selected, (sel) => {
    const nodeIds =
      (sel &&
        findAllTreeParentsInArray(sel, controls).map((x) => x.uniqueId)) ??
      [];
    const expanded = treeFields.expanded.current.value;
    const unExpandedNodes = nodeIds.filter((x) => !expanded.includes(x));
    if (unExpandedNodes.length)
      treeFields.expanded.setValue((x) => [...x, ...unExpandedNodes]);
  });

  useControlEffect(
    () => {
      const overId = dragFields.overId.value;
      const offsetLeft = dragFields.offsetLeft.value;

      if (activeIndex >= 0 && overId != null) {
        const overIndex = treeItems.findIndex(
          (x) => x.control.uniqueId === overId,
        );
        const indexAdjust = overIndex > activeIndex ? 0 : 1;
        const prevItem = treeItems[overIndex - indexAdjust];
        const draggedItem = treeItems[activeIndex];
        const mouseIndent = offsetLeft + draggedItem.indent;
        const overItem = treeItems[overIndex];

        let closestDrop: [number, ControlTreeNode, number] | undefined;

        const checkDrop = (childIndex: number, parent: ControlTreeNode) => {
          const diff = Math.abs((parent.indent ?? 0) + 1 - mouseIndent);
          if (!closestDrop || closestDrop[2] > diff) {
            closestDrop = [childIndex, parent, diff];
          }
        };

        const draggedControl = draggedItem.control;
        let overParent = overItem.parent;
        if (prevItem) {
          const prevParent = prevItem.parent;
          if (prevItem.canDropChild(draggedControl)) {
            checkDrop(0, prevItem);
          }
          if (
            prevParent &&
            prevParent.canDropChild(draggedControl) &&
            (nodeChildCount(prevItem) === 0 || !prevItem.expanded)
          ) {
            checkDrop(prevItem.childIndex + indexAdjust, prevParent);
          }
        } else {
          if (overParent && overParent.canDropChild(draggedControl)) {
            checkDrop(0, overParent);
          }
        }
        if (
          overParent &&
          nodeChildCount(overParent) === overItem.childIndex + 1
        ) {
          while (overParent) {
            if (overParent.parent?.canDropChild(draggedControl))
              checkDrop(overParent.childIndex + 1, overParent.parent);
            overParent = overParent.parent;
          }
        }
        return closestDrop
          ? {
              childIndex: closestDrop[0],
              parent: closestDrop[1],
              dragged: draggedItem,
            }
          : undefined;
      }
      return undefined;
    },
    (v) => (treeFields.dragInsert.value = v),
  );
  const activeFlat = activeIndex != null ? treeItems[activeIndex] : undefined;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleStart}
      onDragEnd={handleEnd}
      onDragOver={handleDragOver}
      onDragMove={handleDragMove}
      measuring={measuring}
    >
      <SortableContext
        items={treeItems.map((x) => x.control.uniqueId)}
        strategy={verticalListSortingStrategy}
      >
        {rootItems.map((x) => renderTreeNode(x))}
      </SortableContext>
      <DragOverlay
        dropAnimation={dropAnimationConfig}
        modifiers={indicator ? [adjustTranslate] : undefined}
      >
        {activeFlat ? renderTreeNode(activeFlat, true) : null}
      </DragOverlay>
    </DndContext>
  );

  function onCollapse(x: ControlTreeNode) {
    treeFields.expanded.setValue((ex) =>
      setIncluded(ex, x.control.uniqueId, !x.expanded),
    );
  }

  function renderTreeNode(x: ControlTreeNode, clone?: boolean): ReactElement {
    const treeFields = treeState.fields;
    const hasChildren = nodeChildCount(x) > 0;
    return x.render({
      renderItem: (title, actions, onRemove) => (
        <TreeItem
          key={x.control.uniqueId}
          node={x}
          clone={clone}
          selected={treeFields.selected}
          title={title ?? ""}
          indentationWidth={indentationWidth}
          indicator={indicator}
          insertState={treeFields.dragInsert}
          active={dragState.fields.active}
          onCollapse={hasChildren ? () => onCollapse(x) : undefined}
          actions={actions}
          onRemove={onRemove}
        />
      ),
      children: clone ? [] : x.childrenNodes.map((c) => renderTreeNode(c)),
    });
  }

  function handleStart(e: DragStartEvent) {
    dragState.value = {
      active: e.active.data.current?.control,
      offsetLeft: 0,
      overId: e.active.id as number,
    };
    document.body.style.setProperty("cursor", "grabbing");
  }

  function handleEnd(e: DragEndEvent) {
    const treeFields = treeState.fields;
    groupedChanges(() => {
      const insertedAt = treeFields.dragInsert.value;
      if (insertedAt) {
        const dragParentChildren = insertedAt.dragged.parent?.children;
        const destParentChildren = insertedAt.parent?.children;
        const draggedControl = insertedAt.dragged.control;
        if (dragParentChildren && destParentChildren) {
          const destIndex = insertedAt.childIndex;
          const currentIndex =
            dragParentChildren.elements.indexOf(draggedControl);
          if (destParentChildren === dragParentChildren) {
            if (currentIndex !== destIndex) {
              updateElements(dragParentChildren, (childList) =>
                update(childList, {
                  $splice: [
                    [currentIndex, 1],
                    [destIndex, 0, draggedControl],
                  ],
                }),
              );
            }
          } else {
            updateElements(dragParentChildren, (childList) =>
              childList.filter((x) => x !== draggedControl),
            );
            updateElements(destParentChildren, (childList) =>
              update(childList, {
                $splice: [[destIndex, 0, draggedControl]],
              }),
            );
          }
        }
      }
      dragState.value = {
        offsetLeft: 0,
        overId: undefined,
        active: undefined,
      };
      treeFields.dragInsert.value = undefined;
    });
    document.body.style.setProperty("cursor", "");
  }
  function handleDragOver({ over }: DragOverEvent) {
    dragState.fields.overId.value = over?.id as number;
  }
  function handleDragMove({ delta }: DragMoveEvent) {
    dragState.fields.offsetLeft.value = Math.round(delta.x / indentationWidth);
  }
}

// interface ControlTreeItemProps {
//   title: string;
//   icon: string | ReactElement;
//   collapsed: boolean;
//   childCount?: number;
//   clone?: boolean;
//   disableInteraction?: boolean;
//   disableSelection?: boolean;
//   ghost?: boolean;
//   handleProps?: any;
//   indicator?: boolean;
//   disableDrag?: boolean;
//   indentationWidth: number;
//   depth: number;
//   canHaveChildren: boolean;
//   onCollapse?(): void;
//   onRemove?(): void;
//   wrapperRef?(node: HTMLLIElement): void;
//   style: CSSProperties;
//   onEditTitle?: (title: string) => void;
//   selected: boolean;
//   onSelect: () => void;
//   actions?: ReactNode;
//   classes: ControlClasses;
//   renderDragHandle: (props: any) => ReactElement;
//   noHandle: ReactNode;
// }
//
// const ControlTreeItem = forwardRef<HTMLDivElement, ControlTreeItemProps>(
//   (
//     {
//       icon,
//       title,
//       childCount,
//       clone,
//       disableSelection,
//       disableInteraction,
//       ghost,
//       handleProps,
//       indicator,
//       collapsed,
//       onCollapse,
//       onRemove,
//       style,
//       wrapperRef,
//       indentationWidth,
//       depth,
//       onEditTitle,
//       onSelect,
//       selected,
//       disableDrag,
//       actions,
//       canHaveChildren,
//       classes: styles,
//       renderDragHandle,
//       noHandle,
//     },
//     ref,
//   ) => {
//     const titleEditing = useControl({ editing: false, title });
//     const { editing, title: titleControl } = titleEditing.fields;
//
//     function setTitleEditing(b: boolean) {
//       if (b) {
//         titleControl.value = title;
//       } else {
//         onEditTitle?.(titleControl.current.value);
//       }
//       editing.value = b;
//     }
//     return (
//       <li
//         className={clsx(
//           styles.Wrapper,
//           clone && styles.clone,
//           ghost && styles.ghost,
//           indicator && styles.indicator,
//           disableSelection && styles.disableSelection,
//           disableInteraction && styles.disableInteraction,
//           selected && styles.selected,
//         )}
//         ref={wrapperRef}
//         style={
//           {
//             "--spacing": `${indentationWidth * depth}px`,
//           } as React.CSSProperties
//         }
//       >
//         <div
//           className={styles.TreeItem}
//           ref={ref}
//           style={style}
//           onClick={onSelect}
//         >
//           {!disableDrag ? renderDragHandle(handleProps) : noHandle}
//           {(onCollapse || canHaveChildren) && (
//             <IconButton
//               size="small"
//               onClick={(e) => {
//                 e.stopPropagation();
//                 onCollapse?.();
//               }}
//               className={clsx(styles.Collapse, collapsed && styles.collapsed)}
//               sx={{ visibility: !onCollapse ? "hidden" : undefined }}
//             >
//               <ExpandMoreIcon />
//             </IconButton>
//           )}
//           {typeof icon === "string" ? <Icon>{icon}</Icon> : icon}
//           <RenderControl
//             children={() =>
//               editing.value ? (
//                 <ClickAwayListener onClickAway={() => setTitleEditing(false)}>
//                   <div style={{ flexGrow: 1 }}>
//                     <FTextField
//                       state={titleEditing.fields.title}
//                       size={"small"}
//                       onFocus={(e) => e.target.select()}
//                       focused
//                       style={{ height: "100%", backgroundColor: "#fff" }}
//                       onKeyPress={(e) => {
//                         if (e.key === "Enter") setTitleEditing(false);
//                       }}
//                     />
//                   </div>
//                 </ClickAwayListener>
//               ) : (
//                 <span
//                   className={styles.Text}
//                   onDoubleClick={
//                     onEditTitle ? () => setTitleEditing(true) : undefined
//                   }
//                 >
//                   {title}
//                 </span>
//               )
//             }
//           />
//           {!clone && onRemove && (
//             <Tooltip title={"Remove"}>
//               <IconButton
//                 onClick={(e) => {
//                   e.stopPropagation();
//                   confirmation(
//                     () => onRemove(),
//                     `Are you sure you want to remove "${title}"`,
//                     "Confirm Delete",
//                   );
//                 }}
//               >
//                 <Delete className={"hoverButtons"} />
//               </IconButton>
//             </Tooltip>
//           )}
//           {clone && childCount && childCount > 1 ? (
//             <span className={styles.Count}>{childCount}</span>
//           ) : null}
//           {actions}
//         </div>
//       </li>
//     );
//   },
// );
//
// const measuring = {
//   droppable: {
//     // strategy: MeasuringStrategy.Always,
//     strategy: MeasuringStrategy.WhileDragging,
//     frequency: 1000,
//   },
// };
//
const animateLayoutChanges: AnimateLayoutChanges = ({
  isSorting,
  wasDragging,
}) => !(isSorting || wasDragging);

// export function SortableTreeItem({
//   node,
//   clone,
//   insertState,
//   active,
//   selected,
//   onRemove,
//   ...props
// }: Pick<
//   ControlTreeItemProps,
//   | "onCollapse"
//   | "clone"
//   | "indentationWidth"
//   | "indicator"
//   | "title"
//   | "actions"
//   | "onRemove"
// > & {
//   node: ControlTreeNode;
//   selected: Control<Control<any> | undefined>;
//   insertState: Control<TreeInsertState | undefined>;
//   active: Control<Control<any> | undefined>;
// }) {
//   const {
//     attributes,
//     isDragging,
//     isSorting,
//     listeners,
//     setDraggableNodeRef,
//     setDroppableNodeRef,
//     transform,
//     transition,
//   } = useSortable({
//     id: node.control.uniqueId,
//     data: { control: node.control },
//     animateLayoutChanges,
//   });
//   const style: CSSProperties = {
//     transform: CSS.Translate.toString(transform),
//     transition,
//   };
//   const isSelected = useIsSelected(selected, node.control);
//   const itemRef = useScrollIntoView<HTMLLIElement>(isSelected);
//   return (
//     <RenderControl
//       children={() => (
//         <ControlTreeItem
//           depth={
//             active.value === node.control && insertState.fields
//               ? insertState.fields.parent.value.indent + 1
//               : node.indent
//           }
//           collapsed={!node.expanded}
//           ref={setDraggableNodeRef}
//           wrapperRef={(r) => {
//             setDroppableNodeRef(r);
//             itemRef.current = r;
//           }}
//           style={style}
//           ghost={isDragging}
//           childCount={
//             clone && nodeCanHaveChildren(node)
//               ? nodeChildCount(node) + 1
//               : undefined
//           }
//           icon={node.icon ?? "tune"}
//           clone={clone}
//           disableSelection={false}
//           disableInteraction={isSorting}
//           onEditTitle={node.updateTitle}
//           selected={!clone && isSelected}
//           canHaveChildren={nodeCanHaveChildren(node)}
//           onSelect={() => (selected.value = node.control)}
//           onRemove={
//             onRemove ??
//             (!node.dragEnabled
//               ? undefined
//               : () => {
//                   groupedChanges(() => {
//                     const siblings = node.parent?.children;
//                     siblings && removeElement(siblings, node.control);
//                     if (selected.value === node.control) {
//                       selected.value = undefined;
//                     }
//                   });
//                 })
//           }
//           disableDrag={!node.dragEnabled}
//           handleProps={{
//             ...attributes,
//             ...listeners,
//           }}
//           {...props}
//         />
//       )}
//     />
//   );
// }

const adjustTranslate: Modifier = ({ transform }) => {
  return {
    ...transform,
    y: transform.y - 25,
  };
};

const dropAnimationConfig: DropAnimation = {
  keyframes({ transform }) {
    return [
      { opacity: 1, transform: CSS.Transform.toString(transform.initial) },
      {
        opacity: 0,
        transform: CSS.Transform.toString({
          ...transform.final,
          x: transform.final.x + 5,
          y: transform.final.y + 5,
        }),
      },
    ];
  },
  easing: "ease-out",
  sideEffects({ active }) {
    active.node.animate([{ opacity: 0 }, { opacity: 1 }], {
      duration: defaultDropAnimation.duration,
      easing: defaultDropAnimation.easing,
    });
  },
};

function nodeChildCount(f: ControlTreeNode | undefined) {
  const children = f?.children?.current.value;
  return children ? children.length : 0;
}

const measuring = {
  droppable: {
    // strategy: MeasuringStrategy.Always,
    strategy: MeasuringStrategy.WhileDragging,
    frequency: 1000,
  },
};

export interface SortableTreeItem {
  handleProps: HTMLAttributes<HTMLElement>;
  itemProps: {
    style: CSSProperties;
    onClick: () => void;
  };
  paddingLeft: number;
  isDragging: boolean;
  setDraggableNodeRef: (elem: HTMLElement | null) => void;
  setDroppableNodeRef: (elem: HTMLElement | null) => void;
  canHaveChildren: boolean;
}
export function useSortableTreeItem({
  node,
  active,
  insertState,
  clone,
  selected,
  indentationWidth,
}: ControlTreeItemProps): SortableTreeItem {
  const {
    transform,
    transition,
    attributes,
    listeners,
    isDragging,
    setDraggableNodeRef,
    setDroppableNodeRef,
  } = useSortable({
    id: node.control.uniqueId,
    data: { control: node.control },
    animateLayoutChanges,
  });

  const style: CSSProperties = {
    transform: CSS.Translate.toString(transform),
    transition,
  };

  const depth =
    active.value === node.control && insertState.fields
      ? insertState.fields.parent.value.indent + 1
      : node.indent;

  const canHaveChildren = !!node.children;

  return {
    handleProps: { ...attributes, ...listeners },
    itemProps: { style, onClick: () => (selected.value = node.control) },
    canHaveChildren,
    setDraggableNodeRef,
    setDroppableNodeRef,
    isDragging,
    paddingLeft: clone ? 0 : depth * indentationWidth,
  };
}
