import { Numberfield } from "@astrolabe/ui/Numberfield";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Numberfield> = {
  component: Numberfield,
  parameters: {
    layout: "centered",
  },
  args: {
    control: undefined,
    required: false,
    label: "Number field",
    inputClass: "",
    className: "text-surface-950",
  },
};

export default meta;
type Story = StoryObj<typeof Numberfield>;

export const Primary: Story = {
  render: (args) => {
    const fieldControl = useControl(1);

    return (
      <Numberfield
        {...args}
        className={args.className}
        inputClass={args.inputClass}
        control={fieldControl}
        label={args.label}
        required={args.required}
      />
    );
  },
};
