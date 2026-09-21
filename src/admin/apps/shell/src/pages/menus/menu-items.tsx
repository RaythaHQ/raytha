import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { JsonObject, MenuItemDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Checkbox,
  ConfirmDialog,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";

type ItemForm = {
  label: string;
  url: string;
  isDisabled: boolean;
  openInNewTab: boolean;
  cssClassName: string;
  parentNavigationMenuItemId: string;
};

const emptyForm: ItemForm = {
  label: "",
  url: "",
  isDisabled: false,
  openInNewTab: false,
  cssClassName: "",
  parentNavigationMenuItemId: "",
};

export function MenuItemsPage() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const queryClient = useQueryClient();
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<MenuItemDetail | null>(null);
  const [form, setForm] = useState<ItemForm>(emptyForm);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const menuQuery = useQuery({
    queryKey: ["menu", id],
    queryFn: () => adminApi.menus.get(id),
    enabled: id.length > 0,
  });
  const itemsQuery = useQuery({
    queryKey: ["menu-items", id],
    queryFn: () => adminApi.menus.items(id).list(),
    enabled: id.length > 0,
  });

  useDocumentTitle([menuQuery.data?.label ?? "Menu items"]);

  const create = useMutation({
    mutationFn: (input: JsonObject) => adminApi.menus.items(id).create(input),
    onSuccess: () => {
      toast.success("Menu item created");
      void queryClient.invalidateQueries({ queryKey: ["menu-items", id] });
      closeForm();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const update = useMutation({
    mutationFn: ({ itemId, input }: { itemId: string; input: JsonObject }) =>
      adminApi.menus.items(id).update(itemId, input),
    onSuccess: () => {
      toast.success("Menu item saved");
      void queryClient.invalidateQueries({ queryKey: ["menu-items", id] });
      closeForm();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: (itemId: string) => adminApi.menus.items(id).remove(itemId),
    onSuccess: () => {
      toast.success("Menu item deleted");
      void queryClient.invalidateQueries({ queryKey: ["menu-items", id] });
      setDeleteId(null);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const reorder = useMutation({
    mutationFn: ({ itemId, ordinal }: { itemId: string; ordinal: number }) =>
      adminApi.menus.items(id).reorder(itemId, ordinal),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["menu-items", id] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
    setForm(emptyForm);
  };

  const openCreate = (parentId: string) => {
    setEditing(null);
    setForm({ ...emptyForm, parentNavigationMenuItemId: parentId });
    setFormOpen(true);
  };

  const openEdit = (item: MenuItemDetail) => {
    setEditing(item);
    setForm({
      label: item.label,
      url: item.url,
      isDisabled: item.isDisabled,
      openInNewTab: item.openInNewTab,
      cssClassName: item.cssClassName,
      parentNavigationMenuItemId: item.parentNavigationMenuItemId ?? "",
    });
    setFormOpen(true);
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    const input: JsonObject = {
      label: form.label,
      url: form.url,
      isDisabled: form.isDisabled,
      openInNewTab: form.openInNewTab,
      cssClassName: form.cssClassName,
      parentNavigationMenuItemId: form.parentNavigationMenuItemId.length > 0 ? form.parentNavigationMenuItemId : null,
      navigationMenuId: id,
    };
    if (editing) {
      update.mutate({ itemId: editing.id, input });
    } else {
      create.mutate(input);
    }
  };

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Menu items" />
        <p className="text-sm text-muted-foreground">Pick a menu from the list.</p>
      </div>
    );
  }

  const items = itemsQuery.data ?? [];
  const tree = buildTree(items);
  const parentChoices = parentOptions(items, editing?.id ?? null);

  return (
    <div className="space-y-6">
      <PageHeader
        title={menuQuery.data?.label || "Menu items"}
        description={menuQuery.data?.developerName}
        actions={
          hasPermission(platformPermissions.contentTypes) ? (
            <Button type="button" onClick={() => openCreate("")}>
              New item
            </Button>
          ) : undefined
        }
      />
      <p className="text-sm">
        <Link to="/menus" className="text-primary hover:underline">
          Back to menus
        </Link>
      </p>
      <QueryGate query={itemsQuery}>
        {() =>
          tree.length === 0 ? (
            <EmptyState icon={Inbox} title="No menu items" hint="Add the first link in this menu." />
          ) : (
            <ul className="space-y-2" aria-label="Menu items">
              {tree.map((node, index) => (
                <MenuItemRow
                  key={node.item.id}
                  node={node}
                  depth={0}
                  isFirst={index === 0}
                  isLast={index === tree.length - 1}
                  canEdit={hasPermission(platformPermissions.contentTypes)}
                  busy={reorder.isPending}
                  onEdit={openEdit}
                  onAddChild={openCreate}
                  onDelete={setDeleteId}
                  onMove={(itemId, ordinal) => reorder.mutate({ itemId, ordinal })}
                />
              ))}
            </ul>
          )
        }
      </QueryGate>
      <Dialog
        open={formOpen}
        onOpenChange={(open) => {
          if (!open) {
            closeForm();
          }
        }}
      >
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{editing ? "Edit menu item" : "New menu item"}</DialogTitle>
          </DialogHeader>
          <DialogContent className="space-y-4">
            <FormField label="Label" required htmlFor="menu-item-label">
              {(control) => (
                <Input
                  {...control}
                  value={form.label}
                  onChange={(event) => setForm({ ...form, label: event.target.value })}
                />
              )}
            </FormField>
            <FormField label="URL" required htmlFor="menu-item-url">
              {(control) => (
                <Input
                  {...control}
                  value={form.url}
                  onChange={(event) => setForm({ ...form, url: event.target.value })}
                />
              )}
            </FormField>
            <FormField label="CSS class" htmlFor="menu-item-css">
              {(control) => (
                <Input
                  {...control}
                  value={form.cssClassName}
                  onChange={(event) => setForm({ ...form, cssClassName: event.target.value })}
                />
              )}
            </FormField>
            <FormField label="Parent" htmlFor="menu-item-parent">
              {(control) => (
                <Select
                  {...control}
                  value={form.parentNavigationMenuItemId}
                  onChange={(event) => setForm({ ...form, parentNavigationMenuItemId: event.target.value })}
                >
                  <option value="">Top level</option>
                  {parentChoices.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.label || item.id}
                    </option>
                  ))}
                </Select>
              )}
            </FormField>
            <div className="flex items-center gap-2">
              <Checkbox
                id="menu-item-disabled"
                checked={form.isDisabled}
                onCheckedChange={(checked) => setForm({ ...form, isDisabled: checked })}
              />
              <label htmlFor="menu-item-disabled" className="text-sm">
                Disabled
              </label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="menu-item-new-tab"
                checked={form.openInNewTab}
                onCheckedChange={(checked) => setForm({ ...form, openInNewTab: checked })}
              />
              <label htmlFor="menu-item-new-tab" className="text-sm">
                Open in new tab
              </label>
            </div>
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={closeForm}>
              Cancel
            </Button>
            <Button type="submit" loading={create.isPending || update.isPending}>
              {editing ? "Save" : "Create"}
            </Button>
          </DialogFooter>
        </form>
      </Dialog>
      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteId(null);
          }
        }}
        title="Delete menu item?"
        body="This cannot be undone."
        onConfirm={() => {
          if (deleteId) {
            remove.mutate(deleteId);
          }
        }}
        pending={remove.isPending}
      />
    </div>
  );
}

type TreeNode = {
  item: MenuItemDetail;
  children: TreeNode[];
};

function buildTree(items: MenuItemDetail[]): TreeNode[] {
  const byParent = new Map<string, MenuItemDetail[]>();
  for (const item of items) {
    const key = item.parentNavigationMenuItemId ?? "";
    const siblings = byParent.get(key);
    if (siblings) {
      siblings.push(item);
    } else {
      byParent.set(key, [item]);
    }
  }
  for (const siblings of byParent.values()) {
    siblings.sort((a, b) => a.ordinal - b.ordinal);
  }
  const walk = (parentId: string): TreeNode[] =>
    (byParent.get(parentId) ?? []).map((item) => ({ item, children: walk(item.id) }));
  return walk("");
}

function descendantIds(items: MenuItemDetail[], rootId: string): Set<string> {
  const ids = new Set<string>();
  const walk = (parentId: string) => {
    for (const item of items) {
      if (item.parentNavigationMenuItemId === parentId) {
        ids.add(item.id);
        walk(item.id);
      }
    }
  };
  walk(rootId);
  return ids;
}

function parentOptions(items: MenuItemDetail[], editingId: string | null): MenuItemDetail[] {
  if (!editingId) {
    return [...items].sort((a, b) => a.label.localeCompare(b.label));
  }
  const nested = descendantIds(items, editingId);
  return items
    .filter((item) => item.id !== editingId && !nested.has(item.id))
    .sort((a, b) => a.label.localeCompare(b.label));
}

function MenuItemRow({
  node,
  depth,
  isFirst,
  isLast,
  canEdit,
  busy,
  onEdit,
  onAddChild,
  onDelete,
  onMove,
}: {
  node: TreeNode;
  depth: number;
  isFirst: boolean;
  isLast: boolean;
  canEdit: boolean;
  busy: boolean;
  onEdit: (item: MenuItemDetail) => void;
  onAddChild: (parentId: string) => void;
  onDelete: (id: string) => void;
  onMove: (itemId: string, ordinal: number) => void;
}) {
  const item = node.item;
  return (
    <li>
      <div
        className="flex flex-wrap items-center gap-2 rounded-lg border border-border bg-card px-3 py-2"
        style={{ marginLeft: depth * 16 }}
      >
        <div className="min-w-0 flex-1">
          <div className="font-medium">{item.label || item.id}</div>
          <div className="truncate text-sm text-muted-foreground">{item.url}</div>
        </div>
        {item.isDisabled ? <Badge variant="secondary">Disabled</Badge> : null}
        {item.openInNewTab ? <Badge variant="info">New tab</Badge> : null}
        {canEdit ? (
          <div className="flex flex-wrap items-center gap-1">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={busy || isFirst || item.ordinal <= 1}
              onClick={() => onMove(item.id, item.ordinal - 1)}
            >
              Up
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={busy || isLast}
              onClick={() => onMove(item.id, item.ordinal + 1)}
            >
              Down
            </Button>
            <Button type="button" variant="outline" size="sm" onClick={() => onAddChild(item.id)}>
              Add child
            </Button>
            <Button type="button" variant="outline" size="sm" onClick={() => onEdit(item)}>
              Edit
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => onDelete(item.id)}>
              Delete
            </Button>
          </div>
        ) : null}
      </div>
      {node.children.length > 0 ? (
        <ul className="mt-2 space-y-2">
          {node.children.map((child, index) => (
            <MenuItemRow
              key={child.item.id}
              node={child}
              depth={depth + 1}
              isFirst={index === 0}
              isLast={index === node.children.length - 1}
              canEdit={canEdit}
              busy={busy}
              onEdit={onEdit}
              onAddChild={onAddChild}
              onDelete={onDelete}
              onMove={onMove}
            />
          ))}
        </ul>
      ) : null}
    </li>
  );
}
