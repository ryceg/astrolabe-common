import { RadioButton } from "@astrolabe/ui/RadioButton";
import { Meta, StoryObj } from "@storybook/react";
import { useControl, useControlEffect } from "@react-typed-forms/core";

const meta: Meta<typeof RadioButton> = {
  component: RadioButton,
  parameters: {
    layout: "centered",
  },
};

export default meta;
type Story = StoryObj<typeof RadioButton>;

export const Primary: Story = {
  render: ({}) => {
    const radioButtonControl = useControl(0);
    return (
      <div className="flex justify-center gap-2">
        <RadioButton
          control={radioButtonControl}
          value={0}
          disabled={false}
          className=""
          isNumber
        />
        <span className="text-primary-900">Radio Button</span>
      </div>
    );
  },
};

export const RadioButtonGroup: Story = {
  render: ({}) => {
    const radioButtonControl = useControl(1);

    return (
      <div className="flex flex-col gap-2">
        {Array.from({ length: 4 }).map((r, i) => (
          <label className="flex gap-2 justify-center">
            <RadioButton
              control={radioButtonControl}
              value={i + 1}
              isNumber
              disabled={i === 2}
              key={i}
            />
            <span className="text-primary-900">Radio button {i + 1}</span>
          </label>
        ))}
      </div>
    );
  },
};
