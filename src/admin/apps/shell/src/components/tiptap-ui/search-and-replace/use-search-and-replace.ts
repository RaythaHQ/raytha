"use client"

import { useCallback, useEffect, useState } from "react"
import type { Editor } from "@tiptap/react"
import { useCurrentEditor } from "@tiptap/react"
import type { FindAndReplaceStorage } from "@tiptap/extension-find-and-replace"

// --- Hooks ---
import { useTiptapEditor } from "@/hooks/use-tiptap-editor"

// --- Icons ---
import { SearchIcon } from "@/components/tiptap-icons/search-icon"

// --- Lib ---
import { isExtensionAvailable } from "@/lib/tiptap-utils"

export const SEARCH_AND_REPLACE_SHORTCUT_KEY = "mod+f"
export const NEXT_RESULT_SHORTCUT_KEY = "mod+shift+f"
export const PREVIOUS_RESULT_SHORTCUT_KEY = "mod+shift+d"

/**
 * CSS classes rendered by the Find and Replace extension for result
 * decorations inside the editor.
 */
export const SEARCH_RESULT_CLASS = "find-and-replace-result"
export const SEARCH_RESULT_CURRENT_CLASS = "find-and-replace-result-current"

export const DEFAULT_SCROLL_INTO_VIEW_OPTIONS: ScrollIntoViewOptions = {
  block: "nearest",
  inline: "nearest",
}

/**
 * Configuration for the search and replace functionality
 */
export interface UseSearchAndReplaceConfig {
  /**
   * The Tiptap editor instance.
   */
  editor?: Editor | null
  /**
   * Whether the trigger should hide when the Find and Replace extension
   * is not registered on the editor.
   * @default false
   */
  hideWhenUnavailable?: boolean
  /**
   * How the current result is scrolled into view. `behavior` is forced to
   * `auto` when the user prefers reduced motion.
   * @default DEFAULT_SCROLL_INTO_VIEW_OPTIONS
   */
  scrollIntoViewOptions?: ScrollIntoViewOptions
}

/**
 * Reactive snapshot of the Find and Replace extension storage.
 */
export interface SearchAndReplaceEditorState {
  /**
   * Number of results for the applied search term.
   */
  total: number
  /**
   * Zero-based index of the current result, or `null` when none is selected.
   */
  currentIndex: number | null
  /**
   * The search term the extension has applied (post-debounce).
   */
  appliedSearchTerm: string
  /**
   * Whether the search is case sensitive.
   */
  caseSensitive: boolean
  /**
   * Whether only whole words are matched.
   */
  wholeWord: boolean
  /**
   * Whether the search term is treated as a regular expression.
   */
  useRegex: boolean
}

const EMPTY_EDITOR_STATE: SearchAndReplaceEditorState = {
  total: 0,
  currentIndex: null,
  appliedSearchTerm: "",
  caseSensitive: false,
  wholeWord: false,
  useRegex: false,
}

/**
 * Checks if the Find and Replace extension is registered on the editor
 */
export function isFindAndReplaceAvailable(editor: Editor | null): boolean {
  return isExtensionAvailable(editor, "findAndReplace")
}

/**
 * Returns the Find and Replace extension storage, or `null` when unavailable
 */
export function getFindAndReplaceStorage(
  editor: Editor | null
): FindAndReplaceStorage | null {
  if (!isFindAndReplaceAvailable(editor)) return null
  return editor?.storage.findAndReplace ?? null
}

/**
 * Determines if the search and replace trigger should be shown
 */
export function shouldShowSearchAndReplace(props: {
  editor: Editor | null
  hideWhenUnavailable: boolean
}): boolean {
  const { editor, hideWhenUnavailable } = props

  if (!editor) return false
  if (hideWhenUnavailable) return isFindAndReplaceAvailable(editor)

  return true
}

/**
 * Formats the result counter as `current / total` for display.
 * The extension index is zero-based and can be `null`, so the counter
 * renders `0 / 0` without results and one-based positions otherwise.
 */
export function formatResultCount(
  currentIndex: number | null,
  total: number
): string {
  if (total === 0) return "0 / 0"
  if (currentIndex === null) return `0 / ${total}`

  return `${currentIndex + 1} / ${total}`
}

interface NavigateSearchResultOptions {
  editor: Editor
  direction: "next" | "previous"
}

function navigateSearchResult({
  editor,
  direction,
}: NavigateSearchResultOptions): boolean {
  const selection = editor.state.selection
  const chain = editor.chain()
  const navigation =
    direction === "next" ? chain.goToNextResult() : chain.goToPreviousResult()

  // Search navigation should update its highlight without changing the
  // editor selection that drives toolbar state.
  return navigation
    .command(({ tr }) => {
      tr.setSelection(selection.map(tr.doc, tr.mapping))
      return true
    })
    .run()
}

/**
 * Scrolls the current result into view without stealing focus. The element
 * is resolved from the result's document position rather than the highlight
 * decoration, so it works regardless of how decorations are rendered.
 * Skipped while the editor itself has focus so typing inside the document
 * never causes scroll jumps, and honors `prefers-reduced-motion`.
 */
export function scrollCurrentResultIntoView(
  editor: Editor | null,
  scrollIntoViewOptions: ScrollIntoViewOptions = DEFAULT_SCROLL_INTO_VIEW_OPTIONS
): void {
  if (!editor || editor.isDestroyed) return

  const storage = getFindAndReplaceStorage(editor)
  const currentIndex = storage?.currentIndex
  if (!storage || currentIndex === null || currentIndex === undefined) return

  const result = storage.results[currentIndex]
  if (!result) return

  const editorDom = editor.view.dom
  if (editorDom.contains(document.activeElement)) return

  let target: HTMLElement | null = null
  try {
    const { node } = editor.view.domAtPos(result.from)
    target = node instanceof HTMLElement ? node : node.parentElement
  } catch {
    return
  }

  if (!target || typeof target.scrollIntoView !== "function") return

  const prefersReducedMotion =
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches

  target.scrollIntoView({
    behavior: "smooth",
    ...scrollIntoViewOptions,
    ...(prefersReducedMotion ? { behavior: "auto" } : {}),
  })
}

/**
 * Main hook that provides search and replace functionality for a Tiptap editor.
 *
 * The `searchTerm`/`replaceTerm` values mirror the inputs immediately, while
 * search execution is forwarded to the extension's `setSearchTerm`, which
 * applies its own debounce (`searchDebounceMs`) — no additional UI debounce is
 * added. An empty search cancels pending work and clears highlights
 * immediately.
 *
 * @example
 * ```tsx
 * function MySearchPanel() {
 *   const {
 *     isVisible,
 *     resultCountLabel,
 *     searchTerm,
 *     setSearchTerm,
 *     goToNext,
 *   } = useSearchAndReplace()
 *
 *   if (!isVisible) return null
 *
 *   return (
 *     <div>
 *       <input
 *         value={searchTerm}
 *         onChange={(event) => setSearchTerm(event.target.value)}
 *       />
 *       <span>{resultCountLabel}</span>
 *       <button onClick={goToNext}>Next</button>
 *     </div>
 *   )
 * }
 * ```
 */
export function useSearchAndReplace(config?: UseSearchAndReplaceConfig) {
  const {
    editor: providedEditor,
    hideWhenUnavailable = false,
    scrollIntoViewOptions = DEFAULT_SCROLL_INTO_VIEW_OPTIONS,
  } = config || {}

  // `useTiptapEditor` resolves through `useEditorState`, whose snapshot stays
  // `null` until the editor's first transaction (with `immediatelyRender:
  // false` there may be none on mount). Fall back to the always-fresh context
  // editor so the trigger and commands work before any interaction.
  const { editor: resolvedEditor } = useTiptapEditor(providedEditor)
  const { editor: contextEditor } = useCurrentEditor()
  const editor = resolvedEditor ?? providedEditor ?? contextEditor ?? null
  const canSearch = isFindAndReplaceAvailable(editor)

  // Visibility defaults to true and is refined once the editor reports
  // activity. The editor resolved above stays `null` until its first
  // transaction (with `immediatelyRender: false` there may be none on
  // mount), so deriving visibility directly would hide the trigger on load.
  const [isVisible, setIsVisible] = useState(true)

  useEffect(() => {
    if (!editor) return

    const handleUpdate = () => {
      setIsVisible(shouldShowSearchAndReplace({ editor, hideWhenUnavailable }))
    }

    handleUpdate()

    editor.on("transaction", handleUpdate)

    return () => {
      editor.off("transaction", handleUpdate)
    }
  }, [editor, hideWhenUnavailable])

  // Immediate input values. Extension storage lags behind the search input by
  // the extension's debounce, so the inputs keep their own state while the
  // extension owns matching, navigation and replacement.
  const [searchTerm, setSearchTermState] = useState("")
  const [replaceTerm, setReplaceTermState] = useState("")

  // Adopt terms configured on the extension (e.g. an initial `searchTerm`
  // option) once the editor is available.
  useEffect(() => {
    const storage = getFindAndReplaceStorage(editor)
    if (!storage) return

    if (storage.searchTerm) {
      setSearchTermState((previous) => previous || storage.searchTerm)
    }
    if (storage.replaceTerm) {
      setReplaceTermState((previous) => previous || storage.replaceTerm)
    }
  }, [editor])

  // Subscribe to extension transactions so count, current index and disabled
  // states update immediately. Synced eagerly on subscribe: the extension may
  // already hold results from a configured `searchTerm` before any
  // transaction happens (e.g. on a freshly loaded page).
  const [editorState, setEditorState] =
    useState<SearchAndReplaceEditorState>(EMPTY_EDITOR_STATE)

  useEffect(() => {
    if (!editor || !isFindAndReplaceAvailable(editor)) {
      setEditorState(EMPTY_EDITOR_STATE)
      return
    }

    const sync = () => {
      const storage = getFindAndReplaceStorage(editor)
      if (!storage) return

      const next: SearchAndReplaceEditorState = {
        total: storage.results.length,
        currentIndex: storage.currentIndex,
        appliedSearchTerm: storage.searchTerm,
        caseSensitive: storage.caseSensitive,
        wholeWord: storage.wholeWord,
        useRegex: storage.useRegex,
      }

      setEditorState((previous) =>
        previous.total === next.total &&
        previous.currentIndex === next.currentIndex &&
        previous.appliedSearchTerm === next.appliedSearchTerm &&
        previous.caseSensitive === next.caseSensitive &&
        previous.wholeWord === next.wholeWord &&
        previous.useRegex === next.useRegex
          ? previous
          : next
      )
    }

    sync()

    editor.on("transaction", sync)

    return () => {
      editor.off("transaction", sync)
    }
  }, [editor])

  const {
    total,
    currentIndex,
    appliedSearchTerm,
    caseSensitive,
    wholeWord,
    useRegex,
  } = editorState

  const setSearchTerm = useCallback(
    (value: string) => {
      setSearchTermState(value)

      if (!editor || !isFindAndReplaceAvailable(editor)) return

      if (value.length === 0) {
        // Cancels the pending debounced search and clears highlights and
        // the result count immediately.
        editor.commands.clearSearch()
      } else {
        editor.commands.setSearchTerm(value)
      }
    },
    [editor]
  )

  const setReplaceTerm = useCallback(
    (value: string) => {
      setReplaceTermState(value)

      if (!editor || !isFindAndReplaceAvailable(editor)) return
      editor.commands.setReplaceTerm(value)
    },
    [editor]
  )

  const toggleCaseSensitive = useCallback(() => {
    if (!editor || !isFindAndReplaceAvailable(editor)) return
    editor.commands.setCaseSensitive(
      !getFindAndReplaceStorage(editor)?.caseSensitive
    )
  }, [editor])

  const toggleWholeWord = useCallback(() => {
    if (!editor || !isFindAndReplaceAvailable(editor)) return
    editor.commands.setWholeWord(!getFindAndReplaceStorage(editor)?.wholeWord)
  }, [editor])

  const toggleUseRegex = useCallback(() => {
    if (!editor || !isFindAndReplaceAvailable(editor)) return
    editor.commands.setUseRegex(!getFindAndReplaceStorage(editor)?.useRegex)
  }, [editor])

  const goToNext = useCallback(() => {
    if (!editor) return false
    return navigateSearchResult({ editor, direction: "next" })
  }, [editor])

  const goToPrevious = useCallback(() => {
    if (!editor) return false
    return navigateSearchResult({ editor, direction: "previous" })
  }, [editor])

  const replaceCurrent = useCallback(() => {
    if (!editor || !editor.isEditable) return false
    return editor.commands.replace()
  }, [editor])

  const replaceAll = useCallback(() => {
    if (!editor || !editor.isEditable) return false
    return editor.commands.replaceAll()
  }, [editor])

  /**
   * Re-applies the stored terms to the editor. Used when the search UI
   * reopens while the hook instance stayed mounted (e.g. popover reopen).
   */
  const applySearch = useCallback(() => {
    if (!editor || !isFindAndReplaceAvailable(editor)) return

    editor.commands.setReplaceTerm(replaceTerm)
    if (searchTerm) {
      editor.commands.setSearchTerm(searchTerm)
    }
  }, [editor, searchTerm, replaceTerm])

  /**
   * Clears highlights and cancels pending work while keeping the entered
   * terms, so closing the search UI does not erase non-empty field values.
   */
  const suspendSearch = useCallback(() => {
    if (!editor || !isFindAndReplaceAvailable(editor) || editor.isDestroyed) {
      return
    }
    editor.commands.clearSearch()
  }, [editor])

  /**
   * Clears the search entirely: highlights, pending work and field values.
   */
  const clearSearch = useCallback(() => {
    setSearchTermState("")
    setReplaceTermState("")

    if (!editor || !isFindAndReplaceAvailable(editor) || editor.isDestroyed) {
      return
    }
    editor.commands.clearSearch()
  }, [editor])

  // Clear highlights and cancel the pending debounced search when the search
  // UI unmounts, so no stale decorations remain without a way to clear them.
  // Unconditional on purpose: a debounced search may still be pending without
  // being reflected in the extension storage yet.
  useEffect(() => {
    if (!editor || !canSearch) return

    return () => {
      if (editor.isDestroyed) return
      editor.commands.clearSearch()
    }
  }, [editor, canSearch])

  // Keep the current result visible whenever results become available or the
  // active result changes. Deferred a frame so layout-heavy integrations
  // (e.g. paginated documents) have settled before measuring.
  useEffect(() => {
    if (total === 0 || currentIndex === null) return

    const frame = requestAnimationFrame(() => {
      scrollCurrentResultIntoView(editor, scrollIntoViewOptions)
    })

    return () => cancelAnimationFrame(frame)
  }, [editor, total, currentIndex, appliedSearchTerm, scrollIntoViewOptions])

  const isEditable = !!editor?.isEditable
  const canNavigate = canSearch && total > 0
  const hasReplaceTerm = replaceTerm.length > 0
  const canReplace =
    canSearch &&
    isEditable &&
    hasReplaceTerm &&
    total > 0 &&
    currentIndex !== null
  const canReplaceAll = canSearch && isEditable && hasReplaceTerm && total > 0

  return {
    editor,
    isVisible,
    canSearch,
    searchTerm,
    replaceTerm,
    appliedSearchTerm,
    total,
    currentIndex,
    resultCountLabel: formatResultCount(currentIndex, total),
    caseSensitive,
    wholeWord,
    useRegex,
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
    clearSearch,
    label: "Search and replace",
    shortcutKeys: SEARCH_AND_REPLACE_SHORTCUT_KEY,
    Icon: SearchIcon,
  }
}

export type UseSearchAndReplaceReturn = ReturnType<typeof useSearchAndReplace>
