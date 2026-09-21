"use client"

import { forwardRef, useCallback, useEffect, useRef } from "react"
import { useHotkeys } from "react-hotkeys-hook"

// --- Hooks ---
import { useComposedRef } from "@/hooks/use-composed-ref"

// --- Icons ---
import { ArrowRightIcon } from "@/components/tiptap-icons/arrow-right-icon"
import { CaseSensitiveIcon } from "@/components/tiptap-icons/case-sensitive-icon"
import { ChevronDownIcon } from "@/components/tiptap-icons/chevron-down-icon"
import { ChevronUpIcon } from "@/components/tiptap-icons/chevron-up-icon"
import { CloseIcon } from "@/components/tiptap-icons/close-icon"
import { ExternalLinkIcon } from "@/components/tiptap-icons/external-link-icon"
import { SearchIcon } from "@/components/tiptap-icons/search-icon"
import { WholeWordIcon } from "@/components/tiptap-icons/whole-word-icon"

// --- Lib ---
import { cn } from "@/lib/tiptap-utils"

// --- Tiptap UI ---
import type { UseSearchAndReplaceConfig } from "@/components/tiptap-ui/search-and-replace"
import {
  NEXT_RESULT_SHORTCUT_KEY,
  PREVIOUS_RESULT_SHORTCUT_KEY,
  SEARCH_AND_REPLACE_SHORTCUT_KEY,
  useSearchAndReplace,
} from "@/components/tiptap-ui/search-and-replace"

// --- UI Primitives ---
import type { ButtonProps } from "@/components/tiptap-ui-primitive/button"
import { Button } from "@/components/tiptap-ui-primitive/button"
import { ButtonGroup } from "@/components/tiptap-ui-primitive/button-group"
import {
  Card,
  CardBody,
  CardFooter,
  CardHeader,
} from "@/components/tiptap-ui-primitive/card"
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
} from "@/components/tiptap-ui-primitive/input-group"
import { Separator } from "@/components/tiptap-ui-primitive/separator"
import { Switch } from "@/components/tiptap-ui-primitive/switch"

// --- Styles ---
import "@/components/tiptap-ui/search-and-replace/search-and-replace.scss"

export interface SearchAndReplaceProps
  extends
    Omit<React.HTMLAttributes<HTMLDivElement>, "children">,
    UseSearchAndReplaceConfig {
  /**
   * Whether the panel is active. While `false` the panel stays mounted but
   * hidden: highlights are cleared and the entered terms are kept, so
   * toggling back on restores the previous search. Unmounting the panel
   * instead clears the search entirely.
   * @default true
   */
  open?: boolean
  /**
   * Called when the panel requests to close (close button or Escape). The
   * panel does not position or dismiss itself — rendering, placement and
   * open state are owned by the consumer.
   */
  onClose?: () => void
  /**
   * Called when the built-in Mod+F shortcut fires while the panel is hidden.
   * Wire it to your open state and the shortcut works out of the box;
   * without it the shortcut only refocuses the search input while the panel
   * is already open.
   */
  onOpen?: () => void
  /**
   * Whether the built-in Mod+F shortcut is bound. While bound (and `onOpen`
   * is wired), it replaces the browser's find-in-page everywhere on the
   * page — an app with its own search should never surface the native one.
   * @default true
   */
  enableShortcut?: boolean
  /**
   * Whether the search input focuses itself when the panel becomes open.
   * @default true
   */
  autoFocusSearch?: boolean
}

export type SearchAndReplaceContentProps = SearchAndReplaceProps

function isModKey(event: React.KeyboardEvent): boolean {
  return event.metaKey || event.ctrlKey
}

const REGEX_DOCS_URL =
  "https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_expressions"

const REGEX_SEARCH_EXAMPLES = [
  { label: "Any middle letter:", pattern: "c.t" },
  { label: "Either term:", pattern: "cat|tiptap" },
  { label: "Uppercase or lowercase C:", pattern: "[Cc]at" },
]

const REGEX_REPLACE_EXAMPLES = [
  { pattern: "TipTap|tiptap|TIPTAP", replacement: "Tiptap" },
  { pattern: "(cat|tiptap)", replacement: '"$1"' },
]

/**
 * Example row for the regex explanation box. Deliberately local to this
 * box — a small ghost-style button whose label mixes plain text with code
 * chips is too specific to become a shared primitive.
 */
function RegexExampleButton({
  pattern,
  onApply,
  children,
}: {
  pattern: string
  onApply: () => void
  children: React.ReactNode
}) {
  return (
    <ButtonGroup
      className="tiptap-search-replace-regex-example-row"
      orientation="horizontal"
    >
      <Button
        type="button"
        className="tiptap-search-replace-regex-example-button"
        data-regex-example={pattern}
        onClick={onApply}
        variant="ghost"
        size="small"
      >
        <span className="tiptap-button-text">{children}</span>
        <ArrowRightIcon className="tiptap-button-icon" aria-hidden="true" />
      </Button>
    </ButtonGroup>
  )
}

/**
 * Search and replace trigger button for toolbars. Purely presentational —
 * wire `onClick` to show the `SearchAndReplace` panel wherever your layout
 * places it (the panel itself binds Mod+F via its `onOpen` prop).
 */
export const SearchAndReplaceButton = forwardRef<
  HTMLButtonElement,
  ButtonProps
>(({ className, children, ...props }, ref) => {
  return (
    <Button
      type="button"
      className={className}
      variant="ghost"
      role="button"
      tabIndex={-1}
      aria-label="Search and replace"
      tooltip="Search and replace"
      shortcutKeys={SEARCH_AND_REPLACE_SHORTCUT_KEY}
      ref={ref}
      {...props}
    >
      {children || <SearchIcon className="tiptap-button-icon" />}
    </Button>
  )
})

SearchAndReplaceButton.displayName = "SearchAndReplaceButton"

/**
 * Search and replace panel for Tiptap editors.
 *
 * Deliberately unpositioned: render it inside a popover, docked in a corner
 * (Notion-style), inline in a collapsed mobile toolbar — the wrapper is up to
 * you. The panel only manages the search itself.
 *
 * For fully custom UIs, use the `useSearchAndReplace` hook instead.
 */
export const SearchAndReplace = forwardRef<
  HTMLDivElement,
  SearchAndReplaceProps
>(
  (
    {
      editor: providedEditor,
      hideWhenUnavailable = false,
      scrollIntoViewOptions,
      open = true,
      onClose,
      onOpen,
      enableShortcut = true,
      autoFocusSearch = true,
      className,
      ...divProps
    },
    ref
  ) => {
    const searchAndReplace = useSearchAndReplace({
      editor: providedEditor,
      hideWhenUnavailable,
      scrollIntoViewOptions,
    })

    const {
      isVisible,
      resultCountLabel,
      searchTerm,
      replaceTerm,
      caseSensitive,
      wholeWord,
      useRegex,
      canSearch,
      canNavigate,
      canReplace,
      canReplaceAll,
      setSearchTerm,
      setReplaceTerm,
      toggleCaseSensitive,
      toggleWholeWord,
      toggleUseRegex,
      goToNext,
      goToPrevious,
      replaceCurrent,
      replaceAll,
      applySearch,
      suspendSearch,
    } = searchAndReplace

    const searchInputRef = useRef<HTMLInputElement>(null)
    const panelRef = useRef<HTMLDivElement>(null)
    const composedPanelRef = useComposedRef(panelRef, ref)

    const focusSearchInput = useCallback(() => {
      const input = searchInputRef.current
      if (!input) return
      input.focus()
      input.select()
    }, [])

    // Example buttons fill the inputs so the pattern can be tested right
    // away; the extension debounce then applies the search.
    const applyRegexExample = useCallback(
      (pattern: string, replacement?: string) => {
        setSearchTerm(pattern)
        if (replacement !== undefined) {
          setReplaceTerm(replacement)
        }
        focusSearchInput()
      },
      [setSearchTerm, setReplaceTerm, focusSearchInput]
    )

    // Built-in Mod+F: with a custom search present, the browser's
    // find-in-page is replaced everywhere on the page, no matter where
    // focus sits. Opens the panel via `onOpen` when hidden, refocuses the
    // search input while already open. Only an unwired `onOpen` falls back
    // to the native find.
    useHotkeys(
      SEARCH_AND_REPLACE_SHORTCUT_KEY,
      (event) => {
        if (open) {
          event.preventDefault()
          focusSearchInput()
          return
        }

        if (!onOpen) return

        event.preventDefault()
        onOpen()
      },
      {
        enabled: enableShortcut && isVisible && canSearch,
        enableOnContentEditable: true,
        enableOnFormTags: true,
      },
      [open, onOpen, focusSearchInput]
    )

    // While search mode is on, plain arrow keys navigate results even when
    // focus sits outside the panel (e.g. on the page body right after
    // load). Form fields and the editor itself are excluded by default, so
    // the caret and other inputs keep their arrows; inside the panel the
    // panel's own key handler takes over.
    useHotkeys(
      "up,down",
      (event) => {
        const target = event.target instanceof Node ? event.target : null
        if (panelRef.current?.contains(target)) return

        event.preventDefault()
        if (event.key === "ArrowDown") {
          goToNext()
        } else {
          goToPrevious()
        }
      },
      { enabled: open && canNavigate },
      [goToNext, goToPrevious]
    )

    // Re-apply the stored terms when the panel becomes open and clear
    // highlights (keeping the values) when it hides. Refs keep the effect
    // from re-firing on every keystroke.
    const applySearchRef = useRef(applySearch)
    const suspendSearchRef = useRef(suspendSearch)
    useEffect(() => {
      applySearchRef.current = applySearch
      suspendSearchRef.current = suspendSearch
    })

    const previousOpenRef = useRef(false)
    useEffect(() => {
      if (previousOpenRef.current === open) return
      previousOpenRef.current = open

      if (open) {
        applySearchRef.current()
      } else {
        suspendSearchRef.current()
      }
    }, [open])

    // Focus the search input once per open, waiting until the editor is
    // ready — on a freshly loaded page the input is still disabled for a
    // moment, and focusing a disabled input is a no-op.
    const focusedForOpenRef = useRef(false)
    useEffect(() => {
      if (!open) {
        focusedForOpenRef.current = false
        return
      }
      if (focusedForOpenRef.current || !autoFocusSearch || !canSearch) return

      focusedForOpenRef.current = true
      focusSearchInput()
    }, [open, canSearch, autoFocusSearch, focusSearchInput])

    const handlePanelKeyDown = useCallback(
      (event: React.KeyboardEvent<HTMLDivElement>) => {
        if (event.key === "Escape") {
          if (onClose) {
            event.preventDefault()
            event.stopPropagation()
            onClose()
          }
          return
        }

        // Arrow up/down step through the results from anywhere inside the
        // panel — inputs, nav buttons, options — like Notion's find bar.
        // Handled at the panel so a focused button navigates too (and the
        // page doesn't scroll instead).
        if (
          (event.key === "ArrowDown" || event.key === "ArrowUp") &&
          !event.shiftKey &&
          !event.altKey &&
          !isModKey(event) &&
          !event.nativeEvent.isComposing &&
          canNavigate
        ) {
          event.preventDefault()
          if (event.key === "ArrowDown") {
            goToNext()
          } else {
            goToPrevious()
          }
          return
        }

        // Navigation shortcuts are scoped to the open panel on purpose, so
        // they never compete with shortcuts elsewhere in the app (e.g. the
        // image download button also uses Mod+Shift+D).
        if (isModKey(event) && event.shiftKey && !event.altKey) {
          const key = event.key.toLowerCase()

          if (key === "f" && canNavigate) {
            event.preventDefault()
            event.stopPropagation()
            goToNext()
            return
          }

          if (key === "d" && canNavigate) {
            event.preventDefault()
            event.stopPropagation()
            goToPrevious()
          }
        }
      },
      [onClose, canNavigate, goToNext, goToPrevious]
    )

    const handleSearchKeyDown = useCallback(
      (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (
          event.key === "Enter" &&
          !event.shiftKey &&
          !isModKey(event) &&
          !event.nativeEvent.isComposing &&
          canNavigate
        ) {
          event.preventDefault()
          goToNext()
        }
      },
      [canNavigate, goToNext]
    )

    const handleReplaceKeyDown = useCallback(
      (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (
          event.key === "Enter" &&
          !event.shiftKey &&
          !isModKey(event) &&
          !event.nativeEvent.isComposing &&
          canReplace
        ) {
          event.preventDefault()
          replaceCurrent()
        }
      },
      [canReplace, replaceCurrent]
    )

    if (!isVisible) {
      return null
    }

    return (
      <Card
        className={cn("tiptap-search-replace", className)}
        data-open={open ? "true" : "false"}
        role="dialog"
        aria-label="Search and replace"
        onKeyDown={handlePanelKeyDown}
        ref={composedPanelRef}
        {...divProps}
      >
        <CardHeader className="tiptap-search-replace-header">
          <span className="tiptap-search-replace-count" aria-live="polite">
            {resultCountLabel}
          </span>

          <div className="tiptap-search-replace-nav-group">
            <ButtonGroup className="button-group" orientation="horizontal">
              <ButtonGroup>
                <Button
                  type="button"
                  data-search-replace-action="prev"
                  variant="ghost"
                  size="small"
                  disabled={!canNavigate}
                  aria-label="Previous result"
                  tooltip="Previous result"
                  shortcutKeys={PREVIOUS_RESULT_SHORTCUT_KEY}
                  onClick={goToPrevious}
                >
                  <ChevronUpIcon className="tiptap-button-icon" />
                </Button>
              </ButtonGroup>
              <ButtonGroup>
                <Button
                  type="button"
                  data-search-replace-action="next"
                  variant="ghost"
                  size="small"
                  disabled={!canNavigate}
                  aria-label="Next result"
                  tooltip="Next result"
                  shortcutKeys={NEXT_RESULT_SHORTCUT_KEY}
                  onClick={goToNext}
                >
                  <ChevronDownIcon className="tiptap-button-icon" />
                </Button>
              </ButtonGroup>
            </ButtonGroup>

            <ButtonGroup className="button-group" orientation="horizontal">
              <Button
                type="button"
                data-search-replace-action="close"
                variant="ghost"
                size="small"
                aria-label="Close"
                tooltip="Close"
                shortcutKeys="esc"
                onClick={onClose}
              >
                <CloseIcon className="tiptap-button-icon" />
              </Button>
            </ButtonGroup>
          </div>
        </CardHeader>

        <CardBody className="tiptap-search-replace-body">
          <div className="tiptap-search-replace-query-controls">
            <div className="tiptap-search-replace-inputs">
              <InputGroup className="tiptap-search-replace-input-group">
                <InputGroupInput
                  data-field="search-query"
                  placeholder="Search"
                  aria-label="Search"
                  value={searchTerm}
                  autoFocus={open && autoFocusSearch}
                  autoComplete="off"
                  autoCorrect="off"
                  autoCapitalize="off"
                  spellCheck={false}
                  disabled={!canSearch}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  onKeyDown={handleSearchKeyDown}
                  ref={searchInputRef}
                />
                <InputGroupAddon
                  align="inline-end"
                  className="tiptap-search-replace-keyboard-hint-addon"
                >
                  <kbd
                    className="tiptap-search-replace-keyboard-hint"
                    aria-hidden="true"
                  >
                    ↵
                  </kbd>
                </InputGroupAddon>
              </InputGroup>

              <InputGroup className="tiptap-search-replace-input-group">
                <InputGroupInput
                  data-field="replace-query"
                  placeholder="Replace"
                  aria-label="Replace"
                  value={replaceTerm}
                  autoComplete="off"
                  autoCorrect="off"
                  autoCapitalize="off"
                  spellCheck={false}
                  disabled={!canSearch}
                  onChange={(event) => setReplaceTerm(event.target.value)}
                  onKeyDown={handleReplaceKeyDown}
                />
                <InputGroupAddon
                  align="inline-end"
                  className="tiptap-search-replace-keyboard-hint-addon"
                >
                  <kbd
                    className="tiptap-search-replace-keyboard-hint"
                    aria-hidden="true"
                  >
                    ↵
                  </kbd>
                </InputGroupAddon>
              </InputGroup>
            </div>

            <div className="tiptap-search-replace-options">
              <Button
                type="button"
                data-option="match-case"
                variant="check"
                aria-checked={caseSensitive}
                disabled={!canSearch}
                onClick={toggleCaseSensitive}
              >
                <CaseSensitiveIcon
                  className="tiptap-button-icon"
                  aria-hidden="true"
                />
                <span className="tiptap-button-text">Match case</span>
              </Button>

              <Button
                type="button"
                data-option="whole-words"
                variant="check"
                aria-checked={wholeWord}
                disabled={!canSearch}
                onClick={toggleWholeWord}
              >
                <WholeWordIcon
                  className="tiptap-button-icon"
                  aria-hidden="true"
                />
                <span className="tiptap-button-text">Whole words</span>
              </Button>
            </div>
          </div>

          <Separator orientation="horizontal" />

          <div className="tiptap-search-replace-regex-toggle">
            <label
              data-option="use-regex"
              className="tiptap-search-replace-regex-toggle-row"
            >
              <span className="tiptap-search-replace-regex-toggle-label">
                Use regular expression
              </span>
              <Switch
                checked={useRegex}
                onCheckedChange={toggleUseRegex}
                disabled={!canSearch}
              />
            </label>

            {useRegex && (
              <div className="tiptap-search-replace-regex-help">
                <div className="tiptap-search-replace-regex-help-section">
                  <span className="tiptap-search-replace-regex-help-label">
                    Try a search pattern
                  </span>

                  <ButtonGroup
                    orientation="vertical"
                    className="tiptap-search-replace-regex-example-list"
                  >
                    {REGEX_SEARCH_EXAMPLES.map(({ label, pattern }) => (
                      <RegexExampleButton
                        key={pattern}
                        pattern={pattern}
                        onApply={() => applyRegexExample(pattern)}
                      >
                        {label} <code>{pattern}</code>
                      </RegexExampleButton>
                    ))}
                  </ButtonGroup>
                </div>

                <Separator
                  orientation="horizontal"
                  className="tiptap-search-replace-regex-help-separator"
                />

                <div className="tiptap-search-replace-regex-help-section">
                  <span className="tiptap-search-replace-regex-help-label">
                    Try search and replace
                  </span>

                  <ButtonGroup
                    orientation="vertical"
                    className="tiptap-search-replace-regex-example-list"
                  >
                    {REGEX_REPLACE_EXAMPLES.map(({ pattern, replacement }) => (
                      <RegexExampleButton
                        key={pattern}
                        pattern={pattern}
                        onApply={() => applyRegexExample(pattern, replacement)}
                      >
                        <code>{pattern}</code> to <code>{replacement}</code>
                      </RegexExampleButton>
                    ))}
                  </ButtonGroup>

                  <a
                    className="tiptap-search-replace-regex-docs-link"
                    data-search-replace-action="regex-docs"
                    href={REGEX_DOCS_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Learn about regular expressions
                    <ExternalLinkIcon
                      className="tiptap-search-replace-regex-docs-link-icon"
                      aria-hidden="true"
                    />
                  </a>
                </div>
              </div>
            )}
          </div>

          <Separator orientation="horizontal" />
        </CardBody>

        <CardFooter className="tiptap-search-replace-actions">
          <ButtonGroup className="button-group" orientation="horizontal">
            <ButtonGroup>
              <Button
                type="button"
                data-search-replace-action="replace"
                disabled={!canReplace}
                aria-label="Replace current result"
                onClick={replaceCurrent}
              >
                Replace
              </Button>
            </ButtonGroup>
            <ButtonGroup>
              <Button
                type="button"
                data-search-replace-action="replace-all"
                disabled={!canReplaceAll}
                aria-label="Replace all results"
                onClick={replaceAll}
              >
                Replace all
              </Button>
            </ButtonGroup>
          </ButtonGroup>
        </CardFooter>
      </Card>
    )
  }
)

SearchAndReplace.displayName = "SearchAndReplace"

/**
 * Alias of `SearchAndReplace` for contexts where the panel is embedded as
 * plain content (e.g. inside a collapsed mobile toolbar).
 */
export const SearchAndReplaceContent = SearchAndReplace

export default SearchAndReplace
