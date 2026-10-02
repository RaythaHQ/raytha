import type { Ref, TextareaHTMLAttributes } from "react";
import { cn } from "./cn";
import { fieldControl } from "./field-control";

export function Textarea({
  className,
  ...props
}: TextareaHTMLAttributes<HTMLTextAreaElement> & { ref?: Ref<HTMLTextAreaElement> }) {
  return <textarea className={cn("flex min-h-20 px-3 py-2 leading-relaxed", fieldControl, className)} {...props} />;
}
