import { javascript } from "@codemirror/lang-javascript";
import { liquid } from "@codemirror/lang-liquid";
import type { TemplateVariableGroup } from "@raytha/api";
import { cn } from "@raytha/ui";
import CodeMirror, { type ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { useEffect, useMemo, useState, type Ref } from "react";
import { liquidAutocomplete } from "./liquid-completion";

const NO_VARIABLES: TemplateVariableGroup[] = [];

export function insertAtCursor(editor: ReactCodeMirrorRef | null, text: string): void {
  const view = editor?.view;
  if (!view) {
    return;
  }
  const { from, to } = view.state.selection.main;
  view.dispatch({
    changes: { from, to, insert: text },
    selection: { anchor: from + text.length },
    scrollIntoView: true,
  });
  view.focus();
}

export function CodeEditor({
  value,
  onChange,
  language,
  editorRef,
  ariaLabel,
  variables = NO_VARIABLES,
  height = "28rem",
  className,
}: {
  value: string;
  onChange: (value: string) => void;
  language: "liquid" | "javascript";
  editorRef?: Ref<ReactCodeMirrorRef>;
  ariaLabel: string;
  variables?: TemplateVariableGroup[];
  height?: string;
  className?: string;
}) {
  const dark = useDarkClass();
  const extensions = useMemo(
    () => (language === "javascript" ? [javascript()] : [liquid(), liquidAutocomplete(variables)]),
    [language, variables],
  );

  return (
    <div className={cn("overflow-hidden rounded-lg border border-border", className)} role="group" aria-label={ariaLabel}>
      <CodeMirror
        ref={editorRef}
        value={value}
        height={height}
        theme={dark ? "dark" : "light"}
        extensions={extensions}
        onChange={onChange}
        basicSetup={{ foldGutter: true, lineNumbers: true, autocompletion: language === "javascript" }}
      />
    </div>
  );
}

function useDarkClass(): boolean {
  const [dark, setDark] = useState(() => document.documentElement.classList.contains("dark"));

  useEffect(() => {
    const root = document.documentElement;
    const sync = () => setDark(root.classList.contains("dark"));
    const observer = new MutationObserver(sync);
    observer.observe(root, { attributes: true, attributeFilter: ["class"] });
    return () => observer.disconnect();
  }, []);

  return dark;
}
