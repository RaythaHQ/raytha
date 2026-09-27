import { toast } from "@raytha/ui";

export async function copyText(text: string, confirmation = "Copied to clipboard"): Promise<void> {
  try {
    if (window.isSecureContext && navigator.clipboard) {
      await navigator.clipboard.writeText(text);
    } else {
      copyWithSelection(text);
    }
    toast.success(confirmation);
  } catch {
    toast.error("Could not copy. Select the text and copy it by hand.");
  }
}

/** The async Clipboard API only exists on secure origins; dev over a Tailscale IP is plain http. */
function copyWithSelection(text: string): void {
  const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null;
  const area = document.createElement("textarea");
  area.value = text;
  area.setAttribute("readonly", "");
  area.style.position = "fixed";
  area.style.opacity = "0";
  document.body.append(area);
  area.select();
  const copied = document.execCommand("copy");
  area.remove();
  previous?.focus();
  if (!copied) {
    throw new Error("Copy command was rejected");
  }
}
