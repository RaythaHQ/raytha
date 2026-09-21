import { useRef, useState, type ChangeEvent } from "react";
import type { Editor } from "@tiptap/react";

import { FilePlusIcon } from "@/components/tiptap-icons/file-plus-icon";
import { Button } from "@/components/tiptap-ui-primitive/button";
import { useTiptapEditor } from "@/hooks/use-tiptap-editor";
import { insertFileLink, uploadEditorFile } from "@/lib/media-upload";

export function FileLinkButton({
  editor: providedEditor,
  text = "File",
}: {
  editor?: Editor | null;
  text?: string;
}) {
  const { editor } = useTiptapEditor(providedEditor);
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);

  async function onPicked(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file || !editor) {
      return;
    }

    setBusy(true);
    try {
      const uploaded = await uploadEditorFile(file);
      insertFileLink(editor, uploaded.name, uploaded.url);
    } catch (error) {
      console.error("File upload failed:", error);
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        disabled={busy || !editor?.isEditable}
        data-disabled={busy || !editor?.isEditable}
        aria-label="Upload a file as a link"
        tooltip="Upload a file as a link"
        onClick={() => inputRef.current?.click()}
      >
        <FilePlusIcon className="tiptap-button-icon" />
        {text ? <span className="tiptap-button-text">{text}</span> : null}
      </Button>
      <input ref={inputRef} type="file" hidden onChange={onPicked} />
    </>
  );
}
