export type FunctionTriggerType =
  | "http_request"
  | "liquid_template"
  | "content_item_created"
  | "content_item_updated"
  | "content_item_deleted";

export type EntryPoint = {
  signature: string;
  when: string;
  returns: string;
};

export type FunctionTrigger = {
  value: FunctionTriggerType;
  label: string;
  summary: string;
  entryPoints: EntryPoint[];
  /** The code must define at least one of these; empty when any method name works. */
  requiredFunctions: string[];
  starter: string;
};

export type ReferenceMember = {
  signature: string;
  description: string;
  example: string;
};

export type ReferenceGroup = {
  id: string;
  title: string;
  description: string;
  members: ReferenceMember[];
};

export type FunctionLimit = {
  title: string;
  detail: string;
};

export type FunctionRecipe = {
  id: string;
  title: string;
  trigger: FunctionTriggerType;
  description: string;
  code: string;
};

const HTTP_STARTER = `/**
 * HTTP request trigger.
 * GET  /raytha/functions/execute/{developerName} calls get(query)
 * POST /raytha/functions/execute/{developerName} calls post(payload, query)
 *
 * query:   array of { Key, Value } pairs, where Value is an array of strings
 * payload: the parsed JSON body, or { Key, Value } pairs for a form post
 * Return a JsonResult, HtmlResult, XmlResult, RedirectResult, or StatusCodeResult.
 */
function get(query) {
    return new JsonResult({ success: true });
    // return new HtmlResult("<p>Hello World</p>");
    // return new RedirectResult("https://raytha.com");
    // return new StatusCodeResult(404, "Not Found");
}

function post(payload, query) {
    return new JsonResult({ received: payload });
}
`;

const LIQUID_STARTER = `/**
 * Liquid template trigger.
 * Call it from any web template:
 *   {{ raytha_function("{developerName}", "greet", name="World") }}
 *
 * Named arguments (name=value) arrive as one object. Positional ones arrive
 * as arg1, arg2, ... Do not mix the two styles in one call.
 * The return value is rendered in place. Errors render nothing.
 */
function greet(args) {
    return "Hello, " + args.name + "!";
}
`;

function contentItemStarter(event: "created" | "updated" | "deleted"): string {
  return `/**
 * Content item ${event} trigger. Runs as a background task after a content item is ${event}.
 *
 * payload is the content item (PascalCase):
 *   payload.Id, payload.PrimaryField, payload.RoutePath, payload.IsPublished,
 *   payload.ContentType.DeveloperName, payload.PublishedContent, payload.DraftContent
 * The return value is ignored. Errors show under System > Background tasks.
 */
function run(payload) {
    // HttpClient.Post("https://example.com/hooks/raytha", null, { id: payload.Id, title: payload.PrimaryField });
}
`;
}

const RUN_ENTRY: EntryPoint = {
  signature: "run(payload)",
  when: "Once per content item event, in a background task.",
  returns: "Nothing. The return value is ignored; a thrown error is recorded on the background task.",
};

export const FUNCTION_TRIGGERS: readonly FunctionTrigger[] = [
  {
    value: "http_request",
    label: "HTTP request",
    summary: "A public endpoint at /raytha/functions/execute/{developerName}. Anyone can call it; check CurrentUser if it must be private.",
    entryPoints: [
      {
        signature: "get(query)",
        when: "GET request. query is [{ Key, Value: [strings] }].",
        returns: "JsonResult, HtmlResult, XmlResult, RedirectResult, or StatusCodeResult.",
      },
      {
        signature: "post(payload, query)",
        when: "POST request. payload is the parsed JSON body, or [{ Key, Value }] pairs for a form.",
        returns: "Same result helpers as get. Anything else is a 500.",
      },
    ],
    requiredFunctions: ["get", "post"],
    starter: HTTP_STARTER,
  },
  {
    value: "liquid_template",
    label: "Liquid template",
    summary: 'Called while a page renders: {{ raytha_function("dev_name", "method", key=value) }}.',
    entryPoints: [
      {
        signature: "anyMethodName(args)",
        when: "The method named in the Liquid call. args holds the named arguments (key=value), or arg1, arg2 for positional ones. Do not mix styles.",
        returns: "Any value (string, number, object, array). It is handed back to Liquid.",
      },
    ],
    requiredFunctions: [],
    starter: LIQUID_STARTER,
  },
  {
    value: "content_item_created",
    label: "Content item created",
    summary: "Runs in the background after any content item is created.",
    entryPoints: [RUN_ENTRY],
    requiredFunctions: ["run"],
    starter: contentItemStarter("created"),
  },
  {
    value: "content_item_updated",
    label: "Content item updated",
    summary: "Runs in the background after any content item is saved.",
    entryPoints: [RUN_ENTRY],
    requiredFunctions: ["run"],
    starter: contentItemStarter("updated"),
  },
  {
    value: "content_item_deleted",
    label: "Content item deleted",
    summary: "Runs in the background after any content item is moved to the trash.",
    entryPoints: [RUN_ENTRY],
    requiredFunctions: ["run"],
    starter: contentItemStarter("deleted"),
  },
];

export const DEFAULT_TRIGGER: FunctionTriggerType = "http_request";

export function triggerFor(value: string): FunctionTrigger {
  return FUNCTION_TRIGGERS.find((trigger) => trigger.value === value) ?? FUNCTION_TRIGGERS[0]!;
}

export function starterCode(trigger: string, developerName: string): string {
  return triggerFor(trigger).starter.replaceAll("{developerName}", developerName.trim() || "{developerName}");
}

/** A hint, not validation: the engine only fails when the entry point is called. */
export function missingEntryPoint(trigger: string, code: string): string | null {
  const { label, requiredFunctions } = triggerFor(trigger);
  if (requiredFunctions.length === 0 || requiredFunctions.some((name) => definesFunction(code, name))) {
    return null;
  }
  const names = requiredFunctions.map((name) => `${name}()`).join(" or ");
  return `${label} functions need a ${names} function. Nothing will run until the code defines one.`;
}

function definesFunction(code: string, name: string): boolean {
  return new RegExp(
    `\\b(?:async\\s+)?function\\s+${name}\\s*\\(|\\b(?:const|let|var)\\s+${name}\\s*=`,
  ).test(code);
}

const LIST_PARAMS = 'search = "", orderBy = "", pageNumber = 1, pageSize = 50';

const API_RESPONSE_NOTE =
  "Every call returns { Success, Error, Result }. A missing entity or failed validation throws instead, so wrap calls in try/catch.";

export const REFERENCE_GROUPS: readonly ReferenceGroup[] = [
  {
    id: "results",
    title: "Result helpers",
    description: "Return one of these from get or post. The content type and status follow the helper.",
    members: [
      {
        signature: "new JsonResult(value)",
        description: "Serializes value as application/json.",
        example: "return new JsonResult({ ok: true, items: [] });",
      },
      {
        signature: "new HtmlResult(html)",
        description: "Returns text/html.",
        example: 'return new HtmlResult("<h1>Thanks!</h1>");',
      },
      {
        signature: "new XmlResult(xml)",
        description: "Returns application/xml.",
        example: 'return new XmlResult("<root><ok>true</ok></root>");',
      },
      {
        signature: "new RedirectResult(url)",
        description: "Answers 302 with Location set to url.",
        example: 'return new RedirectResult("/thank-you");',
      },
      {
        signature: "new StatusCodeResult(statusCode, message)",
        description: "Answers with any status code and a plain-text body.",
        example: 'return new StatusCodeResult(400, "email is required");',
      },
    ],
  },
  {
    id: "content-items",
    title: "API_V1 · Content items",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetContentItems(contentTypeDeveloperName, viewId = "", search = "", filter = "", orderBy = "", pageNumber = 1, pageSize = 50)`,
        description: "Page of content items. Result.Items and Result.TotalCount; filter uses OData syntax.",
        example: `const page = API_V1.GetContentItems("posts", "", "", "IsPublished eq 'true'", "CreationTime desc", 1, 10).Result;`,
      },
      {
        signature: `API_V1.GetDeletedContentItems(contentTypeDeveloperName, ${LIST_PARAMS})`,
        description: "Page of items in the trash for a content type.",
        example: 'const trash = API_V1.GetDeletedContentItems("posts").Result;',
      },
      {
        signature: "API_V1.GetContentItemById(contentItemId)",
        description: "One content item with its PublishedContent and DraftContent.",
        example: "const item = API_V1.GetContentItemById(id).Result;",
      },
      {
        signature: "API_V1.CreateContentItem(contentTypeDeveloperName, saveAsDraft, templateId, content)",
        description: "Creates an item. content is an object keyed by field developer name. Result is the new id.",
        example: 'const id = API_V1.CreateContentItem("posts", true, templateId, { title: "Hello" }).Result;',
      },
      {
        signature: "API_V1.EditContentItem(contentItemId, saveAsDraft, content)",
        description: "Replaces an item's field values, as a draft or published.",
        example: 'API_V1.EditContentItem(id, false, { title: "Updated" });',
      },
      {
        signature: "API_V1.EditContentItemSettings(contentItemId, templateId, routePath)",
        description: "Changes an item's template and URL path.",
        example: 'API_V1.EditContentItemSettings(id, templateId, "blog/hello");',
      },
      {
        signature: "API_V1.UnpublishContentItem(contentItemId)",
        description: "Takes an item off the public site.",
        example: "API_V1.UnpublishContentItem(id);",
      },
      {
        signature: "API_V1.DeleteContentItem(contentItemId)",
        description: "Moves an item to the trash.",
        example: "API_V1.DeleteContentItem(id);",
      },
      {
        signature: "API_V1.GetRouteByPath(routePath)",
        description: "Resolves a URL path to the content item, view, or site page that owns it.",
        example: 'const route = API_V1.GetRouteByPath("about").Result;',
      },
    ],
  },
  {
    id: "content-types",
    title: "API_V1 · Content types and templates",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetContentTypes(${LIST_PARAMS})`,
        description: "Page of content types with their fields.",
        example: "const types = API_V1.GetContentTypes().Result.Items;",
      },
      {
        signature: "API_V1.GetContentTypeByDeveloperName(contentTypeDeveloperName)",
        description: "One content type, including ContentTypeFields.",
        example: 'const posts = API_V1.GetContentTypeByDeveloperName("posts").Result;',
      },
      {
        signature: `API_V1.GetWebTemplates(${LIST_PARAMS})`,
        description: "Page of web templates in the active theme.",
        example: "const templates = API_V1.GetWebTemplates().Result.Items;",
      },
      {
        signature: "API_V1.GetWebTemplateById(webTemplateId)",
        description: "One web template with its Liquid content.",
        example: "const template = API_V1.GetWebTemplateById(templateId).Result;",
      },
    ],
  },
  {
    id: "site-pages",
    title: "API_V1 · Site pages",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetSitePages(${LIST_PARAMS})`,
        description: "Page of site pages.",
        example: "const pages = API_V1.GetSitePages().Result.Items;",
      },
      {
        signature: "API_V1.GetSitePageById(sitePageId)",
        description: "One site page with its widgets and template sections.",
        example: "const page = API_V1.GetSitePageById(pageId).Result;",
      },
      {
        signature: "API_V1.CreateSitePage(title, saveAsDraft, templateId)",
        description: "Creates a site page. Result is the new id.",
        example: 'const id = API_V1.CreateSitePage("Landing", true, templateId).Result;',
      },
      {
        signature: "API_V1.EditSitePage(sitePageId, title, saveAsDraft, templateId)",
        description: "Renames a page or changes its template.",
        example: 'API_V1.EditSitePage(pageId, "New title", true, templateId);',
      },
      {
        signature: "API_V1.EditSitePageSettings(sitePageId, routePath)",
        description: "Changes a page's URL path.",
        example: 'API_V1.EditSitePageSettings(pageId, "landing");',
      },
      {
        signature: "API_V1.PublishSitePage(sitePageId)",
        description: "Publishes the draft layout.",
        example: "API_V1.PublishSitePage(pageId);",
      },
      {
        signature: "API_V1.UnpublishSitePage(sitePageId)",
        description: "Takes a page off the public site.",
        example: "API_V1.UnpublishSitePage(pageId);",
      },
      {
        signature: "API_V1.DeleteSitePage(sitePageId)",
        description: "Deletes a site page.",
        example: "API_V1.DeleteSitePage(pageId);",
      },
    ],
  },
  {
    id: "menus",
    title: "API_V1 · Menus",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetNavigationMenus(${LIST_PARAMS})`,
        description: "Page of navigation menus.",
        example: "const menus = API_V1.GetNavigationMenus().Result.Items;",
      },
      {
        signature: "API_V1.GetNavigationMenuById(navigationMenuId)",
        description: "One menu by id.",
        example: "const menu = API_V1.GetNavigationMenuById(menuId).Result;",
      },
      {
        signature: "API_V1.GetNavigationMenuByDeveloperName(developerName)",
        description: "One menu by developer name.",
        example: 'const main = API_V1.GetNavigationMenuByDeveloperName("main_menu").Result;',
      },
      {
        signature: "API_V1.CreateNavigationMenu(label, developerName)",
        description: "Creates a menu. Result is the new id.",
        example: 'const id = API_V1.CreateNavigationMenu("Footer", "footer").Result;',
      },
      {
        signature: "API_V1.EditNavigationMenu(navigationMenuId, label)",
        description: "Renames a menu.",
        example: 'API_V1.EditNavigationMenu(menuId, "Footer links");',
      },
      {
        signature: "API_V1.DeleteNavigationMenu(navigationMenuId)",
        description: "Deletes a menu and its items.",
        example: "API_V1.DeleteNavigationMenu(menuId);",
      },
      {
        signature: "API_V1.GetNavigationMenuItemsByNavigationMenuId(navigationMenuId)",
        description: "Every item in a menu, flat, with ParentNavigationMenuItemId and Ordinal.",
        example: "const items = API_V1.GetNavigationMenuItemsByNavigationMenuId(menuId).Result;",
      },
      {
        signature: "API_V1.GetNavigationMenuItemById(navigationMenuItemId)",
        description: "One menu item.",
        example: "const item = API_V1.GetNavigationMenuItemById(itemId).Result;",
      },
      {
        signature:
          "API_V1.CreateNavigationMenuItem(navigationMenuId, label, url, isDisabled, openInNewTab, cssClassName, parentNavigationMenuItemId)",
        description: "Adds a link. Pass null as the parent for a top-level item.",
        example: 'API_V1.CreateNavigationMenuItem(menuId, "Blog", "/blog", false, false, "", null);',
      },
      {
        signature:
          "API_V1.EditNavigationMenuItem(navigationMenuItemId, navigationMenuId, label, url, isDisabled, openInNewTab, cssClassName, parentNavigationMenuItemId)",
        description: "Replaces a menu item's fields.",
        example: 'API_V1.EditNavigationMenuItem(itemId, menuId, "Blog", "/blog", false, true, "", null);',
      },
      {
        signature: "API_V1.DeleteNavigationMenuItem(navigationMenuItemId, navigationMenuId)",
        description: "Removes a menu item.",
        example: "API_V1.DeleteNavigationMenuItem(itemId, menuId);",
      },
    ],
  },
  {
    id: "users",
    title: "API_V1 · Users and groups",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetUsers(${LIST_PARAMS})`,
        description: "Page of public-site users.",
        example: 'const users = API_V1.GetUsers("ada").Result.Items;',
      },
      {
        signature: "API_V1.GetUserById(userId)",
        description: "One user.",
        example: "const user = API_V1.GetUserById(userId).Result;",
      },
      {
        signature: "API_V1.CreateUser(emailAddress, firstName, lastName, sendEmail, userGroups)",
        description: "Creates a user. userGroups is an array of user group ids.",
        example: 'const id = API_V1.CreateUser("ada@example.com", "Ada", "Lovelace", true, [groupId]).Result;',
      },
      {
        signature: "API_V1.EditUser(userId, emailAddress, firstName, lastName, userGroups)",
        description: "Replaces a user's profile and group membership.",
        example: 'API_V1.EditUser(userId, "ada@example.com", "Ada", "King", []);',
      },
      {
        signature: "API_V1.DeleteUser(userId)",
        description: "Deletes a user.",
        example: "API_V1.DeleteUser(userId);",
      },
      {
        signature: "API_V1.ResetPassword(userId, sendEmail, newPassword)",
        description: "Sets a new password, optionally emailing the user.",
        example: "API_V1.ResetPassword(userId, true, newPassword);",
      },
      {
        signature: "API_V1.SetIsActive(userId, isActive)",
        description: "Suspends or restores a user.",
        example: "API_V1.SetIsActive(userId, false);",
      },
      {
        signature: `API_V1.GetUserGroups(${LIST_PARAMS})`,
        description: "Page of user groups.",
        example: "const groups = API_V1.GetUserGroups().Result.Items;",
      },
      {
        signature: "API_V1.GetUserGroupById(userGroupId)",
        description: "One user group.",
        example: "const group = API_V1.GetUserGroupById(groupId).Result;",
      },
      {
        signature: "API_V1.CreateUserGroup(developerName, label)",
        description: "Creates a group. Result is the new id.",
        example: 'const id = API_V1.CreateUserGroup("members", "Members").Result;',
      },
      {
        signature: "API_V1.EditUserGroup(userGroupId, label)",
        description: "Renames a group.",
        example: 'API_V1.EditUserGroup(groupId, "Premium members");',
      },
      {
        signature: "API_V1.DeleteUserGroup(userGroupId)",
        description: "Deletes a group.",
        example: "API_V1.DeleteUserGroup(groupId);",
      },
    ],
  },
  {
    id: "media",
    title: "API_V1 · Media",
    description: API_RESPONSE_NOTE,
    members: [
      {
        signature: `API_V1.GetMediaItems(${LIST_PARAMS})`,
        description: "Page of uploaded files.",
        example: "const files = API_V1.GetMediaItems().Result.Items;",
      },
      {
        signature: "API_V1.GetMediaItemUrlByObjectKey(objectKey)",
        description: "Public URL for an uploaded file.",
        example: "const url = API_V1.GetMediaItemUrlByObjectKey(objectKey).Result;",
      },
    ],
  },
  {
    id: "context",
    title: "CurrentOrganization and CurrentUser",
    description: "Read-only context for the site and for whoever triggered the function.",
    members: [
      {
        signature: "CurrentOrganization.OrganizationName · WebsiteUrl · TimeZone · DateFormat",
        description: "Site identity and formatting settings.",
        example: "const site = CurrentOrganization.WebsiteUrl;",
      },
      {
        signature: "CurrentOrganization.SmtpDefaultFromAddress · SmtpDefaultFromName",
        description: "The default sender, handy for Emailer.",
        example: "const from = CurrentOrganization.SmtpDefaultFromAddress;",
      },
      {
        signature: "CurrentOrganization.HomePageId · HomePageType · ActiveThemeId · ContentTypes",
        description: "Home page, active theme, and every content type.",
        example: "const themeId = CurrentOrganization.ActiveThemeId.ToString();",
      },
      {
        signature: "CurrentUser.IsAuthenticated · IsAdmin · UserId",
        description: "Who is calling. Anonymous HTTP callers are not authenticated.",
        example: 'if (!CurrentUser.IsAuthenticated) return new StatusCodeResult(401, "Sign in first");',
      },
      {
        signature: "CurrentUser.FirstName · LastName · FullName · EmailAddress",
        description: "Profile of the signed-in caller.",
        example: 'return new JsonResult({ hello: CurrentUser.FullName });',
      },
      {
        signature: "CurrentUser.Roles · UserGroups · RemoteIpAddress · AuthenticationScheme",
        description: "Roles and group developer names, plus request details.",
        example: 'const isMember = Array.from(CurrentUser.UserGroups).includes("members");',
      },
    ],
  },
  {
    id: "email",
    title: "Emailer and EmailMessage",
    description: "Sends through the site's SMTP settings and records the message in the email log.",
    members: [
      {
        signature: "EmailMessage.From(subject, htmlContent, to, fromEmailAddress, fromName)",
        description: "Builds an HTML email to one recipient.",
        example:
          'const msg = EmailMessage.From("Welcome", "<p>Hi!</p>", "ada@example.com", CurrentOrganization.SmtpDefaultFromAddress, CurrentOrganization.SmtpDefaultFromName);',
      },
      {
        signature: "Emailer.SendEmail(message)",
        description: "Sends an EmailMessage now.",
        example: "Emailer.SendEmail(msg);",
      },
    ],
  },
  {
    id: "http",
    title: "HttpClient",
    description:
      "Server-side HTTP. Each call returns the response body as a string (JSON.parse it) and throws on a non-2xx status.",
    members: [
      {
        signature: "HttpClient.Get(url, headers = null)",
        description: "GET request.",
        example: 'const data = JSON.parse(HttpClient.Get("https://api.example.com/items"));',
      },
      {
        signature: "HttpClient.Post(url, headers = null, body = null, json = true)",
        description: "POST. body is sent as JSON, or as a form when json is false.",
        example: 'HttpClient.Post("https://hooks.example.com", { Authorization: "Bearer " + token }, { event: "hello" });',
      },
      {
        signature: "HttpClient.Put(url, headers = null, body = null, json = true)",
        description: "PUT, same body rules as Post.",
        example: 'HttpClient.Put("https://api.example.com/items/1", null, { name: "Updated" });',
      },
      {
        signature: "HttpClient.Delete(url, headers = null)",
        description: "DELETE request.",
        example: 'HttpClient.Delete("https://api.example.com/items/1");',
      },
    ],
  },
  {
    id: "dotnet",
    title: ".NET helpers",
    description: "Host types available as globals. They behave like .NET, not like the JavaScript built-ins.",
    members: [
      {
        signature: "ShortGuid.NewGuid().ToString()",
        description: "A new 22-character id, the format Raytha uses in URLs. Use ToString(), not toString().",
        example: "const id = ShortGuid.NewGuid().ToString();",
      },
      {
        signature: "Guid.NewGuid().ToString()",
        description: "A new standard GUID string.",
        example: "const guid = Guid.NewGuid().ToString();",
      },
      {
        signature: "DateTime.UtcNow · DateTimeOffset · TimeSpan",
        description: "Dates and durations. JavaScript Date also works.",
        example: 'const stamp = DateTime.UtcNow.ToString("o");',
      },
      {
        signature: "Math.Round(value, digits) · Math.Max(a, b)",
        description: "Math is System.Math here, so Math.max and Math.random do not exist.",
        example: "const total = Math.Round(price * 1.08, 2);",
      },
      {
        signature: "new Random().Next(max) · Convert · Regex · StringBuilder · Encoding · Uri",
        description: "Other .NET types, plus List, Dictionary, HashSet, Queue, Stack, and Enumerable.",
        example: "const roll = new Random().Next(6) + 1;",
      },
    ],
  },
];

export const FUNCTION_LIMITS: readonly FunctionLimit[] = [
  {
    title: "Timeout",
    detail:
      "RAYTHA_FUNCTIONS_TIMEOUT, default 10000 ms. A function still running then is stopped, busy loops included, and the caller gets a timeout error. A function also stops when its HTTP caller disconnects.",
  },
  {
    title: "Concurrency",
    detail:
      "RAYTHA_FUNCTIONS_MAX_ACTIVE, default 5; 0 turns functions off. A call waits up to RAYTHA_FUNCTIONS_QUEUE_TIMEOUT (default 10000 ms), then gets 503.",
  },
  {
    title: "Sandbox",
    detail:
      "Plain JavaScript on V8. No require, import, fetch, or setTimeout. console.log runs but its output is discarded. async functions and await work.",
  },
  {
    title: "Calling other functions",
    detail:
      "A function can call another through API_V1.ExecuteRaythaFunction(name, method, queryJson, payloadJson) or HttpClient to this site. Read the API_V1 result with JSON.parse(response.Result.body.GetRawText()). Each nested call takes its own concurrency slot, and stops when its caller stops, so a runaway recursion ends at the caller's timeout.",
  },
  {
    title: "Errors",
    detail:
      "An uncaught error answers HTTP callers with 500 and the error message. Liquid calls render nothing. Content item triggers record it on the background task.",
  },
];

export const FUNCTION_RECIPES: readonly FunctionRecipe[] = [
  {
    id: "json-api",
    title: "JSON API of published posts",
    trigger: "http_request",
    description: "GET /raytha/functions/execute/{developerName}?page=2 returns a page of posts as JSON.",
    code: `function param(query, key, fallback) {
    const pair = query.find(p => p.Key === key);
    return pair ? pair.Value[0] : fallback;
}

function get(query) {
    const page = parseInt(param(query, "page", "1"), 10);
    const result = API_V1.GetContentItems("posts", "", "", "IsPublished eq 'true'", "CreationTime desc", page, 10).Result;
    return new JsonResult({
        page,
        total: result.TotalCount,
        items: Array.from(result.Items).map(item => ({
            title: item.PrimaryField,
            url: "/" + item.RoutePath,
        })),
    });
}
`,
  },
  {
    id: "contact-form",
    title: "Contact form that emails you",
    trigger: "http_request",
    description: "POST a JSON body { name, email, message }; the site emails its default sender and answers with JSON.",
    code: `function post(payload, query) {
    if (!payload || !payload.email || !payload.message) {
        return new StatusCodeResult(400, "email and message are required");
    }
    const html = "<p><strong>" + payload.name + "</strong> (" + payload.email + ") wrote:</p><p>" + payload.message + "</p>";
    Emailer.SendEmail(EmailMessage.From(
        "New contact form message",
        html,
        CurrentOrganization.SmtpDefaultFromAddress,
        CurrentOrganization.SmtpDefaultFromAddress,
        CurrentOrganization.OrganizationName
    ));
    return new JsonResult({ sent: true });
}
`,
  },
  {
    id: "notify-on-publish",
    title: "Email the team when a post is created",
    trigger: "content_item_created",
    description: "Runs after every new content item; filters to one content type.",
    code: `function run(payload) {
    if (payload.ContentType.DeveloperName !== "posts") {
        return;
    }
    Emailer.SendEmail(EmailMessage.From(
        "New post: " + payload.PrimaryField,
        "<p>A new post was created at " + CurrentOrganization.WebsiteUrl + "/" + payload.RoutePath + "</p>",
        "team@example.com",
        CurrentOrganization.SmtpDefaultFromAddress,
        CurrentOrganization.SmtpDefaultFromName
    ));
}
`,
  },
  {
    id: "external-api",
    title: "Proxy an external API",
    trigger: "http_request",
    description: "Calls a third-party API server-side so the key never reaches the browser.",
    code: `function get(query) {
    try {
        const body = HttpClient.Get("https://api.example.com/weather?city=Chicago", {
            "X-Api-Key": "replace-with-your-key",
        });
        const weather = JSON.parse(body);
        return new JsonResult({ temperature: weather.temperature });
    } catch (e) {
        return new StatusCodeResult(502, "Weather service unavailable");
    }
}
`,
  },
  {
    id: "liquid-helper",
    title: "Liquid helper for formatted prices",
    trigger: "liquid_template",
    description: 'Use in a template as {{ raytha_function("{developerName}", "price", amount=item.PublishedContent.price) }}.',
    code: `function price(args) {
    const amount = Number(args.amount || 0);
    return "$" + Math.Round(amount, 2).toFixed(2);
}
`,
  },
];
