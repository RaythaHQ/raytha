import { memo } from "react"
import { ChevronDownIcon } from "@/components/tiptap-icons/chevron-down-icon"

type SvgProps = React.ComponentPropsWithoutRef<"svg">

export const ChevronUpIcon = memo(
  ({ className, style, ...props }: SvgProps) => {
    return (
      <ChevronDownIcon
        className={className}
        style={{
          transform: "rotate(180deg)",
          transformOrigin: "50% 50%",
          ...style,
        }}
        {...props}
      />
    )
  }
)

ChevronUpIcon.displayName = "ChevronUpIcon"
