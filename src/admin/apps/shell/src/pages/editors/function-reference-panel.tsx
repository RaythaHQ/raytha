import { Badge, cn, Input } from "@raytha/ui";
import { BookOpen, ChevronRight, Copy, CornerDownLeft, Search } from "lucide-react";
import { useMemo, useState, type ReactNode } from "react";
import { copyText } from "./clipboard";
import {
  FUNCTION_LIMITS,
  FUNCTION_RECIPES,
  REFERENCE_GROUPS,
  triggerFor,
  type FunctionRecipe,
  type FunctionTriggerType,
  type ReferenceGroup,
  type ReferenceMember,
} from "./function-reference";

const RECIPES_ID = "recipes";
const LIMITS_ID = "limits";

export function FunctionReferencePanel({
  trigger,
  developerName,
  onInsert,
}: {
  trigger: FunctionTriggerType;
  developerName: string;
  onInsert: (text: string) => void;
}) {
  const named = (text: string) => text.replaceAll("{developerName}", developerName.trim() || "{developerName}");
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState<ReadonlySet<string>>(() => new Set(["results", RECIPES_ID]));
  const terms = useMemo(() => query.trim().toLowerCase().split(/\s+/).filter(Boolean), [query]);
  const groups = useMemo(() => filterGroups(REFERENCE_GROUPS, terms), [terms]);
  const recipes = useMemo(() => orderRecipes(FUNCTION_RECIPES, trigger, terms), [trigger, terms]);
  const current = triggerFor(trigger);
  const searching = terms.length > 0;

  const toggle = (id: string) =>
    setOpen((previous) => {
      const next = new Set(previous);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  const isOpen = (id: string) => searching || open.has(id);

  return (
    <aside
      aria-label="Functions reference"
      className="flex min-h-0 flex-col overflow-hidden rounded-xl border border-border bg-card shadow-card lg:sticky lg:top-4 lg:max-h-[calc(100vh-2rem)]"
    >
      <div className="space-y-3 border-b border-border p-4">
        <div className="flex items-center gap-2">
          <BookOpen className="size-4 text-primary" aria-hidden />
          <h2 className="font-display text-sm font-semibold">Reference</h2>
        </div>
        <section aria-label="Entry points" className="space-y-2 rounded-lg bg-muted/60 p-3">
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">{current.label}</p>
          <p className="text-[13px] leading-5 text-muted-foreground">{named(current.summary)}</p>
          <ul className="space-y-2">
            {current.entryPoints.map((entry) => (
              <li key={entry.signature} className="text-[13px] leading-5">
                <code className="font-mono font-semibold text-foreground">{entry.signature}</code>
                <span className="block text-muted-foreground">{entry.when}</span>
                <span className="block text-muted-foreground">
                  <span className="font-medium text-foreground">Returns:</span> {entry.returns}
                </span>
              </li>
            ))}
          </ul>
        </section>
        <div className="relative">
          <Search
            className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden
          />
          <Input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Escape" && query) {
                event.preventDefault();
                setQuery("");
              }
            }}
            placeholder="Search API_V1, HttpClient, Emailer…"
            aria-label="Search the functions reference"
            className="h-8 pl-8 text-[13px]"
          />
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        {recipes.length > 0 ? (
          <Section
            id={RECIPES_ID}
            title="Recipes"
            count={recipes.length}
            open={isOpen(RECIPES_ID)}
            onToggle={() => toggle(RECIPES_ID)}
          >
            {recipes.map((recipe) => (
              <RecipeRow
                key={recipe.id}
                recipe={{ ...recipe, description: named(recipe.description), code: named(recipe.code) }}
                matchesTrigger={recipe.trigger === trigger}
                onInsert={onInsert}
              />
            ))}
          </Section>
        ) : null}
        {groups.map((group) => (
          <Section
            key={group.id}
            id={group.id}
            title={group.title}
            count={group.members.length}
            open={isOpen(group.id)}
            onToggle={() => toggle(group.id)}
          >
            <p className="px-4 pb-1 pt-2 text-xs leading-4 text-muted-foreground">{group.description}</p>
            {group.members.map((member) => (
              <MemberRow key={member.signature} member={member} onInsert={onInsert} />
            ))}
          </Section>
        ))}
        {!searching ? (
          <Section
            id={LIMITS_ID}
            title="Limits and sandbox"
            count={FUNCTION_LIMITS.length}
            open={isOpen(LIMITS_ID)}
            onToggle={() => toggle(LIMITS_ID)}
          >
            <dl className="space-y-2 px-4 py-3">
              {FUNCTION_LIMITS.map((limit) => (
                <div key={limit.title} className="text-[13px] leading-5">
                  <dt className="font-medium text-foreground">{limit.title}</dt>
                  <dd className="text-muted-foreground">{limit.detail}</dd>
                </div>
              ))}
            </dl>
          </Section>
        ) : null}
        {searching && groups.length === 0 && recipes.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">Nothing matches “{query.trim()}”.</p>
        ) : null}
      </div>
    </aside>
  );
}

function Section({
  id,
  title,
  count,
  open,
  onToggle,
  children,
}: {
  id: string;
  title: string;
  count: number;
  open: boolean;
  onToggle: () => void;
  children: ReactNode;
}) {
  const regionId = `function-reference-${id}`;
  return (
    <section aria-label={title} className="border-b border-border last:border-b-0">
      <button
        type="button"
        aria-expanded={open}
        aria-controls={regionId}
        onClick={onToggle}
        className="sticky top-0 z-10 flex w-full items-center gap-2 bg-card/95 px-4 py-2.5 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur hover:text-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
      >
        <ChevronRight className={cn("size-3.5 transition-transform", open && "rotate-90")} aria-hidden />
        <span className="min-w-0 flex-1 truncate">{title}</span>
        <span className="rounded-full bg-muted px-1.5 py-px text-[10px] tabular-nums">{count}</span>
      </button>
      {open ? (
        <div id={regionId} className="pb-2">
          {children}
        </div>
      ) : null}
    </section>
  );
}

function MemberRow({ member, onInsert }: { member: ReferenceMember; onInsert: (text: string) => void }) {
  return (
    <div className="space-y-1.5 px-4 py-2.5">
      <code className="block break-words font-mono text-[12.5px] font-medium leading-5 text-foreground">
        {member.signature}
      </code>
      <p className="text-xs leading-4 text-muted-foreground">{member.description}</p>
      <Snippet code={member.example} label={member.signature} onInsert={onInsert} />
    </div>
  );
}

function RecipeRow({
  recipe,
  matchesTrigger,
  onInsert,
}: {
  recipe: FunctionRecipe;
  matchesTrigger: boolean;
  onInsert: (text: string) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const trigger = triggerFor(recipe.trigger);
  return (
    <div className="space-y-1.5 px-4 py-2.5">
      <div className="flex items-start justify-between gap-2">
        <button
          type="button"
          aria-expanded={expanded}
          onClick={() => setExpanded((value) => !value)}
          className="min-w-0 text-left text-[13px] font-medium leading-5 text-foreground hover:text-primary focus-visible:outline-2 focus-visible:outline-ring"
        >
          {recipe.title}
        </button>
        <Badge variant={matchesTrigger ? "info" : "neutral"} className="shrink-0">
          {trigger.label}
        </Badge>
      </div>
      <p className="text-xs leading-4 text-muted-foreground">{recipe.description}</p>
      {expanded ? (
        <Snippet code={recipe.code} label={recipe.title} onInsert={onInsert} />
      ) : (
        <button
          type="button"
          onClick={() => setExpanded(true)}
          className="text-xs font-medium text-primary hover:underline"
        >
          Show code
        </button>
      )}
    </div>
  );
}

function Snippet({ code, label, onInsert }: { code: string; label: string; onInsert: (text: string) => void }) {
  return (
    <div className="group relative">
      <pre className="overflow-x-auto rounded-md border border-border bg-muted/50 p-2 pr-16 font-mono text-[11.5px] leading-[1.45] text-foreground">
        {code}
      </pre>
      <div className="absolute right-1 top-1 flex gap-0.5">
        <SnippetAction label={`Insert at cursor: ${label}`} onClick={() => onInsert(code)}>
          <CornerDownLeft aria-hidden />
        </SnippetAction>
        <SnippetAction label={`Copy: ${label}`} onClick={() => void copyText(code)}>
          <Copy aria-hidden />
        </SnippetAction>
      </div>
    </div>
  );
}

function SnippetAction({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      onClick={onClick}
      className="flex size-6 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-card hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring [&_svg]:size-3.5"
    >
      {children}
    </button>
  );
}

function matches(terms: string[], ...fields: string[]): boolean {
  const haystack = fields.join(" ").toLowerCase();
  return terms.every((term) => haystack.includes(term));
}

function filterGroups(groups: readonly ReferenceGroup[], terms: string[]): ReferenceGroup[] {
  if (terms.length === 0) {
    return [...groups];
  }
  return groups
    .map((group) => ({
      ...group,
      members: group.members.filter((member) =>
        matches(terms, group.title, member.signature, member.description, member.example),
      ),
    }))
    .filter((group) => group.members.length > 0);
}

function orderRecipes(
  recipes: readonly FunctionRecipe[],
  trigger: FunctionTriggerType,
  terms: string[],
): FunctionRecipe[] {
  return recipes
    .filter((recipe) => matches(terms, recipe.title, recipe.description, recipe.code))
    .sort((left, right) => Number(right.trigger === trigger) - Number(left.trigger === trigger));
}
