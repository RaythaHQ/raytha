import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  DangerZone,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { BackgroundTaskStatus } from "../components/background-task-status";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { ThemeSectionHeader } from "./editors/theme-section-header";
import { entityFields, readBoolean, readString, toDeveloperName } from "./entity";
import { Link, useNavigate, useParams } from "@tanstack/react-router";

export function ThemesPage() {
  const queryClient = useQueryClient();
  const [importOpen, setImportOpen] = useState(false);
  const [taskId, setTaskId] = useState<string | null>(null);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["themes"] });
    void queryClient.invalidateQueries({ queryKey: ["configuration"] });
  };

  return (
    <div className="space-y-4">
      <CrudListPage
        title="Themes"
        queryKey={["themes"]}
        listKey="themes"
        noun="theme"
        list={adminApi.themes.list}
        createPermission={platformPermissions.templates}
        createLabel="New theme"
        createTo="/themes/new"
        columns={[
          {
            header: "Title",
            cell: (entity) => (
              <Link to="/themes/$themeId" params={{ themeId: entity.id }} className="text-primary hover:underline">
                {readString(entityFields(entity), "title") || entity.id}
              </Link>
            ),
          },
          { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
          { header: "Description", cell: (entity) => readString(entityFields(entity), "description") || "—" },
          {
            header: "Status",
            cell: (entity) => {
              const fields = entityFields(entity);
              const isActive = readBoolean(fields, "isActive");
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

  const [synced, setSynced] = useState<EntityRef | null>(null);
  if (synced !== theme) {
    setSynced(theme);
    setTitle(sourceTitle ? `${sourceTitle} copy` : "");
    setDeveloperName(sourceDeveloper ? `${sourceDeveloper}_copy` : "");
    setDescription(sourceDescription);
  }

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

export function ThemeSettingsPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  useDocumentTitle(["Theme settings"]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [duplicateOpen, setDuplicateOpen] = useState(false);
  const [taskId, setTaskId] = useState<string | null>(null);

  const themeQuery = useQuery({
    queryKey: ["themes", themeId],
    queryFn: () => adminApi.themes.get(themeId),
    enabled: themeId.length > 0,
  });
  const configQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });

  if (!themeId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Theme settings" />
        <p className="text-sm text-muted-foreground">Pick a theme from the list.</p>
      </div>
    );
  }

  const activeThemeId =
    typeof configQuery.data?.activeThemeId === "string" ? configQuery.data.activeThemeId : "";

  return (
    <QueryGate query={themeQuery}>
      {(theme) => (
        <ThemeSettingsForm
          theme={theme}
          activeThemeId={activeThemeId}
          taskId={taskId}
          duplicateOpen={duplicateOpen}
          onDuplicateOpen={setDuplicateOpen}
          onTask={(id) => {
            setTaskId(id);
            void queryClient.invalidateQueries({ queryKey: ["themes"] });
          }}
          onDeleted={() => {
            void queryClient.invalidateQueries({ queryKey: ["themes"] });
            void navigate({ to: "/themes" });
          }}
        />
      )}
    </QueryGate>
  );
}

function ThemeSettingsForm({
  theme,
  activeThemeId,
  taskId,
  duplicateOpen,
  onDuplicateOpen,
  onTask,
  onDeleted,
}: {
  theme: EntityRef;
  activeThemeId: string;
  taskId: string | null;
  duplicateOpen: boolean;
  onDuplicateOpen: (open: boolean) => void;
  onTask: (id: string) => void;
  onDeleted: () => void;
}) {
  const queryClient = useQueryClient();
  const fields = entityFields(theme);
  const isActive = theme.id === activeThemeId || readBoolean(fields, "isActive");
  const isExportable = readBoolean(fields, "isExportable");
  const [title, setTitle] = useState(readString(fields, "title"));
  const [description, setDescription] = useState(readString(fields, "description"));

  const [synced, setSynced] = useState(theme);
  if (synced !== theme) {
    setSynced(theme);
    setTitle(readString(fields, "title"));
    setDescription(readString(fields, "description"));
  }

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["themes"] });
    void queryClient.invalidateQueries({ queryKey: ["themes", theme.id] });
    void queryClient.invalidateQueries({ queryKey: ["configuration"] });
  };

  const save = useMutation({
    mutationFn: () => adminApi.themes.update(theme.id, { title, description }),
    onSuccess: () => {
      toast.success("Theme saved");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const setActive = useMutation({
    mutationFn: () => adminApi.themes.setActive(theme.id),
    onSuccess: () => {
      toast.success("Active theme updated");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const setExportability = useMutation({
    mutationFn: (next: boolean) => adminApi.themes.setExportability(theme.id, { isExportable: next }),
    onSuccess: () => {
      toast.success("Exportability updated");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: () => adminApi.themes.remove(theme.id),
    onSuccess: () => {
      toast.success("Theme deleted");
      onDeleted();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <ThemeSectionHeader themeId={theme.id} active="settings" />
      {taskId ? <BackgroundTaskStatus taskId={taskId} /> : null}
      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              save.mutate();
            }}
          >
            <FormField label="Title" required htmlFor="theme-settings-title">
              {(control) => <Input {...control} value={title} onChange={(event) => setTitle(event.target.value)} />}
            </FormField>
            <FormField label="Description" required htmlFor="theme-settings-description">
              {(control) => (
                <Textarea {...control} value={description} onChange={(event) => setDescription(event.target.value)} />
              )}
            </FormField>
            <Button type="submit" loading={save.isPending}>
              Save
            </Button>
          </form>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Publishing</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {isActive ? (
            <p className="text-sm text-muted-foreground">This theme is the one visitors see.</p>
          ) : (
            <Button type="button" variant="outline" loading={setActive.isPending} onClick={() => setActive.mutate()}>
              Set as active theme
            </Button>
          )}
          <div className="flex items-center gap-2">
            <Checkbox
              id="theme-exportable"
              checked={isExportable}
              disabled={setExportability.isPending}
              onCheckedChange={(checked) => setExportability.mutate(checked)}
            />
            <label htmlFor="theme-exportable" className="text-sm">
              Allow this theme to be exported
            </label>
          </div>
          <Button type="button" variant="outline" onClick={() => onDuplicateOpen(true)}>
            Duplicate theme
          </Button>
        </CardContent>
      </Card>
      {isActive ? (
        <p className="text-sm text-muted-foreground">Set another theme active before deleting this one.</p>
      ) : (
        <DangerZone
          description="Delete this theme and its templates."
          actionLabel="Delete theme"
          confirmTitle="Delete theme?"
          confirmBody="This cannot be undone."
          onConfirm={() => remove.mutate()}
          pending={remove.isPending}
        />
      )}
      <DuplicateThemeDialog
        theme={duplicateOpen ? theme : null}
        onOpenChange={onDuplicateOpen}
        onStarted={onTask}
      />
    </div>
  );
}

export function NewThemePage() {
  useDocumentTitle(["New theme"]);
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [description, setDescription] = useState("");
  const [insertDefaultThemeMediaItems, setInsertDefaultThemeMediaItems] = useState(false);

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.themes.create({ title, developerName, description, insertDefaultThemeMediaItems }),
    onSuccess: () => {
      toast.success("Theme created");
      void navigate({ to: "/themes" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/themes" listKey="themes" label="themes" />} title="New theme" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Title" required htmlFor="theme-title">
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
            <FormField label="Developer name" required htmlFor="theme-developer">
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
            <FormField label="Description" required htmlFor="theme-description">
              {(control) => (
                <Textarea {...control} value={description} onChange={(event) => setDescription(event.target.value)} />
              )}
            </FormField>
            <div className="flex items-center gap-2">
              <Checkbox
                id="theme-default-media"
                checked={insertDefaultThemeMediaItems}
                onCheckedChange={setInsertDefaultThemeMediaItems}
              />
              <label htmlFor="theme-default-media" className="text-sm">
                Include default media
              </label>
            </div>
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
