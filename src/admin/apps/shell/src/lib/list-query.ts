export type ListQuery = {
  search?: string;
  pageNumber?: number;
  orderBy?: string;
};

const STORAGE_PREFIX = "raytha.list:";

export function listQueryFromSearch(search: Record<string, unknown>): ListQuery {
  const query: ListQuery = {};
  const searchText = readSearchString(search.search);
  if (searchText) {
    query.search = searchText;
  }
  const pageNumber = readPageNumber(search.pageNumber);
  if (pageNumber !== undefined && pageNumber !== 1) {
    query.pageNumber = pageNumber;
  }
  const orderBy = readSearchString(search.orderBy);
  if (orderBy) {
    query.orderBy = orderBy;
  }
  return query;
}

export function listQueryFromSearchString(searchString: string): ListQuery {
  const params = new URLSearchParams(searchString.startsWith("?") ? searchString.slice(1) : searchString);
  const search: Record<string, unknown> = {};
  const searchText = params.get("search");
  if (searchText) {
    search.search = searchText;
  }
  const pageNumber = params.get("pageNumber");
  if (pageNumber) {
    search.pageNumber = pageNumber;
  }
  const orderBy = params.get("orderBy");
  if (orderBy) {
    search.orderBy = orderBy;
  }
  return listQueryFromSearch(search);
}

export function compactListQuery(query: ListQuery): Record<string, string> {
  const search: Record<string, string> = {};
  if (query.search) {
    search.search = query.search;
  }
  if (query.pageNumber !== undefined && query.pageNumber !== 1) {
    search.pageNumber = String(query.pageNumber);
  }
  if (query.orderBy) {
    search.orderBy = query.orderBy;
  }
  return search;
}

export function listQueryKey(query: ListQuery): string {
  return JSON.stringify(compactListQuery(query));
}

export function rememberListQuery(listKey: string, query: ListQuery): void {
  try {
    sessionStorage.setItem(`${STORAGE_PREFIX}${listKey}`, JSON.stringify(compactListQuery(query)));
  } catch {
    // sessionStorage can be unavailable
  }
}

export function readRememberedListQuery(listKey: string): Record<string, string> | undefined {
  try {
    const raw = sessionStorage.getItem(`${STORAGE_PREFIX}${listKey}`);
    if (!raw) {
      return undefined;
    }
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return undefined;
    }
    const search: Record<string, string> = {};
    for (const [key, value] of Object.entries(parsed)) {
      if (typeof value === "string" && value.length > 0) {
        search[key] = value;
      }
    }
    return Object.keys(search).length > 0 ? search : undefined;
  } catch {
    return undefined;
  }
}

export function parseOrderBy(orderBy?: string): { column: string; dir: string } {
  if (!orderBy) {
    return { column: "", dir: "" };
  }
  const [column, dir] = orderBy.split(/\s+/);
  return { column: column ?? "", dir: dir ?? "asc" };
}

export function nextOrderBy(
  current: string | undefined,
  column: string,
  naturalDir: "asc" | "desc",
): string | undefined {
  const parsed = parseOrderBy(current);
  if (parsed.column !== column) {
    return `${column} ${naturalDir}`;
  }
  if (parsed.dir === naturalDir) {
    return `${column} ${naturalDir === "asc" ? "desc" : "asc"}`;
  }
  return undefined;
}

export function listHref(
  to: string,
  params?: Record<string, string>,
  search?: Record<string, string>,
): string {
  let path = to;
  for (const [key, value] of Object.entries(params ?? {})) {
    path = path.replaceAll(`$${key}`, encodeURIComponent(value));
  }
  const qs = search ? new URLSearchParams(search).toString() : "";
  return `/raytha${path.startsWith("/") ? path : `/${path}`}${qs ? `?${qs}` : ""}`;
}

export function listApiParams(query: ListQuery): Record<string, string | number | undefined> {
  return {
    search: query.search,
    pageNumber: query.pageNumber,
    orderBy: query.orderBy,
    pageSize: 50,
  };
}

function readSearchString(value: unknown): string | undefined {
  if (typeof value === "string" && value.length > 0) {
    return value;
  }
  return undefined;
}

function readPageNumber(value: unknown): number | undefined {
  if (typeof value === "number" && Number.isFinite(value) && value >= 1) {
    return Math.floor(value);
  }
  if (typeof value === "string" && value.length > 0) {
    const parsed = Number(value);
    if (Number.isFinite(parsed) && parsed >= 1) {
      return Math.floor(parsed);
    }
  }
  return undefined;
}
