import { adminApi, formatError, platformPermissions } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormField,
  Input,
  PageHeader,
  Select,
  toast,
} from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import type { ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { useRef, useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CodeEditor, insertAtCursor } from "./editors/code-editor";
import {
  DEFAULT_TRIGGER,
  FUNCTION_TRIGGERS,
  missingEntryPoint,
  starterCode,
  triggerFor,
  type FunctionTriggerType,
} from "./editors/function-reference";
import { FunctionReferencePanel } from "./editors/function-reference-panel";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, isRecord, readBoolean, readString, toDeveloperName } from "./entity";

export function EmailTemplatesPage() {
  return (
    <CrudListPage
      title="Email templates"
      queryKey={["email-templates"]}
      listKey="email-templates"
      noun="email template"
      list={adminApi.emailTemplates.list}
      columns={[
        {
          header: "Subject",
          cell: (entity) => (
            <Link to="/email-templates/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "subject") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
      ]}
    />
  );
}

export function MenusPage() {
  return (
    <CrudListPage
      title="Menus"
      queryKey={["menus"]}
      listKey="menus"
      noun="menu"
      list={adminApi.menus.list}
      createPermission={platformPermissions.contentTypes}
      createLabel="New menu"
      createTo="/menus/new"
      columns={[
        {
          header: "Label",
          cell: (entity) => (
            <Link to="/menus/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "label") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        {
          header: "Main",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isMainMenu") ? <Badge variant="info">Main</Badge> : "—",
        },
      ]}
    />
  );
}

export function FunctionsPage() {
  return (
    <CrudListPage
      title="Functions"
      queryKey={["functions"]}
      listKey="functions"
      noun="function"
      list={adminApi.functions.list}
      createPermission={platformPermissions.systemSettings}
      createLabel="New function"
      createTo="/functions/new"
      columns={[
        {
          header: "Name",
          cell: (entity) => (
            <Link to="/functions/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "name") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        {
          header: "Trigger",
          cell: (entity) => {
            const trigger = entityFields(entity).triggerType;
            const value = isRecord(trigger) ? readString(trigger, "developerName") : readString({ trigger }, "trigger");
            return value ? triggerFor(value).label : "—";
          },
        },
        {
          header: "Status",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isActive") ? (
              <Badge variant="success">Active</Badge>
            ) : (
              <Badge variant="secondary">Inactive</Badge>
            ),
        },
      ]}
    />
  );
}

export function NewMenuPage() {
  useDocumentTitle(["New menu"]);
  const navigate = useNavigate();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);

  const mutation = useMutation({
    mutationFn: () => adminApi.menus.create({ label, developerName }),
    onSuccess: (created) => {
      toast.success("Menu created");
      void navigate({ to: "/menus/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/menus" listKey="menus" label="menus" />} title="New menu" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="menu-label">
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    const next = event.target.value;
                    setLabel(next);
                    if (!developerTouched) {
                      setDeveloperName(toDeveloperName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="menu-developer">
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
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function NewFunctionPage() {
  useDocumentTitle(["New function"]);
  const navigate = useNavigate();
  const editorRef = useRef<ReactCodeMirrorRef>(null);
  const [name, setName] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [triggerType, setTriggerType] = useState<FunctionTriggerType>(DEFAULT_TRIGGER);
  /** null while the editor still shows the untouched starter, which follows the trigger and developer name. */
  const [editedCode, setEditedCode] = useState<string | null>(null);
  const [offerStarter, setOfferStarter] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const starter = starterCode(triggerType, developerName);
  const code = editedCode ?? starter;
  const entryWarning = missingEntryPoint(triggerType, code);

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.functions.create({ name, developerName, triggerType, code, isActive }),
    onSuccess: (created) => {
      toast.success("Function created");
      void navigate({ to: "/functions/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const changeTrigger = (next: FunctionTriggerType) => {
    setTriggerType(next);
    setOfferStarter(editedCode !== null && editedCode !== starterCode(next, developerName));
  };

  /** Goes through the view so the swap is immediate and Ctrl+Z brings the edited code back. */
  const replaceWithStarter = () => {
    const view = editorRef.current?.view;
    view?.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: starter }, userEvent: "input.replace" });
    setEditedCode(null);
    setOfferStarter(false);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/functions" listKey="functions" label="functions" />}
        title="New function"
        description="Server-side JavaScript that runs on a request, in a template, or when content changes."
      />
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_24rem] lg:items-start">
        <Card>
          <CardContent className="pt-6">
            <form
              className="space-y-4"
              onSubmit={(event: FormEvent) => {
                event.preventDefault();
                mutation.mutate();
              }}
            >
              <FormField label="Name" required htmlFor="fn-name">
                {(control) => (
                  <Input
                    {...control}
                    value={name}
                    onChange={(event) => {
                      const next = event.target.value;
                      setName(next);
                      if (!developerTouched) {
                        setDeveloperName(toDeveloperName(next));
                      }
                    }}
                  />
                )}
              </FormField>
              <FormField label="Developer name" required htmlFor="fn-developer">
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
              <FormField label="Trigger" required htmlFor="fn-trigger">
                {(control) => (
                  <Select
                    {...control}
                    value={triggerType}
                    onChange={(event) => changeTrigger(triggerFor(event.target.value).value)}
                  >
                    {FUNCTION_TRIGGERS.map((trigger) => (
                      <option key={trigger.value} value={trigger.value}>
                        {trigger.label}
                      </option>
                    ))}
                  </Select>
                )}
              </FormField>
              {offerStarter ? (
                <div
                  role="status"
                  className="flex flex-wrap items-center gap-2 rounded-lg border border-info-border bg-info-soft px-3 py-2 text-sm"
                >
                  <span className="min-w-0 flex-1">
                    Replace your code with the {triggerFor(triggerType).label} starter? You can undo with Ctrl+Z.
                  </span>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={replaceWithStarter}
                  >
                    Replace
                  </Button>
                  <Button type="button" size="sm" variant="ghost" onClick={() => setOfferStarter(false)}>
                    Keep my code
                  </Button>
                </div>
              ) : null}
              <div className="space-y-2">
                <label htmlFor="fn-code" className="text-sm font-medium">
                  Code
                </label>
                <CodeEditor
                  value={code}
                  onChange={(next) => setEditedCode(next === starter ? null : next)}
                  language="javascript"
                  editorRef={editorRef}
                  ariaLabel="Function code"
                />
                {entryWarning ? <p className="text-sm text-warning">{entryWarning}</p> : null}
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="fn-active" checked={isActive} onCheckedChange={setIsActive} />
                <label htmlFor="fn-active" className="text-sm">
                  Active
                </label>
              </div>
              <Button type="submit" loading={mutation.isPending}>
                Create
              </Button>
            </form>
          </CardContent>
        </Card>
        <FunctionReferencePanel
          trigger={triggerType}
          developerName={developerName}
          onInsert={(text) => insertAtCursor(editorRef.current, text)}
        />
      </div>
    </div>
  );
}
