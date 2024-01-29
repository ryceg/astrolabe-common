import { NavList } from "@astrolabe/ui/NavLink";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof NavList> = {
  component: NavList,
};

export default meta;
type Story = StoryObj<typeof NavList>;

export const Primary: Story = {
  render: () => {
    return <NavList links={[{ label: "Link 1", path: null }]} />;
  },
};
