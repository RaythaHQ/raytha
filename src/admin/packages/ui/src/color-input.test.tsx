import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { axe } from "jest-axe";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ColorInput, normalizeHexColor } from "./color-input";
import { FormField } from "./form-field";

function Harness({ initial = "", onChange = () => undefined }: { initial?: string; onChange?: (next: string) => void }) {
  const [value, setValue] = useState(initial);
  return (
    <FormField label="Accent" htmlFor="accent">
      {(control) => (
        <ColorInput
          {...control}
          value={value}
          onChange={(next) => {
            setValue(next);
            onChange(next);
          }}
        />
      )}
    </FormField>
  );
}

afterEach(cleanup);

describe("normalizeHexColor", () => {
  it("expands short hex, adds the hash, and lowercases", () => {
    expect(normalizeHexColor("#F80")).toBe("#ff8800");
    expect(normalizeHexColor("ABCDEF")).toBe("#abcdef");
    expect(normalizeHexColor(" #123456 ")).toBe("#123456");
  });

  it("rejects anything that is not 3 or 6 hex digits", () => {
    for (const text of ["", "#ff88", "#gggggg", "red", "#ff880000"]) {
      expect(normalizeHexColor(text)).toBeUndefined();
    }
  });
});

describe("ColorInput", () => {
  it("labels the hex field and names the swatch", async () => {
    const { container } = render(<Harness initial="#336699" />);

    expect(screen.getByLabelText("Accent")).toHaveValue("#336699");
    expect(screen.getByLabelText("Pick a color")).toHaveValue("#336699");
    expect(await axe(container)).toHaveNoViolations();
  });

  it("emits a full hex as it is typed and keeps the swatch in sync", () => {
    const onChange = vi.fn();
    render(<Harness onChange={onChange} />);

    fireEvent.change(screen.getByLabelText("Accent"), { target: { value: "#FF8800" } });

    expect(onChange).toHaveBeenLastCalledWith("#ff8800");
    expect(screen.getByLabelText("Pick a color")).toHaveValue("#ff8800");
  });

  it("expands short hex on blur and reverts invalid text", () => {
    const onChange = vi.fn();
    render(<Harness initial="#000000" onChange={onChange} />);
    const hex = screen.getByLabelText("Accent");

    fireEvent.change(hex, { target: { value: "#f80" } });
    expect(onChange).not.toHaveBeenCalled();
    fireEvent.blur(hex);
    expect(onChange).toHaveBeenLastCalledWith("#ff8800");
    expect(hex).toHaveValue("#ff8800");

    fireEvent.change(hex, { target: { value: "orange" } });
    expect(hex).toHaveAttribute("aria-invalid", "true");
    fireEvent.blur(hex);
    expect(hex).toHaveValue("#ff8800");
  });

  it("picks from the swatch and clears when the hex field is emptied", () => {
    const onChange = vi.fn();
    render(<Harness initial="#000000" onChange={onChange} />);

    fireEvent.input(screen.getByLabelText("Pick a color"), { target: { value: "#aabbcc" } });
    expect(onChange).toHaveBeenLastCalledWith("#aabbcc");
    expect(screen.getByLabelText("Accent")).toHaveValue("#aabbcc");

    fireEvent.change(screen.getByLabelText("Accent"), { target: { value: "" } });
    expect(onChange).toHaveBeenLastCalledWith("");
  });
});
