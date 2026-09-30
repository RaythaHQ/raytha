import { adminApi } from "@raytha/api";
import { Button, Combobox, FormField, Skeleton, type ComboboxOption } from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { ExternalLink, FileText } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { entityFields, readString } from "../entity";
import type { RelationshipField } from "./fields-model";

const SEARCH_DEBOUNCE_MS = 250;
const PAGE_SIZE = 10;

function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

function useRelatedDeveloperName(relatedContentTypeId: string): string {
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
    enabled: relatedContentTypeId.length > 0,
  });
  const related = typesQuery.data?.items.find((item) => item.id === relatedContentTypeId);
  return related ? readString(entityFields(related), "developerName") : "";
}

/** Debounced server search over the related content type; an empty query lists the first page. */
export function useRelatedItemSearch(relatedContentTypeId: string, query: string) {
  const developerName = useRelatedDeveloperName(relatedContentTypeId);
  const search = useDebouncedValue(query.trim(), SEARCH_DEBOUNCE_MS);
  const itemsQuery = useQuery({
    queryKey: ["content-items", developerName, "related-search", search],
    queryFn: () => adminApi.contentItems(developerName).list({ search: search || undefined, pageSize: PAGE_SIZE }),
    enabled: developerName.length > 0,
    placeholderData: keepPreviousData,
  });
  const options: ComboboxOption[] = (itemsQuery.data?.items ?? []).map((item) => ({
    value: item.id,
    label: readString(entityFields(item), "primaryField") || item.id,
  }));
  return {
    developerName,
    options,
    loading: developerName.length === 0 || itemsQuery.isFetching || search !== query.trim(),
  };
}

/** A related item's label linking to its editor, plus a button that opens it in a new tab. */
export function RelatedItemLink({ field, id, label }: { field: RelationshipField; id: string; label: string }) {
  const developerName = useRelatedDeveloperName(field.relatedContentTypeId);
  if (!developerName) {
    return <span>{label}</span>;
  }
  return (
    <span className="inline-flex max-w-full items-center gap-1">
      <Link
        to="/content/$developerName/items/$id"
        params={{ developerName, id }}
        className="truncate text-primary hover:underline"
      >
        {label}
      </Link>
      <Link
        to="/content/$developerName/items/$id"
        params={{ developerName, id }}
        target="_blank"
        rel="noreferrer"
        title="Open in a new tab"
        className="inline-flex size-6 shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
      >
        <ExternalLink className="size-3.5" aria-hidden />
        <span className="sr-only">Open {label} in a new tab</span>
      </Link>
    </span>
  );
}

export function RelationshipPicker({
  field,
  value,
  onChange,
}: {
  field: RelationshipField;
  value: string;
  onChange: (next: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [picked, setPicked] = useState<ComboboxOption | null>(null);
  const focusOnMount = useRef(false);
  const { developerName, options, loading } = useRelatedItemSearch(field.relatedContentTypeId, query);

  const knownLabel = picked?.value === value ? picked.label : "";
  const selectedQuery = useQuery({
    queryKey: ["content-item", developerName, value],
    queryFn: () => adminApi.contentItems(developerName).get(value),
    enabled: developerName.length > 0 && value.length > 0 && !knownLabel,
    retry: false,
  });
  const selectedLabel =
    knownLabel || (selectedQuery.data ? readString(entityFields(selectedQuery.data), "primaryField") || value : "");

  return (
    <FormField
      label={field.label || field.developerName}
      required={field.isRequired}
      hint={field.description || undefined}
      htmlFor={field.developerName}
    >
      {(control) =>
        value ? (
          <div className="flex h-10 items-center gap-2.5 rounded-lg border border-border bg-card pr-1.5 pl-3 shadow-xs">
            <FileText className="size-4 shrink-0 text-muted-foreground" aria-hidden />
            <div className="min-w-0 flex-1 text-sm">
              {selectedLabel ? (
                <Link
                  to="/content/$developerName/items/$id"
                  params={{ developerName, id: value }}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex max-w-full items-center gap-1.5 font-medium text-foreground hover:text-primary hover:underline"
                >
                  <span className="truncate">{selectedLabel}</span>
                  <ExternalLink className="size-3.5 shrink-0 text-muted-foreground" aria-hidden />
                  <span className="sr-only">(opens in a new tab)</span>
                </Link>
              ) : selectedQuery.isError ? (
                <span className="text-muted-foreground">
                  Item not found <span className="font-mono text-xs">{value}</span>
                </span>
              ) : (
                <Skeleton className="h-4 w-40" />
              )}
            </div>
            <Button
              type="button"
              size="sm"
              variant="ghost"
              onClick={() => {
                focusOnMount.current = true;
                setQuery("");
                onChange("");
              }}
            >
              Clear
            </Button>
          </div>
        ) : (
          <Combobox
            {...control}
            ref={(input) => {
              if (input && focusOnMount.current) {
                focusOnMount.current = false;
                input.focus();
              }
            }}
            inputValue={query}
            onInputValueChange={setQuery}
            options={options}
            loading={loading}
            placeholder="Search related items"
            emptyText={query.trim() ? `No items match “${query.trim()}”` : "No items yet"}
            onSelect={(option) => {
              setPicked(option);
              onChange(option.value);
            }}
          />
        )
      }
    </FormField>
  );
}
