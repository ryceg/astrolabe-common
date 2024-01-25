import { Disabler } from "@astrolabe/ui/Disabler";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";
import { Numberfield } from "@astrolabe/ui/Numberfield";

const meta: Meta<typeof Disabler> = {
  component: Disabler,

  parameters: {
    layout: "centered",
  },
  args: {
    control: undefined,
    label: "Label placeholder",
  },
};

export default meta;
type Story = StoryObj<typeof Disabler>;

export const Primary: Story = {
  render: (args) => {
    const disablerControl = useControl(999);

    return (
      <Disabler
        control={disablerControl}
        label={args.label}
        render={(p) => <Numberfield {...p} label={args.label} />}
      />
    );
  },
};
