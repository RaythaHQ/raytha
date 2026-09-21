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
export {
  UsersPage,
  UserGroupsPage,
  NewUserPage,
  EditUserPage,
  NewUserGroupPage,
  EditUserGroupPage,
} from "./users";
export {
  AdminsPage,
  RolesPage,
  NewAdminPage,
  EditAdminPage,
  NewRolePage,
  EditRolePage,
} from "./admins";
export {
  SitePagesPage,
  SitePageDetailPage,
  SitePageLayoutPage,
  NewSitePagePage,
  NewSitePageWidgetPage,
  EditSitePageWidgetPage,
} from "./site-pages";
export { ContentTypesPage, NewContentTypePage, ContentItemsPage } from "./content";
export { ContentTypeFieldsPage, NewContentTypeFieldPage, EditContentTypeFieldPage } from "./content/fields";
export { ContentTypeConfigurationPage } from "./content/configuration";
export { ContentTypeTrashPage } from "./content/trash";
export { NewContentItemPage, EditContentItemPage } from "./content/editor";
export { ContentViewsPage, NewContentViewPage } from "./content/views";
export { ContentViewEditorPage } from "./content/view-editor";
export { EmailTemplatesPage, MenusPage, FunctionsPage, NewMenuPage, NewFunctionPage } from "./templates";
export { ThemesPage, NewThemePage } from "./themes";
export { MediaPage } from "./media";
export {
  AuditLogPage,
  WebhooksPage,
  EmailLogPage,
  BackgroundTasksPage,
  NewWebhookPage,
  EditWebhookPage,
} from "./system";
export { MaintenancePage, ConfigurationPage, SmtpPage, ProfilePage } from "./settings";
export { AuthenticationPage, NewAuthenticationPage, EditAuthenticationPage } from "./authentication";

export function WebTemplatesPage() {
  return <ComingSoonPage title="Web templates" />;
}

export function WidgetTemplatesPage() {
  return <ComingSoonPage title="Widget templates" />;
}
