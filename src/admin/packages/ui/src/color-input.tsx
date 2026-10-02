import { useState, type InputHTMLAttributes } from "react";
import { cn } from "./cn";
import { Input } from "./input";

/** `#rgb` or `#rrggbb`, with or without the `#`, as lowercase `#rrggbb`. */
export function normalizeHexColor(text: string): string | undefined {
  const hex = /^#?([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(text.trim())?.[1]?.toLowerCase();
  if (!hex) {
    return undefined;
  }
  return hex.length === 3 ? `#${[...hex].map((c) => c + c).join("")}` : `#${hex}`;
}

export interface ColorInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "value" | "onChange" | "type"> {
  /** Lowercase `#rrggbb`, or empty for no color. */
  value: string;
  onChange: (next: string) => void;
  swatchLabel?: string;
}

/**
 * A swatch that opens the native picker next to a hex field. The hex field emits a full
 * `#rrggbb` as soon as one is typed, expands `#rgb` on blur, and reverts anything else.
 */
export function ColorInput({
  value,
  onChange,
  disabled,
  className,
  swatchLabel = "Pick a color",
  onBlur,
  ...rest
}: ColorInputProps) {
  const [draft, setDraft] = useState(value);
  const [synced, setSynced] = useState(value);
  if (value !== synced) {
    setSynced(value);
    setDraft(value);
  }
  const invalid = draft.trim() !== "" && normalizeHexColor(draft) === undefined;

  return (
    <div className={cn("flex items-center gap-2", className)}>
      <span
        className="relative inline-flex size-9 shrink-0 overflow-hidden rounded-lg border border-input bg-card text-border-strong shadow-xs transition-[border-color,box-shadow] focus-within:border-brand-500 focus-within:ring-[3px] focus-within:ring-brand-500/20"
        style={value ? { backgroundColor: value } : undefined}
      >
        {!value && (
          <span
            aria-hidden
            className="absolute inset-0 bg-[linear-gradient(135deg,transparent_46%,currentColor_46%,currentColor_54%,transparent_54%)]"
          />
        )}
        <input
          type="color"
          aria-label={swatchLabel}
          value={value || "#000000"}
          disabled={disabled}
          onChange={(event) => onChange(event.target.value.toLowerCase())}
          className="absolute inset-0 size-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
        />
      </span>
      <Input
        {...rest}
        value={draft}
        disabled={disabled}
        placeholder="#rrggbb"
        spellCheck={false}
        autoComplete="off"
        aria-invalid={invalid || rest["aria-invalid"] === true || undefined}
        className="w-32 font-mono"
        onChange={(event) => {
          const next = event.target.value;
          setDraft(next);
          if (next.trim() === "") {
            onChange("");
          } else if (/^#?[0-9a-f]{6}$/i.test(next.trim())) {
            onChange(normalizeHexColor(next) ?? "");
          }
        }}
        onBlur={(event) => {
          const normalized = normalizeHexColor(draft);
          if (draft.trim() === "") {
            setDraft("");
          } else if (normalized) {
            setDraft(normalized);
            if (normalized !== value) {
              onChange(normalized);
            }
          } else {
            setDraft(value);
          }
          onBlur?.(event);
        }}
      />
    </div>
  );
}
