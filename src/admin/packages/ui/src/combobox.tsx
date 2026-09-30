import { Loader2, Search } from "lucide-react";
import { useEffect, useId, useRef, useState, type KeyboardEvent, type Ref } from "react";
import { cn } from "./cn";
import { fieldControl } from "./field-control";
import type { FormFieldControlProps } from "./form-field";

export interface ComboboxOption {
  value: string;
  label: string;
  description?: string;
}

export interface ComboboxProps extends Partial<FormFieldControlProps> {
  inputValue: string;
  onInputValueChange: (value: string) => void;
  options: ComboboxOption[];
  onSelect: (option: ComboboxOption) => void;
  loading?: boolean;
  placeholder?: string;
  emptyText?: string;
  "aria-label"?: string;
  className?: string;
  ref?: Ref<HTMLInputElement>;
}

/**
 * Autocomplete input with a listbox popup (WAI-ARIA combobox, list autocomplete).
 * The caller owns the input text and the options, so it can search the server;
 * the combobox owns open state, the active option, and keyboard handling.
 */
export function Combobox({
  inputValue,
  onInputValueChange,
  options,
  onSelect,
  loading = false,
  placeholder,
  emptyText = "No matches",
  className,
  ref,
  ...inputProps
}: ComboboxProps) {
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const listRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();

  const active = activeIndex < options.length ? activeIndex : -1;
  const activeOption = active >= 0 ? options[active] : undefined;
  const optionId = (index: number) => `${listboxId}-opt-${index}`;

  useEffect(() => {
    if (open && active >= 0) {
      listRef.current?.querySelector(`[data-index="${active}"]`)?.scrollIntoView?.({ block: "nearest" });
    }
  }, [open, active]);

  const show = () => {
    setOpen(true);
    setActiveIndex(-1);
  };

  const close = () => {
    setOpen(false);
    setActiveIndex(-1);
  };

  const choose = (option: ComboboxOption) => {
    close();
    onSelect(option);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      if (!open) {
        setOpen(true);
        setActiveIndex(options.length > 0 ? 0 : -1);
      } else if (options.length > 0) {
        setActiveIndex((active + 1) % options.length);
      }
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      if (open && options.length > 0) {
        setActiveIndex(active <= 0 ? options.length - 1 : active - 1);
      }
    } else if (event.key === "Enter") {
      if (open && activeOption) {
        event.preventDefault();
        choose(activeOption);
      }
    } else if (event.key === "Escape") {
      if (open) {
        event.preventDefault();
        close();
      }
    }
  };

  return (
    <div className={cn("relative w-full", className)}>
      <Search
        className="pointer-events-none absolute top-1/2 left-3 size-3.5 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <input
        {...inputProps}
        ref={ref}
        type="text"
        role="combobox"
        autoComplete="off"
        aria-autocomplete="list"
        aria-expanded={open}
        aria-controls={listboxId}
        aria-activedescendant={open && active >= 0 ? optionId(active) : undefined}
        aria-busy={loading || undefined}
        value={inputValue}
        placeholder={placeholder}
        onChange={(event) => {
          onInputValueChange(event.target.value);
          show();
        }}
        onFocus={show}
        onClick={() => {
          if (!open) {
            show();
          }
        }}
        onBlur={close}
        onKeyDown={onKeyDown}
        className={cn("flex h-9 py-1 pr-9 pl-8.5", fieldControl)}
      />
      {loading ? (
        <Loader2
          className="pointer-events-none absolute top-1/2 right-3 size-3.5 -translate-y-1/2 animate-spin text-muted-foreground"
          aria-hidden
        />
      ) : null}
      <div
        ref={listRef}
        id={listboxId}
        role="listbox"
        tabIndex={-1}
        aria-label={inputProps["aria-label"] ?? "Suggestions"}
        hidden={!open}
        onMouseDown={(event) => event.preventDefault()}
        className="absolute inset-x-0 top-full z-30 mt-1.5 max-h-72 animate-pop overflow-y-auto rounded-xl border border-border bg-card p-1 shadow-pop"
      >
        {options.length === 0 ? (
          <p className="flex items-center gap-2 px-2.5 py-2 text-[13px] text-muted-foreground">
            {loading ? (
              <>
                <Loader2 className="size-3.5 animate-spin" aria-hidden />
                Searching…
              </>
            ) : (
              emptyText
            )}
          </p>
        ) : (
          options.map((option, index) => (
            <button
              key={option.value}
              id={optionId(index)}
              type="button"
              role="option"
              tabIndex={-1}
              aria-selected={index === active}
              data-index={index}
              onClick={() => choose(option)}
              onMouseMove={() => setActiveIndex(index)}
              className={cn(
                "flex min-h-8 w-full cursor-pointer items-center justify-between gap-3 rounded-lg px-2.5 py-1.5 text-left text-[13px]",
                index === active ? "bg-accent text-accent-foreground" : "text-foreground",
              )}
            >
              <span className="min-w-0 truncate">{option.label}</span>
              {option.description ? (
                <span className="shrink-0 text-xs text-muted-foreground">{option.description}</span>
              ) : null}
            </button>
          ))
        )}
      </div>
      <span role="status" className="sr-only">
        {open && !loading ? `${options.length} ${options.length === 1 ? "result" : "results"}` : ""}
      </span>
    </div>
  );
}
