import { describe, expect, it } from "vitest";
import { formatOrgDate } from "./entity";

describe("formatOrgDate", () => {
  it("renders a stored ISO date in the organization's pattern", () => {
    expect(formatOrgDate("2026-09-30", "MM/dd/yyyy")).toBe("09/30/2026");
    expect(formatOrgDate("2026-09-30", "dd/MM/yyyy")).toBe("30/09/2026");
  });

  it("ignores a time component", () => {
    expect(formatOrgDate("2026-01-05T00:00:00Z", "dd/MM/yyyy")).toBe("05/01/2026");
  });

  it("leaves values that are not ISO dates as stored", () => {
    expect(formatOrgDate("next tuesday", "MM/dd/yyyy")).toBe("next tuesday");
  });
});
