import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { JsonObject, MenuItemDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  ConfirmDialog,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  RowActions,
  Select,
  toast,
} from "@raytha/ui";
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useLocation, useNavigate, useParams } from "@tanstack/react-router";
import { GripVertical, Inbox } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
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

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

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

  const onDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }
    const activeItem = items.find((item) => item.id === String(active.id));
    const overItem = items.find((item) => item.id === String(over.id));
    if (!activeItem || !overItem) {
      return;
    }
    const parentId = activeItem.parentNavigationMenuItemId ?? "";
    if ((overItem.parentNavigationMenuItemId ?? "") !== parentId) {
      return;
    }
    const siblings = items
      .filter((item) => (item.parentNavigationMenuItemId ?? "") === parentId)
      .sort((left, right) => left.ordinal - right.ordinal);
    const oldIndex = siblings.findIndex((item) => item.id === activeItem.id);
    const newIndex = siblings.findIndex((item) => item.id === overItem.id);
    if (oldIndex < 0 || newIndex < 0) {
      return;
    }
    const moved = arrayMove(siblings, oldIndex, newIndex)[newIndex];
    const target = siblings[newIndex];
    if (!moved || !target) {
      return;
    }
    reorder.mutate({ itemId: moved.id, ordinal: target.ordinal });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={menuQuery.data?.label || "Menu items"}
        description={menuQuery.data?.developerName}
        actions={
          hasPermission(platformPermissions.contentTypes) ? (
            <Link
              to="/menus/$id/items/new"
              params={{ id }}
              className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600"
            >
              New item
            </Link>
          ) : undefined
        }
      />
      <ListBackLink to="/menus" listKey="menus" label="menus" />
      <QueryGate query={itemsQuery}>
        {() =>
          tree.length === 0 ? (
            <EmptyState icon={Inbox} title="No menu items" hint="Add the first link in this menu." />
          ) : (
            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
              <SortableItemTree
                nodes={tree}
                menuId={id}
                canEdit={hasPermission(platformPermissions.contentTypes)}
                onDelete={setDeleteId}
              />
            </DndContext>
          )
        }
      </QueryGate>
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

export function NewMenuItemPage() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const location = useLocation();
  const parentFromSearch = new URLSearchParams(
    location.searchStr.startsWith("?") ? location.searchStr.slice(1) : location.searchStr,
  ).get("parent");
  return (
    <MenuItemEditor
      menuId={id}
      itemId={null}
      initialParentId={parentFromSearch && parentFromSearch.length > 0 ? parentFromSearch : ""}
    />
  );
}

export function EditMenuItemPage() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const itemId = "itemId" in params && typeof params.itemId === "string" ? params.itemId : "";
  return <MenuItemEditor menuId={id} itemId={itemId || null} initialParentId="" />;
}

function MenuItemEditor({
  menuId,
  itemId,
  initialParentId,
}: {
  menuId: string;
  itemId: string | null;
  initialParentId: string;
}) {
  const isNew = itemId === null;
  useDocumentTitle([isNew ? "New menu item" : "Edit menu item"]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<ItemForm>({ ...emptyForm, parentNavigationMenuItemId: initialParentId });
  const [hydrated, setHydrated] = useState(isNew);

  const itemsQuery = useQuery({
    queryKey: ["menu-items", menuId],
    queryFn: () => adminApi.menus.items(menuId).list(),
    enabled: menuId.length > 0,
  });

  const items = itemsQuery.data ?? [];
  const editing = items.find((item) => item.id === itemId) ?? null;

  useEffect(() => {
    if (isNew || hydrated || !editing) {
      return;
    }
    setForm({
      label: editing.label,
      url: editing.url,
      isDisabled: editing.isDisabled,
      openInNewTab: editing.openInNewTab,
      cssClassName: editing.cssClassName,
      parentNavigationMenuItemId: editing.parentNavigationMenuItemId ?? "",
    });
    setHydrated(true);
  }, [editing, hydrated, isNew]);

  const mutation = useMutation({
    mutationFn: () => {
      const input: JsonObject = {
        label: form.label,
        url: form.url,
        isDisabled: form.isDisabled,
        openInNewTab: form.openInNewTab,
        cssClassName: form.cssClassName,
        parentNavigationMenuItemId: form.parentNavigationMenuItemId.length > 0 ? form.parentNavigationMenuItemId : null,
        navigationMenuId: menuId,
      };
      if (isNew) {
        return adminApi.menus.items(menuId).create(input);
      }
      return adminApi.menus.items(menuId).update(itemId, input);
    },
    onSuccess: () => {
      toast.success(isNew ? "Menu item created" : "Menu item saved");
      void queryClient.invalidateQueries({ queryKey: ["menu-items", menuId] });
      void navigate({ to: "/menus/$id", params: { id: menuId } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const parentChoices = parentOptions(items, itemId);

  if (!menuId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Menu item" />
        <p className="text-sm text-muted-foreground">Pick a menu from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title={isNew ? "New menu item" : editing?.label || "Edit menu item"} />
      <ListBackLink to="/menus/$id" params={{ id: menuId }} listKey={`menu-items:${menuId}`} label="menu items" />
      <Card>
        <CardContent className="pt-6">
          <QueryGate query={itemsQuery}>
            {() => (
              <form
                className="space-y-4"
                onSubmit={(event: FormEvent) => {
                  event.preventDefault();
                  mutation.mutate();
                }}
              >
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
                <Button type="submit" loading={mutation.isPending}>
                  {isNew ? "Create" : "Save"}
                </Button>
              </form>
            )}
          </QueryGate>
        </CardContent>
      </Card>
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

function SortableItemTree({
  nodes,
  menuId,
  canEdit,
  onDelete,
}: {
  nodes: TreeNode[];
  menuId: string;
  canEdit: boolean;
  onDelete: (id: string) => void;
}) {
  return (
    <SortableContext items={nodes.map((node) => node.item.id)} strategy={verticalListSortingStrategy}>
      <ul className="space-y-2" aria-label="Menu items">
        {nodes.map((node) => (
          <SortableMenuItem
            key={node.item.id}
            node={node}
            depth={0}
            menuId={menuId}
            canEdit={canEdit}
            onDelete={onDelete}
          />
        ))}
      </ul>
    </SortableContext>
  );
}

function SortableMenuItem({
  node,
  depth,
  menuId,
  canEdit,
  onDelete,
}: {
  node: TreeNode;
  depth: number;
  menuId: string;
  canEdit: boolean;
  onDelete: (id: string) => void;
}) {
  const item = node.item;
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: item.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    marginLeft: depth * 16,
  };
  return (
    <li>
      <div
        ref={setNodeRef}
        style={style}
        className="flex flex-wrap items-center gap-2 rounded-lg border border-border bg-card px-3 py-2"
      >
        {canEdit ? (
          <button
            type="button"
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
            aria-label={`Reorder ${item.label || item.id}`}
            {...attributes}
            {...listeners}
          >
            <GripVertical className="size-4" />
          </button>
        ) : null}
        <div className="min-w-0 flex-1">
          <Link
            to="/menus/$id/items/$itemId"
            params={{ id: menuId, itemId: item.id }}
            className="font-medium text-primary hover:underline"
          >
            {item.label || item.id}
          </Link>
          <div className="truncate text-sm text-muted-foreground">{item.url}</div>
        </div>
        {item.isDisabled ? <Badge variant="secondary">Disabled</Badge> : null}
        {item.openInNewTab ? <Badge variant="info">New tab</Badge> : null}
        {canEdit ? (
          <div className="flex flex-wrap items-center gap-1">
            <a
              href={`/raytha/menus/${encodeURIComponent(menuId)}/items/new?parent=${encodeURIComponent(item.id)}`}
              className="inline-flex h-8 items-center rounded-lg px-3 text-sm hover:bg-accent"
            >
              Add child
            </a>
            <RowActions
              actions={[
                {
                  id: "delete",
                  label: "Delete",
                  destructive: true,
                  onSelect: () => onDelete(item.id),
                },
              ]}
            />
          </div>
        ) : null}
      </div>
      {node.children.length > 0 ? (
        <div className="mt-2">
          <SortableContext items={node.children.map((child) => child.item.id)} strategy={verticalListSortingStrategy}>
            <ul className="space-y-2">
              {node.children.map((child) => (
                <SortableMenuItem
                  key={child.item.id}
                  node={child}
                  depth={depth + 1}
                  menuId={menuId}
                  canEdit={canEdit}
                  onDelete={onDelete}
                />
              ))}
            </ul>
          </SortableContext>
        </div>
      ) : null}
    </li>
  );
}
