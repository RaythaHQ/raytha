import type { CsvImportMethod } from "@raytha/api";
import { CSV_IMPORT_METHODS } from "@raytha/api";

export function parseCsvImportMethod(value: string): CsvImportMethod | null {
  for (const method of CSV_IMPORT_METHODS) {
    if (method === value) {
      return method;
    }
  }
  return null;
}

export function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result;
      if (typeof result !== "string") {
        reject(new Error("Could not read the file."));
        return;
      }
      const comma = result.indexOf(",");
      resolve(comma >= 0 ? result.slice(comma + 1) : result);
    };
    reader.onerror = () => reject(new Error("Could not read the file."));
    reader.readAsDataURL(file);
  });
}

export function importMethodLabel(method: CsvImportMethod): string {
  if (method === "add_new_records_only") {
    return "Add new records only";
  }
  if (method === "update_existing_records_only") {
    return "Update existing records only";
  }
  if (method === "upsert_all_records") {
    return "Upsert all records";
  }
  const _exhaustive: never = method;
  return _exhaustive;
}
