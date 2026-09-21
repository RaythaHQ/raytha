import { javascript } from "@codemirror/lang-javascript";
import { liquid } from "@codemirror/lang-liquid";
import CodeMirror, { type ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { useEffect, useMemo, useState, type Ref } from "react";

export function insertAtCursor(editor: ReactCodeMirrorRef | null, text: string): void {
  const view = editor?.view;
  if (!view) {
    return;
  }
  const { from, to } = view.state.selection.main;
  view.dispatch({
    changes: { from, to, insert: text },
    selection: { anchor: from + text.length },
  });
  view.focus();
}

export function CodeEditor({
  value,
  onChange,
  language,
  editorRef,
  ariaLabel,
}: {
  value: string;
  onChange: (value: string) => void;
  language: "liquid" | "javascript";
  editorRef?: Ref<ReactCodeMirrorRef>;
  ariaLabel: string;
}) {
  const dark = useDarkClass();
  const extensions = useMemo(() => (language === "javascript" ? [javascript()] : [liquid()]), [language]);

  return (
    <div className="overflow-hidden rounded-lg border border-border" role="group" aria-label={ariaLabel}>
      <CodeMirror
        ref={editorRef}
        value={value}
        height="28rem"
        theme={dark ? "dark" : "light"}
        extensions={extensions}
        onChange={onChange}
        basicSetup={{ foldGutter: true, lineNumbers: true }}
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
