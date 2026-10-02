import jsxA11y from "eslint-plugin-jsx-a11y";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

const rawHttpMessage =
  "RFC-0006: admin HTTP goes through @raytha/api, which owns base paths, JSON headers, error mapping, and the session cache.";
const rawHttpGlobals = ["fetch", "XMLHttpRequest", "EventSource"];

export default tseslint.config(
  {
    ignores: [
      "**/dist/**",
      "**/node_modules/**",
      "types/**",
      // TipTap UI kit is copied source; linting it fights their React 18 patterns.
      "apps/shell/src/components/tiptap-ui/**",
      "apps/shell/src/components/tiptap-ui-primitive/**",
      "apps/shell/src/components/tiptap-node/**",
      "apps/shell/src/components/tiptap-icons/**",
      "apps/shell/src/components/tiptap-extension/**",
      "apps/shell/src/components/tiptap-templates/**",
      "apps/shell/src/hooks/**",
      "apps/shell/src/lib/tiptap-utils.ts",
      "apps/shell/e2e/**",
      "apps/shell/playwright.config.ts",
      "packages/ui/src/**/*.test.tsx",
      "packages/ui/src/setup-tests.ts",
      "packages/ui/vitest.config.ts",
      "apps/shell/src/**/*.test.{ts,tsx}",
      "apps/shell/src/setup-tests.ts",
      "apps/shell/vitest.config.ts",
    ],
  },
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      globals: globals.browser,
    },
    plugins: {
      "react-hooks": reactHooks,
      "jsx-a11y": jsxA11y,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      ...jsxA11y.configs.recommended.rules,
      "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_" }],
      "@typescript-eslint/consistent-type-imports": "error",
      "jsx-a11y/label-has-associated-control": [
        "error",
        { controlComponents: ["Checkbox", "Input", "Select", "Switch", "Textarea", "FileUpload"] },
      ],
    },
  },
  {
    files: ["apps/**/*.{ts,tsx}", "packages/ui/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-globals": ["error", ...rawHttpGlobals.map((name) => ({ name, message: rawHttpMessage }))],
      "no-restricted-properties": [
        "error",
        ...["window", "globalThis", "self"].flatMap((object) =>
          rawHttpGlobals.map((property) => ({ object, property, message: rawHttpMessage })),
        ),
      ],
      "no-restricted-imports": [
        "error",
        {
          paths: ["axios", "ky"].map((name) => ({ name, message: rawHttpMessage })),
          patterns: [{ group: ["axios/*", "ky/*"], message: rawHttpMessage }],
        },
      ],
    },
  },
  {
    // Grandfathered: duplicate XHR upload-with-progress helpers, to be replaced by one in @raytha/api.
    files: ["apps/shell/src/lib/media-upload.ts", "packages/ui/src/file-upload.tsx"],
    rules: {
      "no-restricted-globals": [
        "error",
        ...rawHttpGlobals
          .filter((name) => name !== "XMLHttpRequest")
          .map((name) => ({ name, message: rawHttpMessage })),
      ],
    },
  },
  {
    files: ["**/vite.config.ts"],
    languageOptions: {
      globals: globals.node,
    },
  },
);
