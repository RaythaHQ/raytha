import { Toaster as SonnerToaster, toast as sonnerToast } from "sonner";

/** Fire-and-forget toast notifications; rendered by <Toaster/> in the shell. */
export const toast = {
  success: (title: string) => sonnerToast.success(title),
  error: (title: string) => sonnerToast.error(title),
  info: (title: string) => sonnerToast.info(title),
  message: (title: string) => sonnerToast(title),
};

export function Toaster() {
  return (
    <SonnerToaster
      position="bottom-right"
      closeButton
      toastOptions={{
        classNames: {
          toast: "!rounded-xl !border-border !shadow-pop !font-sans !text-[13px]",
          title: "!font-medium",
          success: "[&_[data-icon]]:!text-success",
          error: "[&_[data-icon]]:!text-destructive",
          info: "[&_[data-icon]]:!text-brand-600",
        },
      }}
    />
  );
}
