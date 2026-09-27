import type { InputHTMLAttributes } from "react";
import { cn } from "./cn";
import { fieldControl } from "./field-control";

export function Input({ className, type, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input type={type} className={cn("flex h-9 px-3 py-1", fieldControl, className)} {...props} />;
}
