import { CloseButton } from "@astrolabe/ui/CloseButton";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof CloseButton> = {
  component: CloseButton,
  parameters: {
    layout: "centered",
  },
  args: {
    className: "text-surface-950",
    onClick: () => {},
  },
};

export default meta;
type Story = StoryObj<typeof CloseButton>;

export const Primary: Story = {
  render: (args) => {
    return <CloseButton {...args} />;
  },
};
