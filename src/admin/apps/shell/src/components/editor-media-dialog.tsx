import { Dialog, DialogContent, DialogHeader, DialogTitle, FileUpload, type UploadedFile } from "@raytha/ui";

export function EditorMediaDialog({
  open,
  onOpenChange,
  title,
  allowedFileTypes,
  onUploaded,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  allowedFileTypes?: string[];
  onUploaded: (file: UploadedFile) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogHeader>
        <DialogTitle>{title}</DialogTitle>
      </DialogHeader>
      <DialogContent>
        <FileUpload
          height={280}
          allowedFileTypes={allowedFileTypes}
          onUploaded={(files) => {
            const first = files[0];
            if (first) {
              onUploaded(first);
              onOpenChange(false);
            }
          }}
        />
      </DialogContent>
    </Dialog>
  );
}
