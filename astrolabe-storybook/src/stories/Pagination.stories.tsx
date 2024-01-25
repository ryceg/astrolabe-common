import { Pagination } from "@astrolabe/ui/Pagination";
import { Meta, StoryObj } from "@storybook/react";
import { useArgs } from "@storybook/preview-api";

const meta: Meta<typeof Pagination> = {
  component: Pagination,
  parameters: {
    layout: "centered",
  },
  args: {
    total: 10,
    page: 0,
    perPage: 2,
    onPageChange: () => {},
  },
};

export default meta;
type Story = StoryObj<typeof Pagination>;

export const Primary: Story = {
  render: (args) => {
    const [{ page }, updateArgs] = useArgs();

    return (
      <Pagination
        {...args}
        page={page}
        onPageChange={(p) => {
          updateArgs({ page: p });
        }}
      />
    );
  },
};
