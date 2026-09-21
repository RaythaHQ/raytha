"use client"

import { forwardRef, useCallback, useEffect, useRef, useState } from "react"
import { Separator } from "@/components/tiptap-ui-primitive/separator"
import "@/components/tiptap-ui-primitive/toolbar/toolbar.scss"
import { cn } from "@/lib/tiptap-utils"
import { useMenuNavigation } from "@/hooks/use-menu-navigation"
import { useComposedRef } from "@/hooks/use-composed-ref"

type BaseProps = React.HTMLAttributes<HTMLDivElement>

interface ToolbarProps extends BaseProps {
  variant?: "floating" | "fixed"
}

const useToolbarNavigation = (
  toolbarRef: React.RefObject<HTMLDivElement | null>
) => {
  const [items, setItems] = useState<HTMLElement[]>([])
  const initialContainerTabIndexRef = useRef<number | null | undefined>(
    undefined
  )

  const collectItems = useCallback(() => {
    if (!toolbarRef.current) return []
    return Array.from(
      toolbarRef.current.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [role="button"]:not([disabled]), [tabindex="0"]:not([disabled])'
      )
    )
  }, [toolbarRef])

  useEffect(() => {
    const toolbar = toolbarRef.current
    if (!toolbar) return

    if (initialContainerTabIndexRef.current === undefined) {
      initialContainerTabIndexRef.current =
        toolbar.hasAttribute("tabindex") && toolbar.tabIndex >= 0
          ? toolbar.tabIndex
          : null
    }

    const updateItems = () => setItems(collectItems())

    updateItems()
    const observer = new MutationObserver(updateItems)
    observer.observe(toolbar, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ["disabled"],
    })

    return () => observer.disconnect()
  }, [collectItems, toolbarRef])

  const { selectedIndex, setSelectedIndex } = useMenuNavigation<HTMLElement>({
    containerRef: toolbarRef,
    items,
    orientation: "horizontal",
    onSelect: (el) => el.click(),
    autoSelectFirstItem: false,
    loopOnTab: false,
  })

  useEffect(() => {
    const toolbar = toolbarRef.current
    if (!toolbar) return

    const handleFocus = (e: FocusEvent) => {
      const target = e.target as HTMLElement
      if (toolbar.contains(target)) {
        target.setAttribute("data-focus-visible", "true")
        const itemIndex = items.indexOf(target)
        if (itemIndex >= 0) setSelectedIndex(itemIndex)
      }
    }

    const handleBlur = (e: FocusEvent) => {
      const target = e.target as HTMLElement
      if (toolbar.contains(target)) target.removeAttribute("data-focus-visible")

      const nextTarget = e.relatedTarget
      const NodeConstructor = toolbar.ownerDocument.defaultView?.Node
      const leavesToolbar =
        !nextTarget ||
        !NodeConstructor ||
        !(nextTarget instanceof NodeConstructor) ||
        !toolbar.contains(nextTarget)

      if (leavesToolbar && initialContainerTabIndexRef.current != null) {
        queueMicrotask(() => {
          if (
            toolbar.isConnected &&
            !toolbar.contains(toolbar.ownerDocument.activeElement)
          ) {
            toolbar.tabIndex = initialContainerTabIndexRef.current!
            setSelectedIndex(-1)
          }
        })
      }
    }

    toolbar.addEventListener("focus", handleFocus, true)
    toolbar.addEventListener("blur", handleBlur, true)

    return () => {
      toolbar.removeEventListener("focus", handleFocus, true)
      toolbar.removeEventListener("blur", handleBlur, true)
    }
  }, [items, setSelectedIndex, toolbarRef])

  useEffect(() => {
    if (!items.length) return

    const toolbar = toolbarRef.current
    if (!toolbar) return

    const initialContainerTabIndex = initialContainerTabIndexRef.current
    const currentIndex = selectedIndex ?? -1

    if (initialContainerTabIndex != null) {
      items.forEach((item) => {
        item.tabIndex = -1
      })

      if (currentIndex >= 0 && items[currentIndex]) {
        toolbar.tabIndex = -1
        if (toolbar.contains(toolbar.ownerDocument.activeElement)) {
          items[currentIndex].focus()
        }
      } else {
        toolbar.tabIndex = initialContainerTabIndex
      }
      return
    }

    const existingTabStop = items.findIndex((item) => item.tabIndex === 0)
    const activeIndex =
      currentIndex >= 0 && items[currentIndex]
        ? currentIndex
        : existingTabStop >= 0
          ? existingTabStop
          : 0

    items.forEach((item, index) => {
      item.tabIndex = index === activeIndex ? 0 : -1
    })

    // Only restore focus while it is inside the toolbar. The item list also
    // refreshes when focus legitimately lives elsewhere (e.g. a popover
    // opened from the toolbar autofocused its input), and stealing focus
    // back would dismiss such overlays.
    if (
      currentIndex >= 0 &&
      toolbar.contains(toolbar.ownerDocument.activeElement)
    ) {
      items[activeIndex]?.focus()
    }
  }, [selectedIndex, items, toolbarRef])
}

export const Toolbar = forwardRef<HTMLDivElement, ToolbarProps>(
  ({ children, className, variant = "fixed", ...props }, ref) => {
    const toolbarRef = useRef<HTMLDivElement>(null)
    const composedRef = useComposedRef(toolbarRef, ref)
    useToolbarNavigation(toolbarRef)

    return (
      <div
        ref={composedRef}
        role="toolbar"
        aria-label="toolbar"
        data-variant={variant}
        className={cn("tiptap-toolbar", className)}
        {...props}
      >
        {children}
      </div>
    )
  }
)
Toolbar.displayName = "Toolbar"

export const ToolbarGroup = forwardRef<HTMLDivElement, BaseProps>(
  ({ children, className, ...props }, ref) => (
    <div
      ref={ref}
      role="group"
      className={cn("tiptap-toolbar-group", className)}
      {...props}
    >
      {children}
    </div>
  )
)
ToolbarGroup.displayName = "ToolbarGroup"

export const ToolbarSeparator = forwardRef<HTMLDivElement, BaseProps>(
  ({ ...props }, ref) => (
    <Separator ref={ref} orientation="vertical" decorative {...props} />
  )
)
ToolbarSeparator.displayName = "ToolbarSeparator"
