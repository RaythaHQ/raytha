import { SimpleEditor } from "@/components/tiptap-templates/simple/simple-editor";
import type { Editor } from "@tiptap/react";
import { useCallback } from "react";

export interface RichTextEditorProps {
  /** Initial HTML. The editor owns the document after mount. */
  content?: string;
  onHtmlChange?: (html: string) => void;
  /** Image/file uploads hit platform storage. Off for email (clients cannot fetch those URLs). */
  enableUploads?: boolean;
  ariaLabel?: string;
  onReady?: (api: RichTextEditorApi) => void;
}

export interface RichTextEditorApi {
  insertText: (text: string) => void;
}

/** Form-facing wrapper around the TipTap Simple Editor used on Maintenance. */
export function RichTextEditor({
  content = "",
  onHtmlChange,
  enableUploads = true,
  ariaLabel,
  onReady,
}: RichTextEditorProps) {
  const handleEditor = useCallback(
    (editor: Editor | null) => {
      if (!editor) {
        onReady?.({ insertText: () => undefined });
        return;
      }
      onReady?.({
        insertText: (text) => {
          editor.chain().focus().insertContent(text).run();
        },
      });
    },
    [onReady],
  );

  return (
    <SimpleEditor
      content={content}
      enableUploads={enableUploads}
      ariaLabel={ariaLabel}
      onEditor={handleEditor}
      onUpdate={({ html }) => onHtmlChange?.(html)}
    />
  );
}
