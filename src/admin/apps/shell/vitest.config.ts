import path from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
      "@raytha/rich-text-editor": path.resolve(import.meta.dirname, "./src/components/rich-text-editor.tsx"),
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/setup-tests.ts"],
    include: ["src/**/*.test.{ts,tsx}"],
  },
});
