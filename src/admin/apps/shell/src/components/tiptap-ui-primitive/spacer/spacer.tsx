"use client"

export type SpacerOrientation = "horizontal" | "vertical"

export function Spacer({
  orientation = "horizontal",
  size,
  style = {},
  ...props
}: React.ComponentProps<"div"> & {
  orientation?: SpacerOrientation
  size?: string | number
}) {
  const computedStyle = {
    ...style,
    ...(orientation === "horizontal" && !size && { flex: 1 }),
    ...(size && {
      width: orientation === "vertical" ? "1px" : size,
      height: orientation === "horizontal" ? "1px" : size,
    }),
  }

  return <div {...props} style={computedStyle} />
}
