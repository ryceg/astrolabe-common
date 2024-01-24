import { Switch } from "@astrolabe/ui/Switch";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Switch> = {
  component: Switch,
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof Switch>;

export const Primary: Story = {
  args: {
    // control
    variant: "primary",
    size: "md",
    disabled: false,
  },
  render: (args) => {
    const control = useControl(false);

    return (
      <label className="flex gap-2 py-2">
        <Switch {...args} control={control} />
        <span> Hide this page node</span>
      </label>
    );
  },
};
