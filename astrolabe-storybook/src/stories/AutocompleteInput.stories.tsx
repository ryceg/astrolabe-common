import {
  AutocompleteInput,
  defaultAutocompleteClasses,
} from "@astrolabe/ui/AutocompleteInput";
import { Meta, StoryObj } from "@storybook/react";
import { useControl } from "@react-typed-forms/core";

const meta: Meta<typeof AutocompleteInput> = {
  component: AutocompleteInput,
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story, params) => {
      const textControl = useControl<string>("");
      const selectedDemo = useControl<string | null>(null);
      return (
        <Story
          args={{
            ...params.args,
            selectedControl: selectedDemo,
            textControl: textControl,
          }}
        />
      );
    },
  ],
};

export default meta;
type Story = StoryObj<typeof AutocompleteInput>;

export const NormalInputAutocomplete: Story = {
  render: (args) => {
    return (
      <AutocompleteInput
        autoHighlight
        selectedControl={args.selectedControl}
        textControl={args.textControl}
        classes={{
          container: "relative grow",
          input:
            "border-surface-50 rounded-md rounded-r-none w-full text-surface-950",
          optionList: defaultAutocompleteClasses.optionList,
          option:
            "[&.Mui-focused]:bg-primary-500 [&.Mui-focused]:text-primary-50 aria-selected:bg-primary-500 hover:bg-primary-500 hover:text-primary-50 aria-selected:text-primary-50 relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50",
        }}
        inputPlaceholder="Enter a Vehicle Code"
        getOptionText={(e) => `${e}`}
        options={["1", "2", "3"]}
        dontFilter
      />
    );
  },
};
