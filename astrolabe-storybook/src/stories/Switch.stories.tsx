import { Switch } from "@astrolabe/ui/Switch";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Switch> = {
  component: Switch,
  decorators: [
    (Story, params) => {
      const control = useControl(true);
      return <Story args={{ ...params.args, control: control }} />;
    },
  ],
  args: {
    size: "md",
    disabled: false,
  },
  parameters: {
    layout: "centered",
  },
};

export default meta;
type Story = StoryObj<typeof Switch>;

export const Primary: Story = {
  args: {
    variant: "primary",
  },
  render: (args) => {
    return (
      <label className="flex gap-2 py-2 items-center">
        <Switch {...args} />
        <span className="text-surface-950">Primary Switch</span>
      </label>
    );
  },
};

export const Secondary: Story = {
  args: {
    variant: "secondary",
  },
  render: (args) => {
    return (
      <label className="flex gap-2 py-2 items-center">
        <Switch {...args} />
        <span className="text-surface-950">Secondary Switch</span>
      </label>
    );
  },
};
