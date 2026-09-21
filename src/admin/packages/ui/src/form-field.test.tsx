import { render, screen } from "@testing-library/react";
import { axe } from "jest-axe";
import { describe, expect, it } from "vitest";
import { FormField } from "./form-field";
import { Input } from "./input";
import { Switch } from "./switch";

describe("FormField", () => {
  it("labels the control and wires required, invalid, and describedby", async () => {
    const { container } = render(
      <FormField label="Email" hint="Work address" error="Enter an email" required htmlFor="email">
        {(field) => <Input {...field} />}
      </FormField>,
    );

    const input = screen.getByLabelText(/email/i);
    expect(input).toBeRequired();
    expect(input).toHaveAttribute("aria-required", "true");
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(input).toHaveAttribute("aria-describedby", "email-hint email-error");
    expect(screen.getByText("Work address")).toHaveAttribute("id", "email-hint");

    const alert = screen.getByRole("alert");
    expect(alert).toHaveTextContent("Enter an email");
    expect(alert).toHaveAttribute("id", "email-error");

    expect(await axe(container)).toHaveNoViolations();
  });

  it("stacks an inline switch under the label", async () => {
    const { container } = render(
      <FormField label="Enabled" htmlFor="enabled">
        {(field) => <Switch {...field} checked={false} onCheckedChange={() => undefined} />}
      </FormField>,
    );

    expect(container.firstElementChild).toHaveClass("flex", "flex-col");
    expect(screen.getByRole("switch", { name: "Enabled" })).toBeInTheDocument();
    expect(await axe(container)).toHaveNoViolations();
  });
});
