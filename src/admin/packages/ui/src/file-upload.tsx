import { adminApi, apiFetch, formatError } from "@raytha/api";
import Uppy from "@uppy/core";
import Dashboard from "@uppy/react/dashboard";
import { useEffect, useRef, useState } from "react";

import "@uppy/core/css/style.min.css";
import "@uppy/dashboard/css/style.min.css";
import "./file-upload.css";

export interface UploadedFile {
  id: string;
  objectKey: string;
  name: string;
  type: string;
  size: number;
  url: string;
}

export interface FileUploadProps {
  onUploaded?: (files: UploadedFile[]) => void;
  /** Override the org setting. When omitted, configuration is fetched. */
  maxFileSizeBytes?: number;
  allowedFileTypes?: string[];
  height?: number;
  note?: string;
  /** When omitted, `/raytha/api/configuration` is consulted. */
  useDirectUploadToCloud?: boolean;
  themeId?: string;
}

interface PresignResponse {
  url: string;
  fields: {
    id: string;
    fileName: string;
    contentType: string;
    objectKey: string;
  };
}

interface LocalUploadResponse {
  url?: string;
  location?: string;
  success?: boolean;
  fields?: {
    id: string;
    fileName: string;
    contentType: string;
    objectKey: string;
  };
}

/**
 * Drag & drop uploader (Uppy Dashboard) that talks to Raytha media endpoints.
 * Cloud: POST /raytha/media-items/presign → PUT → POST create-after-upload.
 * Local: POST /raytha/media-items/upload as XHR FormData.
 */
export function FileUpload({
  onUploaded,
  maxFileSizeBytes,
  allowedFileTypes,
  height = 300,
  note,
  useDirectUploadToCloud,
  themeId,
}: FileUploadProps) {
  const [uppy, setUppy] = useState<Uppy | null>(null);
  const [fetchedLimitBytes, setFetchedLimitBytes] = useState<number | null>(null);
  const [cloudMode, setCloudMode] = useState<boolean | null>(useDirectUploadToCloud ?? null);
  const limitBytes = maxFileSizeBytes ?? fetchedLimitBytes;

  const onUploadedRef = useRef(onUploaded);
  useEffect(() => {
    onUploadedRef.current = onUploaded;
  });

  useEffect(() => {
    if (useDirectUploadToCloud != null && maxFileSizeBytes != null) {
      return;
    }

    let cancelled = false;
    adminApi.configuration
      .get()
      .then((data) => {
        if (cancelled) {
          return;
        }
        if (useDirectUploadToCloud == null) {
          setCloudMode(data.useDirectUploadToCloud);
        }
        if (maxFileSizeBytes == null) {
          setFetchedLimitBytes(data.maxUploadBytes ?? 100 * 1024 * 1024);
        }
      })
      .catch(() => {
        if (cancelled) {
          return;
        }
        if (useDirectUploadToCloud == null) {
          setCloudMode(false);
        }
        if (maxFileSizeBytes == null) {
          setFetchedLimitBytes(100 * 1024 * 1024);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [maxFileSizeBytes, useDirectUploadToCloud]);

  const allowedTypesKey = allowedFileTypes?.join(",") ?? "";

  useEffect(() => {
    if (limitBytes == null || cloudMode == null) {
      return;
    }

    const allowed = allowedTypesKey ? allowedTypesKey.split(",") : null;
    const instance = new Uppy({
      restrictions: {
        maxFileSize: limitBytes,
        allowedFileTypes: allowed,
      },
    });

    instance.addUploader(async (fileIDs) => {
      await Promise.all(
        fileIDs.map(async (id) => {
          try {
            if (cloudMode) {
              await uploadViaPresign(instance, id, themeId);
            } else {
              await uploadLocal(instance, id, themeId);
            }
          } catch (error) {
            const file = instance.getFile(id);
            const err = new Error(formatUploadError(error));
            if (file) {
              instance.emit("upload-error", file, err);
            }
          }
        }),
      );
    });

    instance.on("complete", (result: { successful?: { name?: string; type?: string; size?: number | null; meta: Record<string, unknown> }[] }) => {
      const files = (result.successful ?? []).map((file) => ({
        id: String(file.meta.mediaId ?? ""),
        objectKey: String(file.meta.objectKey ?? ""),
        name: file.name ?? "",
        type: file.type ?? "",
        size: file.size ?? 0,
        url: String(file.meta.mediaUrl ?? mediaUrl(String(file.meta.objectKey ?? ""))),
      }));
      if (files.length > 0) {
        onUploadedRef.current?.(files);
      }
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
  }, [limitBytes, allowedTypesKey, cloudMode, themeId]);

  if (!uppy || limitBytes == null || cloudMode == null) {
    return null;
  }

  const limitNote = `Maximum file size is ${formatMegabytes(limitBytes)}.`;
  const dashboardNote = note ? `${note} ${limitNote}` : limitNote;

  return (
    <div className="raytha-file-upload">
      <Dashboard uppy={uppy} height={height} note={dashboardNote} proudlyDisplayPoweredByUppy={false} />
    </div>
  );
}

function mediaUrl(objectKey: string): string {
  return `/raytha/media-items/objectkey/${encodeURIComponent(objectKey)}`;
}

function fileExtension(name: string): string {
  const index = name.lastIndexOf(".");
  return index >= 0 ? name.slice(index) : "";
}

function formatMegabytes(bytes: number): string {
  const megabytes = bytes / (1024 * 1024);
  const rounded = megabytes >= 1 ? megabytes.toFixed(0) : megabytes.toFixed(2).replace(/\.?0+$/, "");
  return `${rounded} MB`;
}

function formatUploadError(error: unknown): string {
  const message = formatError(error);
  if (error instanceof Error && "status" in error && (error as { status?: number }).status === 413) {
    return message.includes("maximum upload size") ? message : "File exceeds the maximum upload size.";
  }
  if (/upload failed \(413\)/i.test(message)) {
    return "File exceeds the maximum upload size.";
  }
  return message;
}

async function uploadViaPresign(instance: Uppy, fileId: string, themeId?: string): Promise<void> {
  const file = instance.getFile(fileId);
  if (!file) {
    return;
  }

  instance.emit("upload-start", [file]);

  const data = await apiFetch<PresignResponse>("/raytha/media-items/presign", {
    method: "POST",
    body: JSON.stringify({
      filename: file.name,
      contentType: file.type || "application/octet-stream",
      extension: fileExtension(file.name ?? ""),
    }),
  });

  instance.setFileMeta(fileId, {
    mediaId: data.fields.id,
    objectKey: data.fields.objectKey,
    mediaUrl: mediaUrl(data.fields.objectKey),
  });

  if (!(file.data instanceof Blob)) {
    throw new Error("File data is not available");
  }

  await putBlob(data.url, "PUT", { "x-ms-blob-type": "BlockBlob" }, file.data, (loaded, total) => {
    const current = instance.getFile(fileId);
    if (!current) {
      return;
    }
    instance.emit("upload-progress", current, {
      uploadStarted: current.progress.uploadStarted ?? Date.now(),
      bytesUploaded: loaded,
      bytesTotal: total,
    });
  });

  const createUrl = themeId
    ? `/raytha/media-items/create-after-upload?themeId=${encodeURIComponent(themeId)}`
    : "/raytha/media-items/create-after-upload";
  await apiFetch<{ success?: boolean }>(createUrl, {
    method: "POST",
    body: JSON.stringify({
      id: data.fields.id,
      filename: file.name,
      contentType: file.type || "application/octet-stream",
      extension: fileExtension(file.name ?? ""),
      objectKey: data.fields.objectKey,
      length: file.size ?? 0,
    }),
  });

  const uploaded = instance.getFile(fileId);
  if (uploaded) {
    instance.emit("upload-success", uploaded, { status: 200 });
  }
}

async function uploadLocal(instance: Uppy, fileId: string, themeId?: string): Promise<void> {
  const file = instance.getFile(fileId);
  if (!file) {
    return;
  }

  instance.emit("upload-start", [file]);

  if (!(file.data instanceof Blob)) {
    throw new Error("File data is not available");
  }

  const form = new FormData();
  form.append("file", file.data, file.name);

  const endpoint = themeId
    ? `/raytha/media-items/upload?themeId=${encodeURIComponent(themeId)}`
    : "/raytha/media-items/upload";

  const data = await postForm<LocalUploadResponse>(endpoint, form, (loaded, total) => {
    const current = instance.getFile(fileId);
    if (!current) {
      return;
    }
    instance.emit("upload-progress", current, {
      uploadStarted: current.progress.uploadStarted ?? Date.now(),
      bytesUploaded: loaded,
      bytesTotal: total,
    });
  });

  const fields = data.fields;
  const objectKey = fields?.objectKey ?? "";
  instance.setFileMeta(fileId, {
    mediaId: fields?.id ?? "",
    objectKey,
    mediaUrl: data.url ?? data.location ?? mediaUrl(objectKey),
  });

  const uploaded = instance.getFile(fileId);
  if (uploaded) {
    instance.emit("upload-success", uploaded, { status: 200 });
  }
}

function putBlob(
  url: string,
  method: string,
  headers: Record<string, string>,
  body: Blob,
  onProgress: (loaded: number, total: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open(method, url);
    for (const [key, value] of Object.entries(headers)) {
      xhr.setRequestHeader(key, value);
    }
    xhr.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) {
        onProgress(event.loaded, event.total);
      }
    });
    xhr.addEventListener("load", () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
        return;
      }
      if (xhr.status === 413) {
        reject(new Error(readOversizeMessage(xhr.responseText)));
        return;
      }
      reject(new Error(`Upload failed (${xhr.status})`));
    });
    xhr.addEventListener("error", () => reject(new Error("Upload failed")));
    xhr.addEventListener("abort", () => reject(new Error("Upload cancelled")));
    xhr.send(body);
  });
}

function postForm<T>(
  url: string,
  body: FormData,
  onProgress: (loaded: number, total: number) => void,
): Promise<T> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", url);
    xhr.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) {
        onProgress(event.loaded, event.total);
      }
    });
    xhr.addEventListener("load", () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          resolve(JSON.parse(xhr.responseText) as T);
        } catch {
          reject(new Error("Upload succeeded but the response was not JSON."));
        }
        return;
      }
      if (xhr.status === 413) {
        reject(new Error(readOversizeMessage(xhr.responseText)));
        return;
      }
      reject(new Error(`Upload failed (${xhr.status})`));
    });
    xhr.addEventListener("error", () => reject(new Error("Upload failed")));
    xhr.addEventListener("abort", () => reject(new Error("Upload cancelled")));
    xhr.send(body);
  });
}

function readOversizeMessage(body: string): string {
  try {
    const problem = JSON.parse(body) as { detail?: string; error?: string };
    if (problem.detail || problem.error) {
      return problem.detail ?? problem.error ?? "File exceeds the maximum upload size.";
    }
  } catch {
    // Fall through to the generic oversize message.
  }
  return "File exceeds the maximum upload size.";
}
