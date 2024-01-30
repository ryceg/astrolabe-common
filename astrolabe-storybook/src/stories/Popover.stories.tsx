import { Popover } from "@astrolabe/ui/Popover";
import { Meta, StoryObj } from "@storybook/react";
import { Button } from "@astrolabe/ui/Button";
import { useArgs } from "@storybook/preview-api";

const meta: Meta<typeof Popover> = {
  component: Popover,
  parameters: {
    layout: "centered",
  },
  args: {
    content: <div>Popover content</div>,
    className: "m-4",
    side: "top",
    open: false,
    onOpenChange: (c) => {},
    triggerClass: "",
    children: (
      <Button variant="primary" size="default">
        Popover
      </Button>
    ),
  },
};

export default meta;
type Story = StoryObj<typeof Popover>;

export const Primary: Story = {
  render: (args) => {
    const [{ open }, updateArgs] = useArgs();

    return (
      <Popover
        {...args}
        open={open}
        onOpenChange={(v) => {
          updateArgs({ open: v });
        }}
      >
        {args.children}
      </Popover>
    );
  },
};
