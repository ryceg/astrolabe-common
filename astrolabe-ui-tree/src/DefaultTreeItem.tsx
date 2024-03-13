import { ControlTreeItemProps, useSortableTreeItem } from "./ControlTree";
import { DefaultSortableTreeItem } from "./DefaultSortableTreeItem";

export function DefaultTreeItem(props: ControlTreeItemProps) {
  const sortableProps = useSortableTreeItem(props);

  return <DefaultSortableTreeItem {...sortableProps} />;
}
