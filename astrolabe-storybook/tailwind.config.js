/** @type {import('tailwindcss').Config} */

module.exports = {
  content: [
    "./src/stories/**/*.{js,ts,jsx,tsx,mdx}",
    "../astrolabe-ui/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  presets: [require("./tailwind.config.ts")],
};
