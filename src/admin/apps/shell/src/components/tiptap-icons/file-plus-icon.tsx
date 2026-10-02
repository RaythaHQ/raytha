import { memo } from "react";

type SvgProps = React.ComponentPropsWithoutRef<"svg">;

export const FilePlusIcon = memo(({ className, ...props }: SvgProps) => {
  return (
    <svg
      width="24"
      height="24"
      className={className}
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path
        fillRule="evenodd"
        clipRule="evenodd"
        d="M6 2C4.89543 2 4 2.89543 4 4V20C4 21.1046 4.89543 22 6 22H18C19.1046 22 20 21.1046 20 20V8.82843C20 8.29799 19.7893 7.78929 19.4142 7.41421L14.5858 2.58579C14.2107 2.21071 13.702 2 13.1716 2H6ZM6 4H13V8C13 8.55228 13.4477 9 14 9H18V20H6V4ZM15 5.41421L16.5858 7H15V5.41421ZM12 11C12.5523 11 13 11.4477 13 12V13H14C14.5523 13 15 13.4477 15 14C15 14.5523 14.5523 15 14 15H13V16C13 16.5523 12.5523 17 12 17C11.4477 17 11 16.5523 11 16V15H10C9.44772 15 9 14.5523 9 14C9 13.4477 9.44772 13 10 13H11V12C11 11.4477 11.4477 11 12 11Z"
        fill="currentColor"
      />
    </svg>
  );
});

FilePlusIcon.displayName = "FilePlusIcon";
