"use client"

import { cn } from "@/lib/tiptap-utils"
import "./textarea.scss"

function Textarea({ className, ...props }: React.ComponentProps<"textarea">) {
  return (
    <textarea
      data-slot="textarea"
      className={cn("textarea", className)}
      {...props}
    />
  )
}

export { Textarea }
