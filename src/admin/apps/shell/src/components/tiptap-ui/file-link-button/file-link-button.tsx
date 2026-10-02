import { useState } from "react";
import type { Editor } from "@tiptap/react";

import { FilePlusIcon } from "@/components/tiptap-icons/file-plus-icon";
import { Button } from "@/components/tiptap-ui-primitive/button";
import { EditorMediaDialog } from "@/components/editor-media-dialog";
import { useTiptapEditor } from "@/hooks/use-tiptap-editor";
import { insertFileLink } from "@/lib/media-upload";

export function FileLinkButton({
  editor: providedEditor,
  text = "File",
}: {
  editor?: Editor | null;
  text?: string;
}) {
  const { editor } = useTiptapEditor(providedEditor);
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        disabled={!editor?.isEditable}
        data-disabled={!editor?.isEditable}
        aria-label="Upload a file as a link"
        tooltip="Upload a file as a link"
        onClick={() => setOpen(true)}
      >
        <FilePlusIcon className="tiptap-button-icon" />
        {text ? <span className="tiptap-button-text">{text}</span> : null}
      </Button>
      <EditorMediaDialog
        open={open}
        onOpenChange={setOpen}
        title="Insert file"
        onUploaded={(file) => {
          if (editor) {
            insertFileLink(editor, file.name, file.url);
          }
        }}
      />
    </>
  );
}
