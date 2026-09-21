"use client"

import { forwardRef, Fragment, useMemo } from "react"

// --- Tiptap UI Primitive ---
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/tiptap-ui-primitive/tooltip"

// --- Icons ---
import { CheckIcon } from "@/components/tiptap-icons/check-icon"

// --- Lib ---
import { cn, parseShortcutKeys } from "@/lib/tiptap-utils"

import "@/components/tiptap-ui-primitive/button/button-colors.scss"
import "@/components/tiptap-ui-primitive/button/button.scss"

export type ButtonStyle = "ghost" | "primary"
export type ButtonVariant = ButtonStyle | "check"
export type ButtonSize = "small" | "default" | "large"

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  showTooltip?: boolean
  tooltip?: React.ReactNode
  shortcutKeys?: string
  /**
   * Visual treatment or feature variant.
   *
   * The `check` variant is a checkbox-style toggle button. It defaults to
   * the ghost style and small size, supplies `role="checkbox"`, and renders
   * its own check indicator. Set `aria-checked` to control its state.
   *
   * @example
   * ```tsx
   * <Button
   *   variant="check"
   *   aria-checked={checked}
   *   onClick={() => setChecked((value) => !value)}
   * >
   *   <FilterIcon className="tiptap-button-icon" />
   *   <span className="tiptap-button-text">Only active items</span>
   * </Button>
   * ```
   */
  variant?: ButtonVariant
  size?: ButtonSize
}

export const ShortcutDisplay: React.FC<{ shortcuts: string[] }> = ({
  shortcuts,
}) => {
  if (shortcuts.length === 0) return null

  return (
    <div>
      {shortcuts.map((key, index) => (
        <Fragment key={index}>
          {index > 0 && <kbd>+</kbd>}
          <kbd>{key}</kbd>
        </Fragment>
      ))}
    </div>
  )
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      children,
      tooltip,
      showTooltip = true,
      shortcutKeys,
      variant,
      size,
      role,
      "aria-checked": ariaChecked,
      ...props
    },
    ref
  ) => {
    const isCheckVariant = variant === "check"
    const buttonStyle: ButtonStyle | undefined = isCheckVariant
      ? "ghost"
      : variant
    const buttonSize = isCheckVariant ? (size ?? "small") : size
    const buttonRole = isCheckVariant ? (role ?? "checkbox") : role
    const buttonAriaChecked = isCheckVariant
      ? (ariaChecked ?? false)
      : ariaChecked
    const shortcuts = useMemo<string[]>(
      () => parseShortcutKeys({ shortcutKeys }),
      [shortcutKeys]
    )
    const content = (
      <>
        {children}
        {isCheckVariant && (
          <span className="tiptap-button-check" aria-hidden="true">
            <CheckIcon />
          </span>
        )}
      </>
    )

    if (!tooltip || !showTooltip) {
      return (
        <button
          data-slot="tiptap-button"
          className={cn("tiptap-button", className)}
          ref={ref}
          data-style={buttonStyle}
          data-size={buttonSize}
          data-variant={isCheckVariant ? "check" : undefined}
          role={buttonRole}
          aria-checked={buttonAriaChecked}
          {...props}
        >
          {content}
        </button>
      )
    }

    return (
      <Tooltip delay={200}>
        <TooltipTrigger
          data-slot="tiptap-button"
          className={cn("tiptap-button", className)}
          ref={ref}
          data-style={buttonStyle}
          data-size={buttonSize}
          data-variant={isCheckVariant ? "check" : undefined}
          role={buttonRole}
          aria-checked={buttonAriaChecked}
          {...props}
        >
          {content}
        </TooltipTrigger>
        <TooltipContent>
          {tooltip}
          <ShortcutDisplay shortcuts={shortcuts} />
        </TooltipContent>
      </Tooltip>
    )
  }
)

Button.displayName = "Button"

export default Button
