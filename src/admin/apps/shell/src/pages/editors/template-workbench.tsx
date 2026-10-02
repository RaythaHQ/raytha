import type { TemplateVariableGroup } from "@raytha/api";
import { Button, Tabs, TabsList, TabsTrigger } from "@raytha/ui";
import type { ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { FileCode2, PanelRightClose, PanelRightOpen } from "lucide-react";
import { useCallback, useEffect, useId, useRef, useState, type CSSProperties } from "react";
import { CodeEditor, insertAtCursor } from "./code-editor";
import { ThemeAssetsPanel } from "./theme-assets";
import { VariablesPanel, type SearchShortcut } from "./variables-panel";

type PanelTab = "variables" | "assets";

const EDITOR_HEIGHT = "clamp(28rem, 68vh, 48rem)";
const PANEL_STORAGE_KEY = "raytha.template-editor.panel";
const isMac = typeof navigator !== "undefined" && /Mac|iPhone|iPad/.test(navigator.platform);
const shortcut: SearchShortcut = isMac
  ? { label: "⌘⇧F", aria: "Meta+Shift+F" }
  : { label: "Ctrl⇧F", aria: "Control+Shift+F" };

/**
 * Liquid editor with a collapsible side panel. The panel shows Variables when `variables` is given
 * and theme Assets when `themeId` is given.
 */
export function TemplateWorkbench({
  value,
  onChange,
  ariaLabel,
  title,
  variables,
  themeId,
  onSave,
}: {
  value: string;
  onChange: (value: string) => void;
  ariaLabel: string;
  title: string;
  variables?: TemplateVariableGroup[];
  themeId?: string;
  onSave?: () => void;
}) {
  const editorRef = useRef<ReactCodeMirrorRef>(null);
  const panelId = useId();
  const searchIds: Record<PanelTab, string> = { variables: useId(), assets: useId() };
  const tabs: PanelTab[] = [...(variables ? ["variables" as const] : []), ...(themeId ? ["assets" as const] : [])];
  const [open, setOpen] = usePanelPreference();
  const [chosenTab, setTab] = useState<PanelTab>(tabs[0] ?? "variables");
  const tab = tabs.includes(chosenTab) ? chosenTab : (tabs[0] ?? "variables");
  const hasPanel = tabs.length > 0;
  const searchId = searchIds[tab];

  useEffect(() => {
    if (!hasPanel) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.shiftKey && event.key.toLowerCase() === "f") {
        event.preventDefault();
        setOpen(true);
        requestAnimationFrame(() => document.getElementById(searchId)?.focus());
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [hasPanel, searchId, setOpen]);

  const saveRef = useRef(onSave);
  useEffect(() => {
    saveRef.current = onSave;
  }, [onSave]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && !event.shiftKey && event.key.toLowerCase() === "s" && saveRef.current) {
        event.preventDefault();
        saveRef.current();
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, []);

  const insert = (text: string) => insertAtCursor(editorRef.current, text);
  const focusEditor = () => editorRef.current?.view?.focus();

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card shadow-card">
      <div className="flex h-11 items-center gap-3 border-b border-border pl-4 pr-2">
        <FileCode2 className="size-4 shrink-0 text-muted-foreground" aria-hidden />
        <span className="min-w-0 truncate text-sm font-medium">{title}</span>
        <span className="ml-auto hidden items-center gap-1.5 text-xs text-muted-foreground md:flex">
          Type <kbd className="rounded border border-border bg-muted px-1 font-mono text-[11px]">{"{{"}</kbd> for
          variables, <kbd className="rounded border border-border bg-muted px-1 font-mono text-[11px]">{"{%"}</kbd> for
          tags, <kbd className="rounded border border-border bg-muted px-1 font-mono text-[11px]">|</kbd> for filters
        </span>
        {hasPanel ? (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="ml-auto size-8 md:ml-2"
            aria-expanded={open}
            aria-controls={panelId}
            aria-label={open ? "Hide side panel" : "Show side panel"}
            title={open ? "Hide side panel" : "Show side panel"}
            onClick={() => setOpen(!open)}
          >
            {open ? <PanelRightClose aria-hidden /> : <PanelRightOpen aria-hidden />}
          </Button>
        ) : null}
      </div>
      <div className="flex flex-col lg:flex-row">
        <CodeEditor
          editorRef={editorRef}
          value={value}
          onChange={onChange}
          language="liquid"
          ariaLabel={ariaLabel}
          variables={variables}
          height={EDITOR_HEIGHT}
          className="min-w-0 flex-1 rounded-none border-0"
        />
        {hasPanel && open ? (
          <aside
            id={panelId}
            aria-label="Template helpers"
            className="flex h-96 flex-col border-t border-border lg:h-[var(--editor-height)] lg:w-80 lg:shrink-0 lg:border-l lg:border-t-0"
            style={{ "--editor-height": EDITOR_HEIGHT } as CSSProperties}
          >
            {tabs.length > 1 ? (
              <Tabs value={tab} onValueChange={(next) => setTab(next === "assets" ? "assets" : "variables")}>
                <div className="border-b border-border p-2">
                  <TabsList className="grid w-full grid-cols-2" aria-label="Side panel">
                    <TabsTrigger value="variables">Variables</TabsTrigger>
                    <TabsTrigger value="assets">Assets</TabsTrigger>
                  </TabsList>
                </div>
              </Tabs>
            ) : (
              <p className="border-b border-border px-3 py-2.5 text-sm font-medium">
                {tab === "variables" ? "Variables" : "Assets"}
              </p>
            )}
            {tab === "variables" && variables ? (
              <VariablesPanel
                groups={variables}
                onInsert={insert}
                searchId={searchIds.variables}
                shortcut={shortcut}
                onEscape={focusEditor}
              />
            ) : null}
            {tab === "assets" && themeId ? (
              <ThemeAssetsPanel themeId={themeId} onInsert={insert} searchId={searchIds.assets} onEscape={focusEditor} />
            ) : null}
          </aside>
        ) : null}
      </div>
    </div>
  );
}

function usePanelPreference(): [boolean, (open: boolean) => void] {
  const [open, setOpenState] = useState(() => localStorage.getItem(PANEL_STORAGE_KEY) !== "closed");
  const setOpen = useCallback((next: boolean) => {
    localStorage.setItem(PANEL_STORAGE_KEY, next ? "open" : "closed");
    setOpenState(next);
  }, []);
  return [open, setOpen];
}
