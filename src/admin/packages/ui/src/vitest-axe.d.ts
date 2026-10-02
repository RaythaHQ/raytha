import type { AxeResults } from "axe-core";

declare module "vitest" {
  interface Assertion {
    toHaveNoViolations(): AxeResults;
  }
}
