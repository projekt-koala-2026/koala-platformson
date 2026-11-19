import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
    plugins: [react()],
    define: {
        "import.meta.env.VITE_API_URL": JSON.stringify("http://localhost:8080"),
    },
    test: {
        environment: "jsdom",
        setupFiles: ["./tests/setup.ts"],
        clearMocks: true,
        restoreMocks: true,
        coverage: {
            provider: "v8",
            reporter: ["text", "html", "lcov"],
            reportsDirectory: "./coverage",
            include: ["src/**/*.{ts,tsx}"],
            exclude: ["src/main.tsx", "src/vite-env.d.ts", "src/types/**"],
        },
    },
});
