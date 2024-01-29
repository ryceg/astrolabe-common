import { LinearProgress } from "@astrolabe/ui/LinearProgress";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof LinearProgress> = {
  component: LinearProgress,
  parameters: {
    layout: "centered",
  },
  args: {
    variant: "indeterminate",
    className: "",
  },
  argTypes: {
    value: {
      control: "number",
      min: 0,
      max: 100,
    },
  },
};

export default meta;
type Story = StoryObj<typeof LinearProgress>;

export const Primary: Story = {
  render: (args) => {
    return (
      <div className="w-[500px] px-4">
        <LinearProgress {...args} />
      </div>
    );
  },
};
