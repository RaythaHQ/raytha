import { Toaster as SonnerToaster, toast as sonnerToast } from "sonner";

/** Fire-and-forget toast notifications; rendered by <Toaster/> in the shell. */
export const toast = {
  success: (title: string) => sonnerToast.success(title),
  error: (title: string) => sonnerToast.error(title),
  info: (title: string) => sonnerToast.info(title),
  message: (title: string) => sonnerToast(title),
};

export function Toaster() {
  return <SonnerToaster richColors position="bottom-right" closeButton />;
}
