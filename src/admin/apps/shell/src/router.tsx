import { isAuthenticated } from "@raytha/api";
import {
  createBrowserHistory,
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  redirect,
} from "@tanstack/react-router";
import { AppShell } from "./components/app-shell";
import { LoginPage } from "./pages/login";
import {
  AdminsPage,
  AuditLogPage,
  AuthenticationPage,
  BackgroundTasksPage,
  ConfigurationPage,
  ContentItemsPage,
  ContentTypeConfigurationPage,
  ContentTypeFieldsPage,
  ContentTypeTrashPage,
  ContentTypesPage,
  ContentViewEditorPage,
  ContentViewsPage,
  DashboardPage,
  EditAdminPage,
  EditAuthenticationPage,
  EditContentItemPage,
  EditContentTypeFieldPage,
  EditRolePage,
  EditSitePageWidgetPage,
  EditUserGroupPage,
  EditUserPage,
  EditWebhookPage,
  EmailLogPage,
  EmailTemplatesPage,
  FunctionsPage,
  MaintenancePage,
  MediaPage,
  MenusPage,
  NewAdminPage,
  NewAuthenticationPage,
  NewContentItemPage,
  NewContentTypeFieldPage,
  NewContentTypePage,
  NewContentViewPage,
  NewFunctionPage,
  NewMenuPage,
  NewRolePage,
  NewSitePagePage,
  NewSitePageWidgetPage,
  NewThemePage,
  NewUserGroupPage,
  NewUserPage,
  NewWebhookPage,
  ProfilePage,
  RolesPage,
  SitePageDetailPage,
  SitePageLayoutPage,
  SitePagesPage,
  SmtpPage,
  ThemesPage,
  UserGroupsPage,
  UsersPage,
  WebhooksPage,
} from "./pages/placeholder";
import { EmailTemplateEditorPage } from "./pages/editors/email-template-editor";
import { FunctionEditorPage } from "./pages/editors/function-editor";
import { NewWebTemplatePage, WebTemplateEditorPage, WebTemplatesListPage } from "./pages/editors/web-templates";
import { WidgetTemplateEditorPage, WidgetTemplatesListPage } from "./pages/editors/widget-templates";
import { EditMenuItemPage, MenuItemsPage, NewMenuItemPage } from "./pages/menus/menu-items";
import { SetupPage } from "./pages/setup";

const rootRoute = createRootRoute({ component: Outlet });

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  beforeLoad: () => {
    if (isAuthenticated()) {
      throw redirect({ to: "/", replace: true });
    }
  },
  component: LoginPage,
});

const setupRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/setup",
  beforeLoad: () => {
    if (isAuthenticated()) {
      throw redirect({ to: "/", replace: true });
    }
  },
  component: SetupPage,
});

const appRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "app",
  beforeLoad: () => {
    if (!isAuthenticated()) {
      throw redirect({ to: "/login", replace: true });
    }
  },
  component: AppShell,
});

const dashboardRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/",
  component: DashboardPage,
});

const usersRoute = createRoute({ getParentRoute: () => appRoute, path: "/users", component: UsersPage });
const newUserRoute = createRoute({ getParentRoute: () => appRoute, path: "/users/new", component: NewUserPage });
const editUserRoute = createRoute({ getParentRoute: () => appRoute, path: "/users/$id", component: EditUserPage });
const userGroupsRoute = createRoute({ getParentRoute: () => appRoute, path: "/users/groups", component: UserGroupsPage });
const newUserGroupRoute = createRoute({ getParentRoute: () => appRoute, path: "/users/groups/new", component: NewUserGroupPage });
const editUserGroupRoute = createRoute({ getParentRoute: () => appRoute, path: "/users/groups/$id", component: EditUserGroupPage });
const sitePagesRoute = createRoute({ getParentRoute: () => appRoute, path: "/site-pages", component: SitePagesPage });
const newSitePageRoute = createRoute({ getParentRoute: () => appRoute, path: "/site-pages/new", component: NewSitePagePage });
const sitePageDetailRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/site-pages/$id",
  component: SitePageDetailPage,
});
const sitePageLayoutRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/site-pages/$id/layout",
  component: SitePageLayoutPage,
});
const newSitePageWidgetRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/site-pages/$id/layout/widgets/new",
  component: NewSitePageWidgetPage,
});
const editSitePageWidgetRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/site-pages/$id/layout/widgets/$widgetId",
  component: EditSitePageWidgetPage,
});
const contentTypesRoute = createRoute({ getParentRoute: () => appRoute, path: "/content-types", component: ContentTypesPage });
const newContentTypeRoute = createRoute({ getParentRoute: () => appRoute, path: "/content-types/new", component: NewContentTypePage });
const contentItemsRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName",
  component: ContentItemsPage,
});
const newContentItemRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName/new",
  component: NewContentItemPage,
});
const editContentItemRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName/items/$id",
  component: EditContentItemPage,
});
const contentViewsRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName/views",
  component: ContentViewsPage,
});
const newContentViewRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName/views/new",
  component: NewContentViewPage,
});
const contentViewEditorRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content/$developerName/views/$viewId",
  component: ContentViewEditorPage,
});
const contentTypeFieldsRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content-types/$developerName/fields",
  component: ContentTypeFieldsPage,
});
const newContentTypeFieldRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content-types/$developerName/fields/new",
  component: NewContentTypeFieldPage,
});
const editContentTypeFieldRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content-types/$developerName/fields/$id",
  component: EditContentTypeFieldPage,
});
const contentTypeConfigurationRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content-types/$developerName/configuration",
  component: ContentTypeConfigurationPage,
});
const contentTypeTrashRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/content-types/$developerName/trash",
  component: ContentTypeTrashPage,
});
const themesRoute = createRoute({ getParentRoute: () => appRoute, path: "/themes", component: ThemesPage });
const newThemeRoute = createRoute({ getParentRoute: () => appRoute, path: "/themes/new", component: NewThemePage });
const webTemplatesRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/themes/$themeId/web-templates",
  component: WebTemplatesListPage,
});
const newWebTemplateRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/themes/$themeId/web-templates/new",
  component: NewWebTemplatePage,
});
const webTemplateEditorRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/themes/$themeId/web-templates/$id",
  component: WebTemplateEditorPage,
});
const widgetTemplatesRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/themes/$themeId/widget-templates",
  component: WidgetTemplatesListPage,
});
const widgetTemplateEditorRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/themes/$themeId/widget-templates/$id",
  component: WidgetTemplateEditorPage,
});
const emailTemplatesRoute = createRoute({ getParentRoute: () => appRoute, path: "/email-templates", component: EmailTemplatesPage });
const emailTemplateEditorRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/email-templates/$id",
  component: EmailTemplateEditorPage,
});
const menusRoute = createRoute({ getParentRoute: () => appRoute, path: "/menus", component: MenusPage });
const newMenuRoute = createRoute({ getParentRoute: () => appRoute, path: "/menus/new", component: NewMenuPage });
const menuItemsRoute = createRoute({ getParentRoute: () => appRoute, path: "/menus/$id", component: MenuItemsPage });
const newMenuItemRoute = createRoute({ getParentRoute: () => appRoute, path: "/menus/$id/items/new", component: NewMenuItemPage });
const editMenuItemRoute = createRoute({ getParentRoute: () => appRoute, path: "/menus/$id/items/$itemId", component: EditMenuItemPage });
const functionsRoute = createRoute({ getParentRoute: () => appRoute, path: "/functions", component: FunctionsPage });
const newFunctionRoute = createRoute({ getParentRoute: () => appRoute, path: "/functions/new", component: NewFunctionPage });
const functionEditorRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/functions/$id",
  component: FunctionEditorPage,
});
const auditLogRoute = createRoute({ getParentRoute: () => appRoute, path: "/audit-log", component: AuditLogPage });
const webhooksRoute = createRoute({ getParentRoute: () => appRoute, path: "/webhooks", component: WebhooksPage });
const newWebhookRoute = createRoute({ getParentRoute: () => appRoute, path: "/webhooks/new", component: NewWebhookPage });
const editWebhookRoute = createRoute({ getParentRoute: () => appRoute, path: "/webhooks/$id", component: EditWebhookPage });
const emailLogRoute = createRoute({ getParentRoute: () => appRoute, path: "/email-log", component: EmailLogPage });
const backgroundTasksRoute = createRoute({ getParentRoute: () => appRoute, path: "/background-tasks", component: BackgroundTasksPage });
const mediaRoute = createRoute({ getParentRoute: () => appRoute, path: "/media", component: MediaPage });
const maintenanceRoute = createRoute({ getParentRoute: () => appRoute, path: "/maintenance", component: MaintenancePage });
const profileRoute = createRoute({ getParentRoute: () => appRoute, path: "/profile", component: ProfilePage });

const settingsAdminsRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/admins", component: AdminsPage });
const newAdminRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/admins/new", component: NewAdminPage });
const editAdminRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/admins/$id", component: EditAdminPage });
const settingsConfigurationRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/configuration", component: ConfigurationPage });
const settingsAuthenticationRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/authentication", component: AuthenticationPage });
const newAuthenticationRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/settings/authentication/new/$schemeType",
  component: NewAuthenticationPage,
});
const editAuthenticationRoute = createRoute({
  getParentRoute: () => appRoute,
  path: "/settings/authentication/$id",
  component: EditAuthenticationPage,
});
const settingsSmtpRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/smtp", component: SmtpPage });
const settingsRolesRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/roles", component: RolesPage });
const newRoleRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/roles/new", component: NewRolePage });
const editRoleRoute = createRoute({ getParentRoute: () => appRoute, path: "/settings/roles/$id", component: EditRolePage });

const routeTree = rootRoute.addChildren([
  loginRoute,
  setupRoute,
  appRoute.addChildren([
    dashboardRoute,
    usersRoute,
    newUserRoute,
    editUserRoute,
    userGroupsRoute,
    newUserGroupRoute,
    editUserGroupRoute,
    sitePagesRoute,
    newSitePageRoute,
    sitePageDetailRoute,
    sitePageLayoutRoute,
    newSitePageWidgetRoute,
    editSitePageWidgetRoute,
    contentTypesRoute,
    newContentTypeRoute,
    contentTypeFieldsRoute,
    newContentTypeFieldRoute,
    editContentTypeFieldRoute,
    contentTypeConfigurationRoute,
    contentTypeTrashRoute,
    contentItemsRoute,
    newContentItemRoute,
    editContentItemRoute,
    contentViewsRoute,
    newContentViewRoute,
    contentViewEditorRoute,
    themesRoute,
    newThemeRoute,
    webTemplatesRoute,
    newWebTemplateRoute,
    webTemplateEditorRoute,
    widgetTemplatesRoute,
    widgetTemplateEditorRoute,
    emailTemplatesRoute,
    emailTemplateEditorRoute,
    menusRoute,
    newMenuRoute,
    menuItemsRoute,
    newMenuItemRoute,
    editMenuItemRoute,
    functionsRoute,
    newFunctionRoute,
    functionEditorRoute,
    auditLogRoute,
    webhooksRoute,
    newWebhookRoute,
    editWebhookRoute,
    emailLogRoute,
    backgroundTasksRoute,
    mediaRoute,
    maintenanceRoute,
    profileRoute,
    settingsAdminsRoute,
    newAdminRoute,
    editAdminRoute,
    settingsConfigurationRoute,
    settingsAuthenticationRoute,
    newAuthenticationRoute,
    editAuthenticationRoute,
    settingsSmtpRoute,
    settingsRolesRoute,
    newRoleRoute,
    editRoleRoute,
  ]),
]);

/** TanStack joins `basepath` + `/` as `/raytha/`. Keep the dashboard URL slash-free. */
function dropAdminIndexSlash(path: string): string {
  if (path === "/raytha/" || path.startsWith("/raytha/?") || path.startsWith("/raytha/#")) {
    return `/raytha${path.slice("/raytha/".length)}`;
  }
  return path;
}

const history = createBrowserHistory();
const push = history.push.bind(history);
const replace = history.replace.bind(history);
const createHref = history.createHref.bind(history);
history.push = (path, state, opts) => push(dropAdminIndexSlash(path), state, opts);
history.replace = (path, state, opts) => replace(dropAdminIndexSlash(path), state, opts);
history.createHref = (path) => dropAdminIndexSlash(createHref(path));

export const router = createRouter({
  routeTree,
  history,
  basepath: "/raytha",
  trailingSlash: "never",
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
