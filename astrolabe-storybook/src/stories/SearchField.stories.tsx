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
    className: "",
    widthClass: "",
    placeholder: "Search field",
  },
};

export default meta;
type Story = StoryObj<typeof SearchField>;

export const Primary: Story = {
  render: (args) => {
    const searchField = useControl("");

    return <SearchField {...args} control={searchField} />;
  },
};
