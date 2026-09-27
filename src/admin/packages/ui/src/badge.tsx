import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";
import { cn } from "./cn";

const dot = "before:size-1.5 before:shrink-0 before:rounded-full before:bg-current before:content-['']";

const badgeVariants = cva(
  "inline-flex h-5.5 items-center gap-1.5 whitespace-nowrap rounded-md border px-2 text-xs font-medium leading-none",
  {
    variants: {
      variant: {
        default: "border-transparent bg-primary text-primary-foreground",
        secondary: "border-border bg-muted text-secondary-foreground",
        neutral: "border-border bg-muted text-secondary-foreground",
        outline: "border-border-strong bg-card text-foreground",
        success: cn("border-success-border bg-success-soft text-success", dot),
        warning: cn("border-warning-border bg-warning-soft text-warning", dot),
        info: cn("border-info-border bg-info-soft text-info", dot),
        destructive: cn("border-destructive-border bg-destructive-soft text-destructive-soft-foreground", dot),
        danger: cn("border-destructive-border bg-destructive-soft text-destructive-soft-foreground", dot),
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />;
}
