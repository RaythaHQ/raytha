"use client"

import "@/components/tiptap-ui-primitive/separator/separator.scss"
import { cn } from "@/lib/tiptap-utils"

export type Orientation = "horizontal" | "vertical"

export function Separator({
  decorative,
  orientation = "vertical",
  className,
  ...props
}: React.ComponentProps<"div"> & {
  orientation?: Orientation
  decorative?: boolean
}) {
  const ariaOrientation = orientation === "vertical" ? orientation : undefined
  const semanticProps = decorative
    ? { role: "none" }
    : { "aria-orientation": ariaOrientation, role: "separator" }

  return (
    <div
      className={cn("tiptap-separator", className)}
      data-orientation={orientation}
      {...semanticProps}
      {...props}
    />
  )
}
