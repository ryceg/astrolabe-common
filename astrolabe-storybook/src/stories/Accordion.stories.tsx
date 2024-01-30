import { Accordion } from "@astrolabe/ui/Accordion";
import { Meta, StoryObj } from "@storybook/react";

const meta: Meta<typeof Accordion> = {
  component: Accordion,
  parameters: {
    layout: "centered",
  },
};

export default meta;
type Story = StoryObj<typeof Accordion>;

export const Primary: Story = {
  render: () => {
    return (
      <Accordion
        type="single"
        collapsible
        className="text-surface-950"
        itemClass="text-surface-950"
        children={[
          {
            title: "History",
            contents: (
              <div className="flex flex-col gap-2 divide-y">
                <section>
                  <div className="my-2 flex justify-between font-bold">
                    <div>Lorem Ipsumer</div>
                    <div>Updated lorember 2019</div>
                  </div>
                  <p>
                    Lorem ipsum dolor sit amet consectetur adipisicing elit.
                    Illo in eum libero atque reprehenderit dolorem eligendi hic
                    asperiores magni sint? Nemo pariatur alias non magni
                    accusantium, minima voluptatem facere quo.
                  </p>
                </section>
                <section>
                  <div className="my-2 flex justify-between font-bold">
                    <div>Lorem Ipsumd</div>
                    <div>Updated 26 ipsumary 2016</div>
                  </div>
                  <p>
                    Lorem ipsum dolor sit amet, consectetur adipisicing elit.
                    Quasi ullam impedit distinctio laboriosam accusantium
                    dignissimos? Numquam, earum corporis dignissimos quod
                    expedita impedit ipsa deserunt quibusdam consectetur facere,
                    tempore aperiam quis.
                  </p>
                </section>
              </div>
            ),
          },
        ]}
      />
    );
  },
};
