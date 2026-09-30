import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { axe } from "jest-axe";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Combobox, type ComboboxOption } from "./combobox";
import { FormField } from "./form-field";

const OPTIONS: ComboboxOption[] = [
  { value: "a", label: "Ada Lovelace" },
  { value: "b", label: "Grace Hopper" },
  { value: "c", label: "Alan Turing" },
];

function Harness({
  options = OPTIONS,
  loading = false,
  onSelect = () => undefined,
}: {
  options?: ComboboxOption[];
  loading?: boolean;
  onSelect?: (option: ComboboxOption) => void;
}) {
  const [text, setText] = useState("");
  return (
    <>
      <FormField label="Author" htmlFor="author">
        {(control) => (
          <Combobox
            {...control}
            inputValue={text}
            onInputValueChange={setText}
            options={options}
            loading={loading}
            onSelect={(option) => {
              setText(option.label);
              onSelect(option);
            }}
          />
        )}
      </FormField>
      <button type="button">Elsewhere</button>
    </>
  );
}

describe("Combobox", () => {
  afterEach(cleanup);

  it("opens on focus and exposes combobox and listbox semantics", async () => {
    const { container } = render(<Harness />);
    const input = screen.getByRole("combobox", { name: "Author" });
    expect(input).toHaveAttribute("aria-expanded", "false");

    act(() => input.focus());

    expect(input).toHaveAttribute("aria-expanded", "true");
    const listbox = screen.getByRole("listbox");
    expect(input).toHaveAttribute("aria-controls", listbox.id);
    expect(screen.getAllByRole("option")).toHaveLength(3);
    expect(await axe(container)).toHaveNoViolations();
  });

  it("moves the active option with arrow keys and selects it with Enter", () => {
    const onSelect = vi.fn();
    render(<Harness onSelect={onSelect} />);
    const input = screen.getByRole("combobox");
    act(() => input.focus());

    fireEvent.keyDown(input, { key: "ArrowDown" });
    fireEvent.keyDown(input, { key: "ArrowDown" });
    const grace = screen.getByRole("option", { name: "Grace Hopper" });
    expect(grace).toHaveAttribute("aria-selected", "true");
    expect(input).toHaveAttribute("aria-activedescendant", grace.id);

    fireEvent.keyDown(input, { key: "ArrowUp" });
    fireEvent.keyDown(input, { key: "ArrowUp" });
    expect(screen.getByRole("option", { name: "Alan Turing" })).toHaveAttribute("aria-selected", "true");

    fireEvent.keyDown(input, { key: "Enter" });
    expect(onSelect).toHaveBeenCalledWith(OPTIONS[2]);
    expect(input).toHaveValue("Alan Turing");
    expect(input).toHaveAttribute("aria-expanded", "false");
  });

  it("selects an option on click", () => {
    const onSelect = vi.fn();
    render(<Harness onSelect={onSelect} />);
    act(() => screen.getByRole("combobox").focus());

    fireEvent.click(screen.getByRole("option", { name: "Ada Lovelace" }));

    expect(onSelect).toHaveBeenCalledWith(OPTIONS[0]);
  });

  it("closes on Escape and when focus leaves", () => {
    render(<Harness />);
    const input = screen.getByRole("combobox");
    act(() => input.focus());

    fireEvent.keyDown(input, { key: "Escape" });
    expect(input).toHaveAttribute("aria-expanded", "false");

    fireEvent.keyDown(input, { key: "ArrowDown" });
    expect(input).toHaveAttribute("aria-expanded", "true");

    act(() => screen.getByRole("button", { name: "Elsewhere" }).focus());
    expect(input).toHaveAttribute("aria-expanded", "false");
  });

  it("reopens as the user types", () => {
    render(<Harness />);
    const input = screen.getByRole("combobox");
    act(() => input.focus());
    fireEvent.keyDown(input, { key: "Escape" });

    fireEvent.change(input, { target: { value: "gr" } });

    expect(input).toHaveValue("gr");
    expect(input).toHaveAttribute("aria-expanded", "true");
  });

  it("shows loading and empty states", () => {
    const { rerender } = render(<Harness options={[]} loading />);
    const input = screen.getByRole("combobox");
    act(() => input.focus());
    expect(screen.getByRole("listbox")).toHaveTextContent("Searching…");
    expect(input).toHaveAttribute("aria-busy", "true");

    rerender(<Harness options={[]} />);
    expect(screen.getByRole("listbox")).toHaveTextContent("No matches");
    expect(screen.getByRole("status")).toHaveTextContent("0 results");
  });
});
