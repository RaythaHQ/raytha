/**
 * Where the cursor sits in a Liquid template, for completion. `from` is the document
 * offset where the typed `query` starts, so a completion replaces `from..cursor`.
 */
export type LiquidContext =
  | { kind: "text" }
  | { kind: "tag-name"; from: number; query: string }
  | { kind: "filter"; from: number; query: string }
  | { kind: "expression"; from: number; query: string; delimiter: "output" | "tag"; justOpened: boolean };

const LOOKBEHIND = 4000;

export function liquidContextAt(doc: string, pos: number): LiquidContext {
  const start = Math.max(0, pos - LOOKBEHIND);
  const before = doc.slice(start, pos);
  const openOutput = before.lastIndexOf("{{");
  const openTag = before.lastIndexOf("{%");
  const open = Math.max(openOutput, openTag);
  if (open < 0 || open < Math.max(before.lastIndexOf("}}"), before.lastIndexOf("%}"))) {
    return { kind: "text" };
  }

  const delimiter = open === openTag ? "tag" : "output";
  const body = before.slice(open + 2).replace(/^-/, "");
  if (insideString(body)) {
    return { kind: "text" };
  }

  if (delimiter === "tag") {
    const tagName = /^\s*(\w*)$/.exec(body);
    if (tagName) {
      return { kind: "tag-name", from: pos - tagName[1].length, query: tagName[1] };
    }
  }

  const filter = /\|\s*(\w*)$/.exec(body);
  if (filter) {
    return { kind: "filter", from: pos - filter[1].length, query: filter[1] };
  }

  const token = /[\w.[\]]*$/.exec(body)?.[0] ?? "";
  return {
    kind: "expression",
    from: pos - token.length,
    query: token,
    delimiter,
    justOpened: body.trim() === "",
  };
}

function insideString(body: string): boolean {
  let quote: string | null = null;
  for (const char of body) {
    if (quote) {
      if (char === quote) {
        quote = null;
      }
    } else if (char === '"' || char === "'") {
      quote = char;
    }
  }
  return quote !== null;
}
