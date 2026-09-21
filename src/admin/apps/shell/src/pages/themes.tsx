import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { BackgroundTaskStatus } from "../components/background-task-status";
import { CrudListPage } from "./crud-list";
import { entityFields, readBoolean, readString, toDeveloperName } from "./entity";
import { Link } from "@tanstack/react-router";

export function ThemesPage() {
  const queryClient = useQueryClient();
  const [importOpen, setImportOpen] = useState(false);
  const [duplicate, setDuplicate] = useState<EntityRef | null>(null);
  const [taskId, setTaskId] = useState<string | null>(null);

  const configQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  const activeThemeId =
    typeof configQuery.data?.activeThemeId === "string" ? configQuery.data.activeThemeId : "";

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["themes"] });
    void queryClient.invalidateQueries({ queryKey: ["configuration"] });
  };

  const setActive = useMutation({
    mutationFn: (id: string) => adminApi.themes.setActive(id),
    onSuccess: () => {
      toast.success("Active theme updated");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const setExportability = useMutation({
    mutationFn: ({ id, isExportable }: { id: string; isExportable: boolean }) =>
      adminApi.themes.setExportability(id, { isExportable }),
    onSuccess: () => {
      toast.success("Exportability updated");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-4">
      <CrudListPage
        title="Themes"
        queryKey={["themes"]}
        noun="theme"
        list={adminApi.themes.list}
        create={adminApi.themes.create}
        remove={adminApi.themes.remove}
        createPermission={platformPermissions.templates}
        createLabel="New theme"
        createFields={[
          { key: "title", label: "Title", required: true, autoDeveloperNameFrom: "developerName" },
          { key: "developerName", label: "Developer name", required: true },
          { key: "description", label: "Description", type: "textarea", required: true },
          { key: "insertDefaultThemeMediaItems", label: "Include default media", type: "checkbox" },
        ]}
        columns={[
          { header: "Title", cell: (entity) => readString(entityFields(entity), "title") || entity.id },
          { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
          { header: "Description", cell: (entity) => readString(entityFields(entity), "description") || "—" },
          {
            header: "Status",
            cell: (entity) => {
              const fields = entityFields(entity);
              const isActive = entity.id === activeThemeId || readBoolean(fields, "isActive");
              const isExportable = readBoolean(fields, "isExportable");
              return (
                <span className="flex flex-wrap gap-1">
                  {isActive ? <Badge variant="success">Active</Badge> : null}
                  {isExportable ? <Badge variant="info">Exportable</Badge> : <Badge variant="secondary">Hidden from export</Badge>}
                </span>
              );
            },
          },
        ]}
        actions={
          <Button type="button" variant="outline" onClick={() => setImportOpen(true)}>
            Import from URL
          </Button>
        }
        rowActions={(entity) => {
          const fields = entityFields(entity);
          const isActive = entity.id === activeThemeId || readBoolean(fields, "isActive");
          const isExportable = readBoolean(fields, "isExportable");
          return (
            <>
              <Link
                to="/themes/$themeId/web-templates"
                params={{ themeId: entity.id }}
                className="text-sm text-primary hover:underline"
              >
                Web templates
              </Link>
              <Link
                to="/themes/$themeId/widget-templates"
                params={{ themeId: entity.id }}
                className="text-sm text-primary hover:underline"
              >
                Widgets
              </Link>
              <Button
                type="button"
                size="sm"
                variant="ghost"
                disabled={isActive || setActive.isPending}
                onClick={() => setActive.mutate(entity.id)}
              >
                Set active
              </Button>
              <Button type="button" size="sm" variant="ghost" onClick={() => setDuplicate(entity)}>
                Duplicate
              </Button>
              <Button
                type="button"
                size="sm"
                variant="ghost"
                disabled={setExportability.isPending}
                onClick={() => setExportability.mutate({ id: entity.id, isExportable: !isExportable })}
              >
                {isExportable ? "Make private" : "Make exportable"}
              </Button>
            </>
          );
        }}
      />
      {taskId ? <BackgroundTaskStatus taskId={taskId} /> : null}
      <ImportThemeDialog
        open={importOpen}
        onOpenChange={setImportOpen}
        onStarted={(id) => {
          setTaskId(id);
          invalidate();
        }}
      />
      <DuplicateThemeDialog
        theme={duplicate}
        onOpenChange={(open) => {
          if (!open) {
            setDuplicate(null);
          }
        }}
        onStarted={(id) => {
          setTaskId(id);
          invalidate();
        }}
      />
    </div>
  );
}

function ImportThemeDialog({
  open,
  onOpenChange,
  onStarted,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onStarted: (taskId: string) => void;
}) {
  const [title, setTitle] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [description, setDescription] = useState("");
  const [url, setUrl] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.themes.importFromUrl({
        title,
        developerName: developerName || toDeveloperName(title),
        description,
        url,
      }),
    onSuccess: (result) => {
      toast.success(`Import started. Task ${result.id}`);
      onStarted(result.id);
      onOpenChange(false);
      setTitle("");
      setDeveloperName("");
      setDeveloperTouched(false);
      setDescription("");
      setUrl("");
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        onOpenChange(next);
        if (!next) {
          setTitle("");
          setDeveloperName("");
          setDeveloperTouched(false);
          setDescription("");
          setUrl("");
        }
      }}
    >
      <form onSubmit={handleSubmit}>
        <DialogHeader>
          <DialogTitle>Import theme from URL</DialogTitle>
        </DialogHeader>
        <DialogContent className="space-y-4">
          <FormField label="Title" required htmlFor="import-theme-title">
            {(control) => (
              <Input
                {...control}
                value={title}
                onChange={(event) => {
                  const next = event.target.value;
                  setTitle(next);
                  if (!developerTouched) {
                    setDeveloperName(toDeveloperName(next));
                  }
                }}
              />
            )}
          </FormField>
          <FormField label="Developer name" required htmlFor="import-theme-developer-name">
            {(control) => (
              <Input
                {...control}
                value={developerName}
                onChange={(event) => {
                  setDeveloperTouched(true);
                  setDeveloperName(event.target.value);
                }}
              />
            )}
          </FormField>
          <FormField label="Description" required htmlFor="import-theme-description">
            {(control) => (
              <Textarea
                {...control}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
              />
            )}
          </FormField>
          <FormField label="Package URL" required htmlFor="import-theme-url">
            {(control) => (
              <Input
                {...control}
                type="url"
                value={url}
                onChange={(event) => setUrl(event.target.value)}
              />
            )}
          </FormField>
        </DialogContent>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending}>
            Import
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  );
}

function DuplicateThemeDialog({
  theme,
  onOpenChange,
  onStarted,
}: {
  theme: EntityRef | null;
  onOpenChange: (open: boolean) => void;
  onStarted: (taskId: string) => void;
}) {
  const fields = theme ? entityFields(theme) : {};
  const sourceTitle = readString(fields, "title");
  const sourceDeveloper = readString(fields, "developerName");
  const sourceDescription = readString(fields, "description");
  const [title, setTitle] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [description, setDescription] = useState("");

  useEffect(() => {
    if (!theme) {
      setTitle("");
      setDeveloperName("");
      setDescription("");
      return;
    }
    setTitle(sourceTitle ? `${sourceTitle} copy` : "");
    setDeveloperName(sourceDeveloper ? `${sourceDeveloper}_copy` : "");
    setDescription(sourceDescription);
  }, [theme, sourceTitle, sourceDeveloper, sourceDescription]);

  const mutation = useMutation({
    mutationFn: () => {
      if (!theme) {
        throw new Error("Pick a theme to duplicate.");
      }
      return adminApi.themes.duplicate(theme.id, { title, developerName, description });
    },
    onSuccess: (result) => {
      toast.success(`Duplicate started. Task ${result.id}`);
      onStarted(result.id);
      onOpenChange(false);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <Dialog open={theme !== null} onOpenChange={onOpenChange}>
      <form onSubmit={handleSubmit}>
        <DialogHeader>
          <DialogTitle>Duplicate theme</DialogTitle>
        </DialogHeader>
        <DialogContent className="space-y-4">
          <FormField label="Title" required htmlFor="duplicate-theme-title">
            {(control) => (
              <Input {...control} value={title} onChange={(event) => setTitle(event.target.value)} />
            )}
          </FormField>
          <FormField label="Developer name" required htmlFor="duplicate-theme-developer-name">
            {(control) => (
              <Input
                {...control}
                value={developerName}
                onChange={(event) => setDeveloperName(event.target.value)}
              />
            )}
          </FormField>
          <FormField label="Description" required htmlFor="duplicate-theme-description">
            {(control) => (
              <Textarea
                {...control}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
              />
            )}
          </FormField>
        </DialogContent>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending}>
            Duplicate
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  );
}
