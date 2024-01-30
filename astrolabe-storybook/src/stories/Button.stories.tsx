import { Button } from "@astrolabe/ui/Button";
import { Meta, StoryObj } from "@storybook/react";
import { userEvent, within } from "@storybook/test";

const meta: Meta<typeof Button> = {
  component: Button,
  parameters: {
    layout: "centered",
  },
  argTypes: {
    variant: {
      control: "select",
      options: [
        "default",
        "primary",
        "secondary",
        "warning",
        "danger",
        "outline",
        "ghost",
        "gray",
        "link",
        "hyperlink",
      ],
      description: "The visual style variant for the button.",
      table: {
        defaultValue: { summary: "default" },
      },
    },
    size: {
      control: "radio",
      options: ["default", "sm", "lg"],
      description: "The size variant for the button.",
    },
    asChild: {
      description:
        "Whether to render the button as a child of a slot component",
      table: {
        defaultValue: { summary: "false" },
      },
    },
    onClick: { action: "clicked" },
  },
};

export default meta;
type Story = StoryObj<typeof Button>;

export const PlainButton: Story = {
  args: {
    className: "",
    variant: "primary",
    size: "default",
    asChild: false,
  },
  render: ({ variant, size, onClick, ...args }) => {
    return (
      <Button {...args} variant={variant} size={size} onClick={onClick}>
        Button Text
      </Button>
    );
  },

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const button = canvas.getByRole("button");

    await userEvent.click(button);
  },
};

export const IconButton: Story = {
  args: {
    className: "flex gap-2",
    variant: "primary",
    size: "default",
    asChild: false,
  },
  render: ({ variant, size, onClick, ...args }) => {
    return (
      <Button {...args} variant={variant} size={size} onClick={onClick}>
        <i className="fa-light fa-route" />
        <span>Route icon button</span>
      </Button>
    );
  },
};
