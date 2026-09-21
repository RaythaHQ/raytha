import { adminApi, apiFetch } from "@raytha/api";
import type { Editor } from "@tiptap/react";

export const EDITOR_MAX_FILE_SIZE = 25 * 1024 * 1024;

export interface UploadedEditorFile {
  id: string;
  objectKey: string;
  name: string;
  url: string;
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
  fields?: {
    id: string;
    fileName: string;
    contentType: string;
    objectKey: string;
  };
}

export function fileDownloadUrl(objectKey: string): string {
  return `/raytha/media-items/objectkey/${encodeURIComponent(objectKey)}`;
}

function fileExtension(name: string): string {
  const index = name.lastIndexOf(".");
  return index >= 0 ? name.slice(index) : "";
}

export async function uploadEditorFile(
  file: File,
  onProgress?: (event: { progress: number }) => void,
  abortSignal?: AbortSignal,
): Promise<UploadedEditorFile> {
  if (!file) {
    throw new Error("No file provided");
  }

  if (file.size > EDITOR_MAX_FILE_SIZE) {
    throw new Error(`File size exceeds maximum allowed (${EDITOR_MAX_FILE_SIZE / (1024 * 1024)}MB)`);
  }

  if (abortSignal?.aborted) {
    throw new Error("Upload cancelled");
  }

  const config = await adminApi.configuration.get().catch(() => ({ useDirectUploadToCloud: false }));

  if (config.useDirectUploadToCloud) {
    const data = await apiFetch<PresignResponse>("/raytha/media-items/presign", {
      method: "POST",
      body: JSON.stringify({
        filename: file.name,
        contentType: file.type || "application/octet-stream",
        extension: fileExtension(file.name),
      }),
    });

    await putBlob(data.url, "PUT", { "x-ms-blob-type": "BlockBlob" }, file, (loaded, total) => {
      if (total > 0) {
        onProgress?.({ progress: Math.round((loaded / total) * 100) });
      }
    }, abortSignal);

    await apiFetch("/raytha/media-items/create-after-upload", {
      method: "POST",
      body: JSON.stringify({
        id: data.fields.id,
        filename: file.name,
        contentType: file.type || "application/octet-stream",
        extension: fileExtension(file.name),
        objectKey: data.fields.objectKey,
        length: file.size,
      }),
    });

    return {
      id: data.fields.id,
      objectKey: data.fields.objectKey,
      name: file.name,
      url: fileDownloadUrl(data.fields.objectKey),
    };
  }

  const form = new FormData();
  form.append("file", file);
  const uploaded = await postForm<LocalUploadResponse>("/raytha/media-items/upload", form, (loaded, total) => {
    if (total > 0) {
      onProgress?.({ progress: Math.round((loaded / total) * 100) });
    }
  }, abortSignal);

  const objectKey = uploaded.fields?.objectKey ?? "";
  return {
    id: uploaded.fields?.id ?? "",
    objectKey,
    name: file.name,
    url: uploaded.url ?? uploaded.location ?? fileDownloadUrl(objectKey),
  };
}

export function insertFileLink(editor: Editor, name: string, url: string, pos?: number): void {
  const node = {
    type: "text" as const,
    text: name,
    marks: [
      {
        type: "link",
        attrs: { href: url, target: "_blank", rel: "noopener noreferrer" },
      },
    ],
  };

  const chain = editor.chain().focus();
  if (typeof pos === "number") {
    chain.insertContentAt(pos, node).run();
    return;
  }
  chain.insertContent(node).run();
}

export async function ingestEditorFiles(editor: Editor, files: File[], pos?: number): Promise<void> {
  for (const file of files) {
    const uploaded = await uploadEditorFile(file);
    if (file.type.startsWith("image/")) {
      const image = { type: "image", attrs: { src: uploaded.url, alt: uploaded.name } };
      if (typeof pos === "number") {
        editor.chain().focus().insertContentAt(pos, image).run();
      } else {
        editor.chain().focus().setImage({ src: uploaded.url, alt: uploaded.name }).run();
      }
    } else {
      insertFileLink(editor, uploaded.name, uploaded.url, pos);
    }
  }
}

function putBlob(
  url: string,
  method: string,
  headers: Record<string, string>,
  body: Blob,
  onProgress: (loaded: number, total: number) => void,
  abortSignal?: AbortSignal,
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
      } else {
        reject(new Error(`Upload failed (${xhr.status})`));
      }
    });
    xhr.addEventListener("error", () => reject(new Error("Upload failed")));
    xhr.addEventListener("abort", () => reject(new Error("Upload cancelled")));
    abortSignal?.addEventListener("abort", () => xhr.abort());
    xhr.send(body);
  });
}

function postForm<T>(
  url: string,
  body: FormData,
  onProgress: (loaded: number, total: number) => void,
  abortSignal?: AbortSignal,
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
      } else {
        reject(new Error(`Upload failed (${xhr.status})`));
      }
    });
    xhr.addEventListener("error", () => reject(new Error("Upload failed")));
    xhr.addEventListener("abort", () => reject(new Error("Upload cancelled")));
    abortSignal?.addEventListener("abort", () => xhr.abort());
    xhr.send(body);
  });
}

/** @deprecated Use uploadEditorFile. Kept so copied TipTap imports stay valid. */
export const EDITOR_PRODUCT_KEY = "raytha";
