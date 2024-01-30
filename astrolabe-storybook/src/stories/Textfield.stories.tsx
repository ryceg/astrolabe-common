import { Textfield } from "@astrolabe/ui/Textfield";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Textfield> = {
  component: Textfield,
  parameters: {
    layout: "centered",
  },
  args: {
    control: undefined,
    label: "Textfield Label",
    required: false,
    className: "text-surface-950 ",
    inputClass: "border-surface-950 border-2",
  },
};

export default meta;
type Story = StoryObj<typeof Textfield>;

export const Primary: Story = {
  render: (args) => {
    const textField = useControl("");
    return <Textfield {...args} control={textField} />;
  },
};
