import { adminApi } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import { Card, CardContent, CardHeader, CardTitle, PageHeader, QueryGate } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { formatCell, humanizeKey, isRecord } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function DashboardPage() {
  useDocumentTitle(["Dashboard"]);
  const query = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => adminApi.dashboard(),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Dashboard" description="Overview of your site." />
      <QueryGate query={query}>{(data) => <MetricGrid data={data} />}</QueryGate>
    </div>
  );
}

function MetricGrid({ data }: { data: JsonObject }) {
  const cards = flattenMetrics(data);
  if (cards.length === 0) {
    return <p className="text-sm text-muted-foreground">No metrics returned.</p>;
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {cards.map((card) => (
        <Card key={card.title}>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="font-display text-2xl font-semibold">{card.value}</p>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function flattenMetrics(data: JsonObject, prefix = ""): { title: string; value: string }[] {
  const cards: { title: string; value: string }[] = [];
  for (const [key, value] of Object.entries(data)) {
    const title = prefix ? `${prefix} · ${humanizeKey(key)}` : humanizeKey(key);
    if (isRecord(value)) {
      cards.push(...flattenMetrics(value, title));
      continue;
    }
    if (Array.isArray(value)) {
      cards.push({ title, value: value.map((item) => formatCell(item)).filter(Boolean).join(", ") || "—" });
      continue;
    }
    cards.push({ title, value: formatCell(value) || "—" });
  }
  return cards;
}
