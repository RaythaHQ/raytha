import { render, screen } from "@testing-library/react";
import { axe } from "jest-axe";
import { describe, expect, it } from "vitest";
import { Button } from "./button";

describe("Button", () => {
  it("has no axe violations", async () => {
    const { container } = render(<Button type="button">Save</Button>);

    expect(screen.getByRole("button", { name: "Save" })).toBeEnabled();
    expect(await axe(container)).toHaveNoViolations();
  });
});
