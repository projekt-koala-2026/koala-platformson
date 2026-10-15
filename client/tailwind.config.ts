import type { Config } from "tailwindcss";

export default {
    content: ["./index.html", "./src/**/*.{ts,tsx}"],
    theme: {
        extend: {
            boxShadow: { panel: "0 12px 30px rgb(15 23 42 / 0.06)" },
        },
    },
    plugins: [],
} satisfies Config;
