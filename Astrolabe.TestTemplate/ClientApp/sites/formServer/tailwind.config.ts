import type { Config } from "tailwindcss";
import * as ti from "./tail.include";

const config: Config = {
  content: [
    "./src/**/*.{js,ts,jsx,tsx,mdx}",
    "node_modules/@react-typed-forms/schemas/lib/*.js",
    "node_modules/@astroapps/schemas-editor/lib/*.js",
    "./**/node_modules/@astroapps/tree-ui/lib/*.js",
    "node_modules/@astrolabe/ui/**/*.tsx",
  ],
  presets: [ti],
  theme: {
    extend: {
      backgroundImage: {
        "gradient-radial": "radial-gradient(var(--tw-gradient-stops))",
        "gradient-conic":
          "conic-gradient(from 180deg at 50% 50%, var(--tw-gradient-stops))",
      },
    },
  },
  plugins: [],
};
export default config;
