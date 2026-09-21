import jsxA11y from "eslint-plugin-jsx-a11y";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

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
    files: ["**/vite.config.ts"],
    languageOptions: {
      globals: globals.node,
    },
  },
);
