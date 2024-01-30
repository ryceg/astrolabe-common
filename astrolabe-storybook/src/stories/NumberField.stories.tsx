import { Numberfield } from "@astrolabe/ui/Numberfield";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Numberfield> = {
  component: Numberfield,
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story, params) => {
      const fieldControl = useControl(1);
      return (
        <Story
          args={{
            ...params.args,
            control: fieldControl,
          }}
        />
      );
    },
  ],
  args: {
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
    return <Numberfield {...args} />;
  },
};
