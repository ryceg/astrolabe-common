import { CircularProgress } from "@astrolabe/ui/CircularProgress";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof CircularProgress> = {
  component: CircularProgress,
  parameters: {
    layout: "centered",
  },
  args: {
    className: "",
    variant: "primary",
    size: "default",
    alignment: "centered",
  },
};

export default meta;
type Story = StoryObj<typeof CircularProgress>;

export const Primary: Story = {
  render: (args) => <CircularProgress {...args} />,
};
