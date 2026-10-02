import type { SitePageWidgetDefinition } from "@raytha/api";
import { EmptyState, FormField, Textarea, cn } from "@raytha/ui";
import { SlidersHorizontal, TriangleAlert } from "lucide-react";
import type { ReactNode } from "react";
import { DefinitionFieldControl, WIDE_TYPES } from "../content/definition-field-control";
import { settingsFromJson, withSetting, type WidgetSettings } from "./models";

/**
 * The settings form of one widget, generated from its template's fields. A widget whose type the
 * active theme lacks, or whose settings are not a JSON object, gets the raw JSON instead.
 */
export function WidgetSettingsForm({
  widgetType,
  definition,
  settings,
  onChange,
  idPrefix = "widget-setting-",
}: {
  widgetType: string;
  definition: SitePageWidgetDefinition | undefined;
  settings: WidgetSettings;
  onChange: (next: WidgetSettings) => void;
  idPrefix?: string;
}) {
  const values = settings.values;
  if (!definition || values === null) {
    return (
      <JsonSettingsEditor
        settings={settings}
        onChange={onChange}
        reason={
          definition ? (
            "These settings are not a JSON object, so they are shown as JSON."
          ) : (
            <>
              The active theme has no <code className="font-mono">{widgetType || "unnamed"}</code> widget template.
              Its settings are shown as JSON so they stay editable.
            </>
          )
        }
      />
    );
  }

  if (definition.fields.length === 0) {
    return (
      <EmptyState
        icon={SlidersHorizontal}
        className="py-8"
        title="No settings"
        hint="This widget template has no fields. Add fields to the template to make it configurable."
      />
    );
  }

  return (
    <div className="grid gap-x-5 gap-y-5 sm:grid-cols-2">
      {definition.fields.map((field) => (
        <div key={field.developerName} className={cn(WIDE_TYPES.has(field.fieldType) && "sm:col-span-2")}>
          <DefinitionFieldControl
            definition={field}
            value={values[field.developerName]}
            siblings={values}
            idPrefix={idPrefix}
            onChange={(next) => onChange(withSetting(settings, definition.fields, field.developerName, next))}
          />
        </div>
      ))}
    </div>
  );
}

function JsonSettingsEditor({
  settings,
  onChange,
  reason,
}: {
  settings: WidgetSettings;
  onChange: (next: WidgetSettings) => void;
  reason: ReactNode;
}) {
  const invalid = settings.values === null && settings.json.trim() !== "";
  return (
    <div className="space-y-3">
      <p className="flex gap-2 rounded-lg border border-warning-border bg-warning-soft px-3 py-2 text-sm leading-6 text-foreground">
        <TriangleAlert className="mt-1 size-4 shrink-0 text-warning" aria-hidden />
        <span>{reason}</span>
      </p>
      <FormField
        label="Settings JSON"
        htmlFor="widget-settings-json"
        error={invalid ? "Enter a JSON object, like {\"headline\": \"Hello\"}." : undefined}
      >
        {(control) => (
          <Textarea
            {...control}
            rows={12}
            spellCheck={false}
            className="font-mono text-xs"
            value={settings.json}
            onChange={(event) => onChange(settingsFromJson(event.target.value))}
          />
        )}
      </FormField>
    </div>
  );
}
