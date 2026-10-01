import Uppy from "@uppy/core";
import Dashboard from "@uppy/react/dashboard";
import { useEffect, useRef, useState } from "react";

import "@uppy/core/css/style.min.css";
import "@uppy/dashboard/css/style.min.css";
import "./file-upload.css";

export interface FileDropProps {
  /** Called with the chosen file, or null once it is removed. Nothing is sent to the server. */
  onFileChange: (file: File | null) => void;
  /** Extensions (`.json`) or MIME types, as Uppy's `allowedFileTypes`. */
  allowedFileTypes?: string[];
  maxFileSizeBytes?: number;
  height?: number;
  note?: string;
}

/**
 * Uppy Dashboard that holds one file in the browser for the page to read, as opposed to
 * {@link FileUpload}, which stores files as media. Choosing a second file replaces the first.
 */
export function FileDrop({ onFileChange, allowedFileTypes, maxFileSizeBytes, height = 240, note }: FileDropProps) {
  const [uppy, setUppy] = useState<Uppy | null>(null);

  const onFileChangeRef = useRef(onFileChange);
  useEffect(() => {
    onFileChangeRef.current = onFileChange;
  });

  const allowedTypesKey = allowedFileTypes?.join(",") ?? "";

  useEffect(() => {
    const instance = new Uppy({
      autoProceed: false,
      restrictions: {
        maxNumberOfFiles: 1,
        maxFileSize: maxFileSizeBytes ?? null,
        allowedFileTypes: allowedTypesKey ? allowedTypesKey.split(",") : null,
      },
    });

    instance.on("file-added", (file) => {
      onFileChangeRef.current(file.data instanceof File ? file.data : null);
    });
    instance.on("file-removed", () => {
      onFileChangeRef.current(null);
    });

    // Managing an external-system instance: the state only carries the Uppy
    // object into render, so the one extra render is intentional.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setUppy(instance);
    return () => {
      document.body.classList.remove("uppy-Dashboard-isFixed");
      setUppy(null);
      instance.destroy();
    };
  }, [allowedTypesKey, maxFileSizeBytes]);

  if (!uppy) {
    return null;
  }

  return (
    <div className="raytha-file-upload">
      <Dashboard
        uppy={uppy}
        height={height}
        note={note}
        hideUploadButton
        proudlyDisplayPoweredByUppy={false}
      />
    </div>
  );
}
