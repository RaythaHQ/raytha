import { adminApi, formatError } from "@raytha/api";
import {
  ConfirmDialog,
  EmptyState,
  PageHeader,
  QueryGate,
  RowActions,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
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
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { GripVertical, Inbox } from "lucide-react";
import { useMemo, useState } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import {
  fieldTypeLabel,
  parseContentFields,
  parseContentTypeSummary,
  parseFieldTypeOptions,
  type ContentField,
} from "./fields-model";
import { ContentTypeNav } from "./nav";

export { NewContentTypeFieldPage, EditContentTypeFieldPage } from "./field-editor";

export function ContentTypeFieldsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Fields", developerName]);
  const queryClient = useQueryClient();
  const [deleteField, setDeleteField] = useState<ContentField | null>(null);
  const fieldsApi = adminApi.fields(developerName);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const fieldsQuery = useQuery({
    queryKey: ["content-fields", developerName],
    queryFn: () => fieldsApi.list({ pageSize: 200, orderBy: "FieldOrder asc" }),
    enabled: developerName.length > 0,
    placeholderData: keepPreviousData,
  });
  const fieldTypesQuery = useQuery({
    queryKey: ["content-field-types"],
    queryFn: () => adminApi.contentTypes.fieldTypes(),
  });

  const contentType = parseContentTypeSummary(typeQuery.data);
  const fields = useMemo(() => {
    const fromList = parseContentFields(fieldsQuery.data?.items);
    if (fromList.length > 0) {
      return fromList;
    }
    return contentType?.fields ?? [];
  }, [fieldsQuery.data, contentType]);
  const fieldTypes = parseFieldTypeOptions(fieldTypesQuery.data);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-fields", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-type", developerName] });
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => fieldsApi.remove(id),
    onSuccess: () => {
      toast.success("Field deleted");
      setDeleteField(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const reorderMutation = useMutation({
    mutationFn: ({ id, newFieldOrder }: { id: string; newFieldOrder: number }) =>
      fieldsApi.reorder(id, newFieldOrder),
    onSuccess: () => invalidate(),
    onError: (error) => toast.error(formatError(error)),
  });

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const onDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }
    const oldIndex = fields.findIndex((field) => field.id === String(active.id));
    const newIndex = fields.findIndex((field) => field.id === String(over.id));
    if (oldIndex < 0 || newIndex < 0) {
      return;
    }
    const moved = arrayMove(fields, oldIndex, newIndex)[newIndex];
    if (!moved) {
      return;
    }
    const target = fields[newIndex];
    reorderMutation.mutate({ id: moved.id, newFieldOrder: target?.fieldOrder ?? newIndex });
  };

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Fields" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${contentType?.labelPlural || developerName} fields`}
        description="Define the fields editors fill in for each item."
        actions={
          <Link
            to="/content-types/$developerName/fields/new"
            params={{ developerName }}
            className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600"
          >
            New field
          </Link>
        }
      />
      <ListBackLink to="/content-types" listKey="content-types" label="content types" />
      <ContentTypeNav developerName={developerName} />
      <QueryGate query={fieldsQuery}>
        {() =>
          fields.length === 0 ? (
            <EmptyState icon={Inbox} title="No fields" hint="Add a field to start collecting content." />
          ) : (
            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
              <SortableContext items={fields.map((field) => field.id)} strategy={verticalListSortingStrategy}>
                <Table aria-label="Fields">
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-10"><span className="sr-only">Reorder</span></TableHead>
                      <TableHead>Label</TableHead>
                      <TableHead>Developer name</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>Required</TableHead>
                      <TableHead className="w-12"><span className="sr-only">Actions</span></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {fields.map((field) => (
                      <SortableFieldRow
                        key={field.id}
                        field={field}
                        typeLabel={fieldTypeLabel(field.fieldType, fieldTypes)}
                        developerName={developerName}
                        onDelete={() => setDeleteField(field)}
                      />
                    ))}
                  </TableBody>
                </Table>
              </SortableContext>
            </DndContext>
          )
        }
      </QueryGate>
      <ConfirmDialog
        open={deleteField !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteField(null);
          }
        }}
        title="Delete field?"
        body="Existing values for this field stay in stored content but the field will no longer appear on forms."
        onConfirm={() => {
          if (deleteField) {
            deleteMutation.mutate(deleteField.id);
          }
        }}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

function SortableFieldRow({
  field,
  typeLabel,
  developerName,
  onDelete,
}: {
  field: ContentField;
  typeLabel: string;
  developerName: string;
  onDelete: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: field.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
  };
  return (
    <TableRow ref={setNodeRef} style={style}>
      <TableCell>
        <button
          type="button"
          className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          aria-label={`Reorder ${field.label || field.developerName}`}
          {...attributes}
          {...listeners}
        >
          <GripVertical className="size-4" />
        </button>
      </TableCell>
      <TableCell>
        <Link
          to="/content-types/$developerName/fields/$id"
          params={{ developerName, id: field.id }}
          className="text-primary hover:underline"
        >
          {field.label}
        </Link>
      </TableCell>
      <TableCell className="font-mono text-xs">{field.developerName}</TableCell>
      <TableCell>{typeLabel}</TableCell>
      <TableCell>{field.isRequired ? "Yes" : "No"}</TableCell>
      <TableCell>
        <RowActions
          actions={[
            {
              id: "delete",
              label: "Delete",
              destructive: true,
              onSelect: onDelete,
            },
          ]}
        />
      </TableCell>
    </TableRow>
  );
}
