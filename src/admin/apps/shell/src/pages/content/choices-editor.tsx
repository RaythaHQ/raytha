import { Button, Checkbox, Input } from "@raytha/ui";
import { toDeveloperName } from "../entity";
import type { FieldChoice } from "./fields-model";

export function ChoicesEditor({
  choices,
  onChange,
  labelPrefix = "",
}: {
  choices: FieldChoice[];
  onChange: (choices: FieldChoice[]) => void;
  /** Keeps control names unique when several choice lists share a page. */
  labelPrefix?: string;
}) {
  return (
    <div className="space-y-3">
      <p className="text-sm font-medium">Choices</p>
      {choices.map((choice, index) => (
        <div key={index} className="grid gap-2 rounded-lg border border-border p-3 sm:grid-cols-[1fr_1fr_auto_auto]">
          <Input
            aria-label={`${labelPrefix}Choice ${index + 1} label`}
            placeholder="Label"
            value={choice.label}
            onChange={(event) => {
              const label = event.target.value;
              const next = choices.slice();
              const current = next[index];
              if (!current) {
                return;
              }
              next[index] = {
                ...current,
                label,
                developerName:
                  current.developerName === "" || current.developerName === toDeveloperName(current.label)
                    ? toDeveloperName(label)
                    : current.developerName,
              };
              onChange(next);
            }}
          />
          <Input
            aria-label={`${labelPrefix}Choice ${index + 1} developer name`}
            placeholder="Developer name"
            value={choice.developerName}
            onChange={(event) => {
              const next = choices.slice();
              const current = next[index];
              if (!current) {
                return;
              }
              next[index] = { ...current, developerName: event.target.value };
              onChange(next);
            }}
          />
          <label className="flex items-center gap-2 text-sm">
            <Checkbox
              checked={choice.disabled}
              onCheckedChange={(checked) => {
                const next = choices.slice();
                const current = next[index];
                if (!current) {
                  return;
                }
                next[index] = { ...current, disabled: checked };
                onChange(next);
              }}
            />
            Disabled
          </label>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={choices.length === 1}
            onClick={() => onChange(choices.filter((_, choiceIndex) => choiceIndex !== index))}
          >
            Remove
          </Button>
        </div>
      ))}
      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={() => onChange([...choices, { label: "", developerName: "", disabled: false }])}
      >
        Add choice
      </Button>
    </div>
  );
}
