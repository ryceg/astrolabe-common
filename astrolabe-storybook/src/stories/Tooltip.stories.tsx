import { Tooltip } from "@astrolabe/ui/Tooltip";
import { Meta, StoryObj } from "@storybook/react";
import { Button } from "@astrolabe/ui/Button";

const meta: Meta<typeof Tooltip> = {
  component: Tooltip,
  // parameters: {
  //   layout: "centered",
  // },
  args: {
    children: (
      <Button variant="primary" size="default">
        Tooltip
      </Button>
    ),
    content: <div>Tooltip content</div>,
    sideOffset: 4,
    contentClass: "",
    triggerClass: "",
    open: false,
    onOpenChange: (c) => {},
    asChild: false,
  },
};

export default meta;
type Story = StoryObj<typeof Tooltip>;

export const DefaultTooltip: Story = {
  args: {
    variant: "default",
  },
  render: (args) => {
    return <Tooltip {...args}>{args.children}</Tooltip>;
  },
};

export const DangerTooltip: Story = {
  args: {
    variant: "danger",
  },
  render: (args) => {
    return <Tooltip {...args}>{args.children}</Tooltip>;
  },
};
