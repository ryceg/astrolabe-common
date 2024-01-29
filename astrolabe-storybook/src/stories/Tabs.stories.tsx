import { Tabs } from "@astrolabe/ui/Tabs";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof Tabs> = {
  component: Tabs,
  args: {
    tabs: [
      {
        id: "1",
        title: <div>Tab title 1</div>,
        content: <div>Tab content 1</div>,
      },
      {
        id: "2",
        title: <div>Tab title 2</div>,
        content: <div>Tab content 2</div>,
      },
    ],
    contentClass: "text-surface-950",
  },
};

export default meta;
type Story = StoryObj<typeof Tabs>;

export const Primary: Story = {
  args: {
    color: "primary",
  },
  render: (args) => {
    const tabsControl = useControl("1");
    return <Tabs {...args} control={tabsControl} />;
  },
};

export const Secondary: Story = {
  args: {
    color: "secondary",
  },
  render: (args) => {
    const tabsControl = useControl("2");
    return <Tabs {...args} control={tabsControl} />;
  },
};
