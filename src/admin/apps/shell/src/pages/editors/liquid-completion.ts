import {
  autocompletion,
  pickedCompletion,
  snippet,
  type Completion,
  type CompletionContext,
  type CompletionResult,
  type CompletionSource,
} from "@codemirror/autocomplete";
import type { TemplateVariableGroup } from "@raytha/api";
import { liquidContextAt, type LiquidContext } from "@raytha/ui";
import { EditorView, type Extension } from "@uiw/react-codemirror";
import { liquidFilters, liquidTags, raythaFilters, raythaFunctions, type LiquidTerm } from "./liquid-catalog";

const LOOKBEHIND = 4000;
const WORD = /^\w*$/;
const PATH = /^[\w.[\]]*$/;
const EMPTY = /^$/;

export function liquidAutocomplete(variables: TemplateVariableGroup[]): Extension {
  const liquid = liquidSource(variables);
  return [
    autocompletion({ override: [liquid, languageFallback], icons: false, maxRenderedOptions: 200 }),
    completionTheme,
  ];
}

function liquidSource(groups: TemplateVariableGroup[]): CompletionSource {
  const tags = liquidTags.map(tagCompletion);
  const filters = [
    ...raythaFilters.map((term) => termCompletion(term, { name: "Raytha filters", rank: 0 }, 1)),
    ...liquidFilters.map((term) => termCompletion(term, { name: "Liquid filters", rank: 1 }, 0)),
  ];
  const browse = [
    ...groups.flatMap((group, rank) =>
      group.variables.map(
        (variable): Completion => ({
          label: variable.path,
          detail: variable.description ?? undefined,
          info: () => infoNode(group.category, variable.description, variable.example),
          type: "variable",
          section: { name: group.category, rank },
          apply: applyVariable(variable.path),
        }),
      ),
    ),
    ...raythaFunctions.map((term) => termCompletion(term, { name: "Raytha functions", rank: groups.length }, 0)),
  ];
  // Sections outrank match quality in CodeMirror's sort, so once the user types, rank one flat list.
  const search = browse.map(({ section: _section, ...completion }) => completion);

  return (context: CompletionContext): CompletionResult | null => {
    const cx = contextAt(context);
    switch (cx.kind) {
      case "text":
        return null;
      case "tag-name":
        return { from: cx.from, options: tags, validFor: WORD };
      case "filter":
        return { from: cx.from, options: filters, validFor: WORD };
      case "expression":
        if (cx.query !== "") {
          return { from: cx.from, options: search, validFor: PATH };
        }
        if (!cx.justOpened && !context.explicit) {
          return null;
        }
        return { from: cx.from, options: browse, validFor: EMPTY };
    }
  };
}

/** Outside `{{ }}` and `{% %}`, keep the HTML completions the Liquid language brings. */
async function languageFallback(context: CompletionContext): Promise<CompletionResult | null> {
  if (contextAt(context).kind !== "text") {
    return null;
  }
  for (const source of context.state.languageDataAt<CompletionSource>("autocomplete", context.pos)) {
    if (typeof source !== "function") {
      continue;
    }
    const result = await source(context);
    if (result) {
      return result;
    }
  }
  return null;
}

function contextAt(context: CompletionContext): LiquidContext {
  const start = Math.max(0, context.pos - LOOKBEHIND);
  const cx = liquidContextAt(context.state.sliceDoc(start, context.pos), context.pos - start);
  return cx.kind === "text" ? cx : { ...cx, from: cx.from + start };
}

function applyVariable(path: string) {
  return (view: EditorView, completion: Completion, from: number, to: number) => {
    const lead = /[{%]$/.test(view.state.sliceDoc(from - 1, from)) ? " " : "";
    const trail = /^[}%]/.test(view.state.sliceDoc(to, to + 1)) ? " " : "";
    view.dispatch({
      changes: { from, to, insert: `${lead}${path}${trail}` },
      selection: { anchor: from + lead.length + path.length },
      annotations: pickedCompletion.of(completion),
      userEvent: "input.complete",
    });
  };
}

function tagCompletion(term: LiquidTerm): Completion {
  const template = term.template ?? `${term.name} %}`;
  return {
    label: term.name,
    detail: term.detail,
    type: "keyword",
    boost: term.name.startsWith("end") ? -1 : 0,
    apply: (view, completion, from, to) => {
      const close = /^\s*-?%}/.exec(view.state.sliceDoc(to, to + 16));
      const lead = /[%-]$/.test(view.state.sliceDoc(from - 1, from)) ? " " : "";
      snippet(lead + template)(view, completion, from, close ? to + close[0].length : to);
    },
  };
}

function termCompletion(term: LiquidTerm, section: { name: string; rank: number }, boost: number): Completion {
  return {
    label: term.name,
    detail: term.detail,
    type: "function",
    section,
    boost,
    apply: term.template ? snippet(term.template) : term.name,
  };
}

function infoNode(category: string, description: string | null, example: string | null): HTMLElement {
  const root = document.createElement("div");
  const label = document.createElement("p");
  label.className = "cm-completionCategory";
  label.textContent = category;
  root.append(label);
  if (description) {
    const text = document.createElement("p");
    text.textContent = description;
    root.append(text);
  }
  if (example) {
    const code = document.createElement("pre");
    code.textContent = example;
    root.append(code);
  }
  return root;
}

const completionTheme = EditorView.theme({
  ".cm-tooltip.cm-tooltip-autocomplete": {
    border: "1px solid var(--border)",
    borderRadius: "10px",
    background: "var(--card)",
    boxShadow: "var(--shadow-pop)",
    overflow: "hidden",
    padding: "4px",
  },
  ".cm-tooltip.cm-tooltip-autocomplete > ul": {
    fontFamily: "var(--font-mono)",
    fontSize: "12.5px",
    maxHeight: "18rem",
    minWidth: "22rem",
    maxWidth: "36rem",
  },
  ".cm-tooltip.cm-tooltip-autocomplete > ul > li": {
    display: "flex",
    alignItems: "baseline",
    gap: "12px",
    padding: "5px 8px",
    borderRadius: "6px",
    lineHeight: "1.4",
  },
  ".cm-tooltip.cm-tooltip-autocomplete > ul > li[aria-selected]": {
    background: "var(--brand-100)",
    color: "var(--brand-950)",
  },
  ".cm-tooltip.cm-tooltip-autocomplete > ul > completion-section": {
    display: "block",
    padding: "8px 8px 4px",
    fontFamily: "var(--font-sans)",
    fontSize: "10.5px",
    fontWeight: "600",
    letterSpacing: "0.06em",
    textTransform: "uppercase",
    color: "var(--muted-foreground)",
    borderBottom: "none",
    opacity: "1",
  },
  ".cm-completionLabel": { whiteSpace: "nowrap" },
  ".cm-completionMatchedText": { textDecoration: "none", fontWeight: "700", color: "var(--primary)" },
  ".cm-completionDetail": {
    marginLeft: "auto",
    fontFamily: "var(--font-sans)",
    fontStyle: "normal",
    fontSize: "11.5px",
    color: "var(--muted-foreground)",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
    maxWidth: "16rem",
  },
  ".cm-tooltip.cm-completionInfo": {
    border: "1px solid var(--border)",
    borderRadius: "10px",
    background: "var(--card)",
    boxShadow: "var(--shadow-pop)",
    padding: "10px 12px",
    maxWidth: "22rem",
    fontSize: "12.5px",
    lineHeight: "1.5",
    color: "var(--foreground)",
  },
  ".cm-completionCategory": {
    margin: "0 0 4px",
    fontSize: "10.5px",
    fontWeight: "600",
    letterSpacing: "0.06em",
    textTransform: "uppercase",
    color: "var(--muted-foreground)",
  },
  ".cm-completionInfo pre": {
    margin: "8px 0 0",
    padding: "8px",
    borderRadius: "6px",
    background: "var(--muted)",
    fontFamily: "var(--font-mono)",
    fontSize: "11.5px",
    whiteSpace: "pre-wrap",
  },
});
