import { EmptyState, PageHeader } from "@raytha/ui";
import { Construction } from "lucide-react";

export function ComingSoonPage({ title, description }: { title: string; description?: string }) {
  return (
    <div className="space-y-6">
      <PageHeader title={title} description={description} />
      <EmptyState icon={Construction} title="Coming soon" hint="This page is scaffolded for Raytha 2.0 and is not wired up yet." />
    </div>
  );
}

export { DashboardPage } from "./dashboard";
export { UsersPage, UserGroupsPage } from "./users";
export { AdminsPage, RolesPage } from "./admins";
export { SitePagesPage, SitePageDetailPage, SitePageLayoutPage } from "./site-pages";
export { ContentTypesPage, NewContentTypePage, ContentItemsPage } from "./content";
export { ContentTypeFieldsPage } from "./content/fields";
export { ContentTypeConfigurationPage } from "./content/configuration";
export { ContentTypeTrashPage } from "./content/trash";
export { NewContentItemPage, EditContentItemPage } from "./content/editor";
export { ContentViewsPage } from "./content/views";
export { ContentViewEditorPage } from "./content/view-editor";
export { EmailTemplatesPage, MenusPage, FunctionsPage } from "./templates";
export { ThemesPage } from "./themes";
export { MediaPage } from "./media";
export { AuditLogPage, WebhooksPage, EmailLogPage, FeatureFlagsPage, BackgroundTasksPage } from "./system";
export { MaintenancePage, ConfigurationPage, SmtpPage, AuthenticationPage, ProfilePage } from "./settings";

export function WebTemplatesPage() {
  return <ComingSoonPage title="Web templates" />;
}

export function WidgetTemplatesPage() {
  return <ComingSoonPage title="Widget templates" />;
}
