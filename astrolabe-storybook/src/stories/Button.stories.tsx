import { Button } from "@astrolabe/ui/Button";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof Button> = {
  component: Button,
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof Button>;

export const Primary: Story = {
  args: {
    // control
    variant: "primary",
    size: "default",
  },
  render: ({ variant, size, ...args }) => {
    return (
      <Button {...args} variant={variant} size={size}>
        Test
      </Button>
    );
  },
};
