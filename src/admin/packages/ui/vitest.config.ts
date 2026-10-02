import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "jsdom",
    setupFiles: ["./src/setup-tests.ts"],
    include: ["src/**/*.test.tsx"],
  },
});
