import { SearchField } from "@astrolabe/ui/SeachField";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof SearchField> = {
  component: SearchField,
  parameters: {
    layout: "centered",
  },
  args: {
    control: undefined,
    className: "text-surface-950",
    widthClass: "w-full",
    placeholder: "Search field",
  },
  decorators: [
    (Story, params) => {
      const control = useControl("");
      return <Story args={{ ...params.args, control: control }} />;
    },
  ],
};

export default meta;
type Story = StoryObj<typeof SearchField>;

export const Primary: Story = {
  render: (args) => {
    return <SearchField {...args} />;
  },
};
