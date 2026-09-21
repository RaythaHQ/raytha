import { adminApi } from "@raytha/api";
import {
  Button,
  Checkbox,
  FileUpload,
  FormField,
  Input,
  Select,
  Textarea,
} from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { RichTextEditor } from "../../components/rich-text-editor";
import { entityFields, readString } from "../entity";
import type {
  CardSettings,
  ContentListSettings,
  CtaSettings,
  EmbedSettings,
  FaqSettings,
  HeroSettings,
  ImageTextSettings,
  WidgetSettings,
  WysiwygSettings,
} from "./models";

const BUTTON_STYLES: readonly string[] = ["primary", "secondary", "outline-primary", "outline-light", "light", "link"];
const ALIGNMENTS: readonly string[] = ["left", "center", "right"];
const PADDINGS: readonly string[] = ["none", "small", "medium", "large"];
const IMAGE_POSITIONS: readonly string[] = ["left", "right"];
const DISPLAY_STYLES: readonly string[] = ["cards", "list", "compact"];
const EMBED_TYPES: readonly string[] = ["iframe", "html"];
const ASPECT_RATIOS: readonly string[] = ["16x9", "4x3", "1x1", "21x9"];

export function WidgetSettingsForm({
  settings,
  onChange,
}: {
  settings: WidgetSettings;
  onChange: (next: WidgetSettings) => void;
}) {
  switch (settings.kind) {
    case "hero":
      return <HeroForm value={settings} onChange={onChange} />;
    case "wysiwyg":
      return <WysiwygForm value={settings} onChange={onChange} />;
    case "imagetext":
      return <ImageTextForm value={settings} onChange={onChange} />;
    case "card":
      return <CardForm value={settings} onChange={onChange} />;
    case "faq":
      return <FaqForm value={settings} onChange={onChange} />;
    case "cta":
      return <CtaForm value={settings} onChange={onChange} />;
    case "embed":
      return <EmbedForm value={settings} onChange={onChange} />;
    case "contentlist":
      return <ContentListForm value={settings} onChange={onChange} />;
    case "unknown":
      return <p className="text-sm text-muted-foreground">No settings editor for this widget type.</p>;
    default: {
      const _exhaustive: never = settings;
      return _exhaustive;
    }
  }
}

function HeroForm({ value, onChange }: { value: HeroSettings; onChange: (next: HeroSettings) => void }) {
  return (
    <div className="space-y-4">
      <TextField id="hero-headline" label="Headline" value={value.headline} onChange={(headline) => onChange({ ...value, headline })} />
      <TextField id="hero-subheadline" label="Subheadline" value={value.subheadline} onChange={(subheadline) => onChange({ ...value, subheadline })} />
      <ImageField id="hero-background" label="Background image" value={value.backgroundImage} onChange={(backgroundImage) => onChange({ ...value, backgroundImage })} />
      <TextField id="hero-bg-color" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
      <TextField id="hero-text-color" label="Text color" value={value.textColor} onChange={(textColor) => onChange({ ...value, textColor })} />
      <TextField id="hero-button-text" label="Button text" value={value.buttonText} onChange={(buttonText) => onChange({ ...value, buttonText })} />
      <TextField id="hero-button-url" label="Button URL" value={value.buttonUrl} onChange={(buttonUrl) => onChange({ ...value, buttonUrl })} />
      <ChoiceField id="hero-button-style" label="Button style" value={value.buttonStyle} options={BUTTON_STYLES} onChange={(buttonStyle) => onChange({ ...value, buttonStyle })} />
      <ChoiceField id="hero-alignment" label="Alignment" value={value.alignment} options={ALIGNMENTS} onChange={(alignment) => onChange({ ...value, alignment })} />
      <NumberField id="hero-min-height" label="Minimum height" value={value.minHeight} onChange={(minHeight) => onChange({ ...value, minHeight })} />
    </div>
  );
}

function WysiwygForm({ value, onChange }: { value: WysiwygSettings; onChange: (next: WysiwygSettings) => void }) {
  return (
    <div className="space-y-4">
      <HtmlField label="Content" value={value.content} onChange={(content) => onChange({ ...value, content })} />
      <TextField id="wysiwyg-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
      <ChoiceField id="wysiwyg-padding" label="Padding" value={value.padding} options={PADDINGS} onChange={(padding) => onChange({ ...value, padding })} />
    </div>
  );
}

function ImageTextForm({
  value,
  onChange,
}: {
  value: ImageTextSettings;
  onChange: (next: ImageTextSettings) => void;
}) {
  return (
    <div className="space-y-4">
      <ImageField id="imagetext-image" label="Image" value={value.imageUrl} onChange={(imageUrl) => onChange({ ...value, imageUrl })} />
      <TextField id="imagetext-alt" label="Image alt text" value={value.imageAlt} onChange={(imageAlt) => onChange({ ...value, imageAlt })} />
      <TextField id="imagetext-headline" label="Headline" value={value.headline} onChange={(headline) => onChange({ ...value, headline })} />
      <HtmlField label="Content" value={value.content} onChange={(content) => onChange({ ...value, content })} />
      <ChoiceField id="imagetext-position" label="Image position" value={value.imagePosition} options={IMAGE_POSITIONS} onChange={(imagePosition) => onChange({ ...value, imagePosition })} />
      <TextField id="imagetext-button-text" label="Button text" value={value.buttonText} onChange={(buttonText) => onChange({ ...value, buttonText })} />
      <TextField id="imagetext-button-url" label="Button URL" value={value.buttonUrl} onChange={(buttonUrl) => onChange({ ...value, buttonUrl })} />
      <ChoiceField id="imagetext-button-style" label="Button style" value={value.buttonStyle} options={BUTTON_STYLES} onChange={(buttonStyle) => onChange({ ...value, buttonStyle })} />
      <TextField id="imagetext-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
    </div>
  );
}

function CardForm({ value, onChange }: { value: CardSettings; onChange: (next: CardSettings) => void }) {
  return (
    <div className="space-y-4">
      <TextField id="card-title" label="Title" value={value.title} onChange={(title) => onChange({ ...value, title })} />
      <HtmlField label="Description" value={value.description} onChange={(description) => onChange({ ...value, description })} />
      <ImageField id="card-image" label="Image" value={value.imageUrl} onChange={(imageUrl) => onChange({ ...value, imageUrl })} />
      <TextField id="card-alt" label="Image alt text" value={value.imageAlt} onChange={(imageAlt) => onChange({ ...value, imageAlt })} />
      <TextField id="card-button-text" label="Button text" value={value.buttonText} onChange={(buttonText) => onChange({ ...value, buttonText })} />
      <TextField id="card-button-url" label="Button URL" value={value.buttonUrl} onChange={(buttonUrl) => onChange({ ...value, buttonUrl })} />
      <ChoiceField id="card-button-style" label="Button style" value={value.buttonStyle} options={BUTTON_STYLES} onChange={(buttonStyle) => onChange({ ...value, buttonStyle })} />
      <TextField id="card-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
    </div>
  );
}

function FaqForm({ value, onChange }: { value: FaqSettings; onChange: (next: FaqSettings) => void }) {
  return (
    <div className="space-y-4">
      <TextField id="faq-headline" label="Headline" value={value.headline} onChange={(headline) => onChange({ ...value, headline })} />
      <TextField id="faq-subheadline" label="Subheadline" value={value.subheadline} onChange={(subheadline) => onChange({ ...value, subheadline })} />
      <label className="flex items-center gap-2 text-sm">
        <Checkbox checked={value.expandFirst} onCheckedChange={(expandFirst) => onChange({ ...value, expandFirst })} />
        Expand first item
      </label>
      <TextField id="faq-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
      <div className="space-y-3">
        {value.items.map((item, index) => (
          <div key={`${index}-${item.question}`} className="space-y-3 rounded-lg border border-border p-3">
            <TextField
              id={`faq-q-${index}`}
              label={`Question ${index + 1}`}
              value={item.question}
              onChange={(question) =>
                onChange({
                  ...value,
                  items: value.items.map((current, currentIndex) =>
                    currentIndex === index ? { ...current, question } : current,
                  ),
                })
              }
            />
            <HtmlField
              label="Answer"
              value={item.answer}
              onChange={(answer) =>
                onChange({
                  ...value,
                  items: value.items.map((current, currentIndex) =>
                    currentIndex === index ? { ...current, answer } : current,
                  ),
                })
              }
            />
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() =>
                onChange({ ...value, items: value.items.filter((_, currentIndex) => currentIndex !== index) })
              }
            >
              Remove question
            </Button>
          </div>
        ))}
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange({ ...value, items: [...value.items, { question: "", answer: "" }] })}
        >
          Add question
        </Button>
      </div>
    </div>
  );
}

function CtaForm({ value, onChange }: { value: CtaSettings; onChange: (next: CtaSettings) => void }) {
  return (
    <div className="space-y-4">
      <TextField id="cta-headline" label="Headline" value={value.headline} onChange={(headline) => onChange({ ...value, headline })} />
      <HtmlField label="Content" value={value.content} onChange={(content) => onChange({ ...value, content })} />
      <TextField id="cta-button-text" label="Button text" value={value.buttonText} onChange={(buttonText) => onChange({ ...value, buttonText })} />
      <TextField id="cta-button-url" label="Button URL" value={value.buttonUrl} onChange={(buttonUrl) => onChange({ ...value, buttonUrl })} />
      <ChoiceField id="cta-button-style" label="Button style" value={value.buttonStyle} options={BUTTON_STYLES} onChange={(buttonStyle) => onChange({ ...value, buttonStyle })} />
      <TextField id="cta-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
      <TextField id="cta-text-color" label="Text color" value={value.textColor} onChange={(textColor) => onChange({ ...value, textColor })} />
      <ChoiceField id="cta-alignment" label="Alignment" value={value.alignment} options={ALIGNMENTS} onChange={(alignment) => onChange({ ...value, alignment })} />
    </div>
  );
}

function EmbedForm({ value, onChange }: { value: EmbedSettings; onChange: (next: EmbedSettings) => void }) {
  return (
    <div className="space-y-4">
      <ChoiceField id="embed-type" label="Embed type" value={value.embedType} options={EMBED_TYPES} onChange={(embedType) => onChange({ ...value, embedType })} />
      <TextField id="embed-iframe" label="Iframe URL" value={value.iframeUrl} onChange={(iframeUrl) => onChange({ ...value, iframeUrl })} />
      <FormField label="Embed HTML" htmlFor="embed-html">
        {(control) => (
          <Textarea
            {...control}
            rows={8}
            value={value.htmlContent}
            onChange={(event) => onChange({ ...value, htmlContent: event.target.value })}
          />
        )}
      </FormField>
      <ChoiceField id="embed-ratio" label="Aspect ratio" value={value.aspectRatio} options={ASPECT_RATIOS} onChange={(aspectRatio) => onChange({ ...value, aspectRatio })} />
      <NumberField
        id="embed-max-width"
        label="Max width"
        value={value.maxWidth ?? 0}
        onChange={(maxWidth) => onChange({ ...value, maxWidth: maxWidth > 0 ? maxWidth : null })}
      />
      <TextField id="embed-caption" label="Caption" value={value.caption} onChange={(caption) => onChange({ ...value, caption })} />
      <TextField id="embed-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
    </div>
  );
}

function ContentListForm({
  value,
  onChange,
}: {
  value: ContentListSettings;
  onChange: (next: ContentListSettings) => void;
}) {
  const typesQuery = useQuery({
    queryKey: ["content-types", "site-page-widget"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });
  const viewsQuery = useQuery({
    queryKey: ["content-views", value.contentType],
    queryFn: () => adminApi.views(value.contentType).list({ pageSize: 100 }),
    enabled: value.contentType.length > 0,
    placeholderData: keepPreviousData,
  });
  const types = typesQuery.data?.items ?? [];
  const views = viewsQuery.data?.items ?? [];

  return (
    <div className="space-y-4">
      <TextField id="list-headline" label="Headline" value={value.headline} onChange={(headline) => onChange({ ...value, headline })} />
      <TextField id="list-subheadline" label="Subheadline" value={value.subheadline} onChange={(subheadline) => onChange({ ...value, subheadline })} />
      <FormField label="Content type" required htmlFor="list-content-type">
        {(control) => (
          <Select
            {...control}
            value={value.contentType}
            onChange={(event) => onChange({ ...value, contentType: event.target.value, viewId: "" })}
          >
            <option value="">Select a content type</option>
            {types.map((type) => {
              const fields = entityFields(type);
              const developerName = readString(fields, "developerName");
              return (
                <option key={type.id} value={developerName}>
                  {readString(fields, "labelPlural", "labelSingular") || developerName || type.id}
                </option>
              );
            })}
          </Select>
        )}
      </FormField>
      <FormField label="View" htmlFor="list-view">
        {(control) => (
          <Select
            {...control}
            value={value.viewId}
            disabled={value.contentType.length === 0}
            onChange={(event) => onChange({ ...value, viewId: event.target.value })}
          >
            <option value="">Default view</option>
            {views.map((view) => {
              const fields = entityFields(view);
              return (
                <option key={view.id} value={view.id}>
                  {readString(fields, "label", "developerName") || view.id}
                </option>
              );
            })}
          </Select>
        )}
      </FormField>
      <TextField id="list-filter" label="Filter" value={value.filter} onChange={(filter) => onChange({ ...value, filter })} />
      <TextField id="list-order" label="Order by" value={value.orderBy} onChange={(orderBy) => onChange({ ...value, orderBy })} />
      <NumberField id="list-page-size" label="Page size" value={value.pageSize} onChange={(pageSize) => onChange({ ...value, pageSize })} />
      <ChoiceField id="list-style" label="Display style" value={value.displayStyle} options={DISPLAY_STYLES} onChange={(displayStyle) => onChange({ ...value, displayStyle })} />
      <label className="flex items-center gap-2 text-sm">
        <Checkbox checked={value.showImage} onCheckedChange={(showImage) => onChange({ ...value, showImage })} />
        Show image
      </label>
      <label className="flex items-center gap-2 text-sm">
        <Checkbox checked={value.showDate} onCheckedChange={(showDate) => onChange({ ...value, showDate })} />
        Show date
      </label>
      <label className="flex items-center gap-2 text-sm">
        <Checkbox checked={value.showExcerpt} onCheckedChange={(showExcerpt) => onChange({ ...value, showExcerpt })} />
        Show excerpt
      </label>
      <TextField id="list-link-text" label="View all text" value={value.linkText} onChange={(linkText) => onChange({ ...value, linkText })} />
      <TextField id="list-link-url" label="View all URL" value={value.linkUrl} onChange={(linkUrl) => onChange({ ...value, linkUrl })} />
      <TextField id="list-bg" label="Background color" value={value.backgroundColor} onChange={(backgroundColor) => onChange({ ...value, backgroundColor })} />
    </div>
  );
}

function TextField({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <FormField label={label} htmlFor={id}>
      {(control) => <Input {...control} value={value} onChange={(event) => onChange(event.target.value)} />}
    </FormField>
  );
}

function NumberField({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <FormField label={label} htmlFor={id}>
      {(control) => (
        <Input
          {...control}
          type="number"
          value={Number.isFinite(value) ? String(value) : ""}
          onChange={(event) => {
            const parsed = Number(event.target.value);
            onChange(Number.isFinite(parsed) ? parsed : 0);
          }}
        />
      )}
    </FormField>
  );
}

function ChoiceField({
  id,
  label,
  value,
  options,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  options: readonly string[];
  onChange: (value: string) => void;
}) {
  return (
    <FormField label={label} htmlFor={id}>
      {(control) => (
        <Select {...control} value={value} onChange={(event) => onChange(event.target.value)}>
          {options.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>
      )}
    </FormField>
  );
}

function HtmlField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-2">
      <p className="text-sm font-medium">{label}</p>
      <RichTextEditor content={value} onHtmlChange={onChange} ariaLabel={label} />
    </div>
  );
}

function ImageField({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-2">
      <TextField id={id} label={label} value={value} onChange={onChange} />
      {value ? (
        <img src={value} alt="" className="max-h-32 rounded-lg border border-border object-contain" />
      ) : null}
      <FileUpload
        height={180}
        allowedFileTypes={["image/*"]}
        note="Upload an image or paste a URL above."
        onUploaded={(files) => {
          const first = files[0];
          if (first) {
            onChange(first.url);
          }
        }}
      />
    </div>
  );
}
