export type ThemePreference = "light" | "dark" | "system";

const THEME_KEY = "raytha.theme";

export function readThemePreference(): ThemePreference {
  const stored = localStorage.getItem(THEME_KEY);
  if (stored === "light" || stored === "dark" || stored === "system") {
    return stored;
  }
  return "system";
}

export function resolvedTheme(preference: ThemePreference): "light" | "dark" {
  if (preference === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  return preference;
}

export function applyTheme(preference: ThemePreference): void {
  document.documentElement.classList.toggle("dark", resolvedTheme(preference) === "dark");
}

export function persistTheme(preference: ThemePreference): void {
  localStorage.setItem(THEME_KEY, preference);
  applyTheme(preference);
}

export function applyStoredTheme(): void {
  applyTheme(readThemePreference());
}
