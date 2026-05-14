import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: "class",
  content: ["./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Cairo", "Tahoma", "system-ui", "sans-serif"],
      },
      colors: {
        brand: {
          DEFAULT: "#1976d2",
          dark: "#115293",
          light: "#42a5f5",
        },
        accent: "#26a69a",
        danger: "#e53935",
        warn: "#ffa000",
        success: "#43a047",
      },
    },
  },
  plugins: [],
};
export default config;
