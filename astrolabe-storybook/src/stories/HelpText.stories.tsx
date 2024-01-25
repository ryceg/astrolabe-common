import { HelpText } from "@astrolabe/ui/HelpText";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof HelpText> = {
  component: HelpText,
  parameters: {
    layout: "centered",
  },
  args: {
    children: <span>Help text content</span>,
    className: "",
    side: "top",
    iconClass: "",
  },
};

export default meta;
type Story = StoryObj<typeof HelpText>;

export const Primary: Story = {
  render: (args) => {
    return <HelpText side={args.side}>{args.children}</HelpText>;
  },
};
