# Raytha Pages Refactor - Architecture Diagrams

## Current State (Before Refactor)

```
┌─────────────────────────────────────────────────────────────────┐
│                         _Layout.cshtml                           │
│                      (HTML shell + scripts)                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
    ┌─────────▼──────────┐      ┌──────────▼───────────┐
    │  SidebarLayout     │      │  EmptyLayout         │
    │  (200+ lines of    │      │  (Login, Setup)      │
    │   inline nav HTML) │      └──────────────────────┘
    └─────────┬──────────┘
              │
    ┌─────────▼──────────────────────┐
    │  Feature Layouts (DUPLICATED)  │
    │  • Admins/SubActionLayout      │
    │  • ContentItems/SubActionLayout│
    │  • EmailTemplates/SubAction... │
    │  • NavigationMenus/SubAction...│
    └─────────┬──────────────────────┘
              │
    ┌─────────▼──────────┐
    │   Feature Pages    │
    │   • Inline alerts  │
    │   • No breadcrumbs │
    │   • Hardcoded nav  │
    │   • Stimulus attrs │
    └────────────────────┘

Problems:
❌ SubActionLayout duplicated 4x
❌ Inline navigation (not reusable)
❌ No breadcrumbs anywhere
❌ Stimulus controllers scattered
❌ Inconsistent patterns
```

## Target State (After Refactor)

```
┌─────────────────────────────────────────────────────────────────┐
│                         _Layout.cshtml                           │
│                   (HTML shell + Bootstrap 5)                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
    ┌─────────▼──────────┐      ┌──────────▼───────────┐
    │  SidebarLayout     │      │  EmptyLayout         │
    │  • <vc:sidebar />  │      │  (Login, Setup)      │
    │  • <breadcrumbs /> │      └──────────────────────┘
    │  • <alert />       │
    └─────────┬──────────┘
              │
    ┌─────────▼─────────────────┐
    │  Shared/SubActionLayout   │
    │  (ONE version, reusable)  │
    │  • Back navigation        │
    │  • <breadcrumbs />        │
    │  • <alert />              │
    └─────────┬─────────────────┘
              │
    ┌─────────▼──────────┐
    │   Feature Pages    │
    │   ✅ RouteNames    │
    │   ✅ Breadcrumbs   │
    │   ✅ TagHelpers    │
    │   ✅ No Stimulus   │
    └────────────────────┘

Benefits:
✅ SubActionLayout: 4 → 1 (single source of truth)
✅ Navigation: ViewComponent (testable, reusable)
✅ Breadcrumbs: Auto-generated or manual
✅ No Stimulus: Vanilla JS or Bootstrap native
✅ Consistent: Same pattern everywhere
```

---

## Navigation Architecture

### Before (Inline in Layout)

```
┌────────────────────────────────────────┐
│       SidebarLayout.cshtml             │
│  ┌──────────────────────────────────┐  │
│  │  @functions {                    │  │
│  │    IsActivePage(string item) {   │  │
│  │      // 20 lines of logic        │  │
│  │    }                              │  │
│  │  }                                │  │
│  │                                   │  │
│  │  <nav>                            │  │
│  │    <a class="@(IsActivePage..."> │  │
│  │       Dashboard                   │  │
│  │    </a>                           │  │
│  │    @if (permission check)         │  │
│  │      <a ...>Users</a>             │  │
│  │    @endif                         │  │
│  │    @foreach (contentType)         │  │
│  │      <a ...>@contentType</a>      │  │
│  │    @endforeach                    │  │
│  │    ... 150+ more lines ...        │  │
│  │  </nav>                           │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘

Problems:
❌ Not testable
❌ Not reusable
❌ Hard to maintain
❌ Duplicated permission checks
```

### After (ViewComponent + Metadata)

```
┌────────────────────────────────────────────────────────────────┐
│                         NavMap.cs                              │
│  public static IEnumerable<NavMenuItem> GetMenuItems()        │
│  {                                                             │
│    return new[]                                               │
│    {                                                           │
│      new NavMenuItem {                                        │
│        Id = "Dashboard",                                      │
│        Label = "Dashboard",                                   │
│        RouteName = RouteNames.Dashboard.Index,                │
│        Icon = IconLibrary.Dashboard,                          │
│        Permission = null                                      │
│      },                                                        │
│      // ... all menu items ...                                │
│    };                                                          │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│              SidebarViewComponent.cs                           │
│  public async Task<IViewComponentResult> InvokeAsync()        │
│  {                                                             │
│    var menuItems = NavMap.GetMenuItems();                     │
│    var filteredItems = new List<NavMenuItem>();               │
│                                                                │
│    foreach (var item in menuItems)                            │
│    {                                                           │
│      if (await IsAuthorized(item))                            │
│        filteredItems.Add(item);                               │
│    }                                                           │
│                                                                │
│    // Add dynamic content types...                            │
│                                                                │
│    return View(filteredItems);                                │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│       Components/Sidebar/Default.cshtml                        │
│  @model IEnumerable<NavMenuItem>                              │
│                                                                │
│  <nav aria-label="Main navigation">                           │
│    <ul class="nav">                                           │
│      @foreach (var item in Model)                             │
│      {                                                         │
│        <li class="nav-item">                                  │
│          <a asp-page="@item.RouteName"                        │
│             nav-active-section="@item.Id">                    │
│            @item.Label                                        │
│          </a>                                                 │
│        </li>                                                  │
│      }                                                         │
│    </ul>                                                      │
│  </nav>                                                       │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│              SidebarLayout.cshtml                              │
│  <vc:sidebar />  ← One line!                                  │
└────────────────────────────────────────────────────────────────┘

Benefits:
✅ Testable (unit test NavMap, integration test ViewComponent)
✅ Reusable (ViewComponent can be used anywhere)
✅ Maintainable (add menu item = update NavMap.cs)
✅ Centralized permissions
✅ 200 lines → 20 lines in layout
```

---

## Routing Architecture

### Before (Mixed Patterns)

```
Page A:
<a href="/users">Users</a>  ← Hardcoded

Page B:
<a asp-page="/Users/Edit">Edit</a>  ← Literal string

Page C:
<a asp-page="@RouteNames.Users.Edit">Edit</a>  ← Good! But inconsistent

PageModel:
return RedirectToPage("/Users/Index");  ← Hardcoded
```

### After (Consistent via RouteNames)

```
┌────────────────────────────────────────────────────────────────┐
│                    RouteNames.cs (Single Source of Truth)       │
│                                                                │
│  public static class Users                                     │
│  {                                                             │
│    public const string Index = "/Users/Index";                │
│    public const string Create = "/Users/Create";              │
│    public const string Edit = "/Users/Edit";                  │
│    public const string Delete = "/Users/Delete";              │
│  }                                                             │
│                                                                │
│  public static class ContentTypes { ... }                     │
│  public static class Themes { ... }                           │
│  // ... all features ...                                      │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                      All Views                                 │
│  <a asp-page="@RouteNames.Users.Index">Users</a>              │
│  <a asp-page="@RouteNames.Users.Edit" asp-route-id="@id">     │
│    Edit User                                                   │
│  </a>                                                          │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                    All PageModels                              │
│  return RedirectToPage(RouteNames.Users.Index);                │
│  return RedirectToPage(RouteNames.Users.Edit, new { id });     │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                      CI Enforcement                            │
│  scripts/check-routes.sh:                                      │
│    ✅ All routes use RouteNames constants                      │
│    ❌ No hardcoded paths found                                 │
│                                                                │
│  Unit Tests:                                                   │
│    ✅ All RouteNames constants are non-null                    │
│    ✅ All pages have corresponding RouteNames entry            │
└────────────────────────────────────────────────────────────────┘
```

---

## TagHelper Architecture

### Alert TagHelper (Example)

```
┌────────────────────────────────────────────────────────────────┐
│                      BasePageModel                             │
│  protected void SetErrorMessage(string message)                │
│  {                                                             │
│    ViewData["ErrorMessage"] = message;                        │
│    TempData["ErrorMessage"] = message;                        │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                    Any PageModel                               │
│  public async Task<IActionResult> OnPostDeleteAsync(Guid id)  │
│  {                                                             │
│    // Delete logic...                                         │
│    SetSuccessMessage("User deleted successfully");            │
│    return RedirectToPage(RouteNames.Users.Index);             │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                    AlertTagHelper                              │
│  [HtmlTargetElement("alert")]                                 │
│  public class AlertTagHelper : TagHelper                      │
│  {                                                             │
│    public override void Process(...)                          │
│    {                                                           │
│      // Read ViewData["ErrorMessage"], etc.                   │
│      // Generate Bootstrap alert HTML                         │
│      output.Content.SetHtmlContent(...);                      │
│    }                                                           │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                      Any View                                  │
│  <alert />  ← One line replaces 20+ lines of @if checks       │
│                                                                │
│  Renders as:                                                   │
│  <div class="alert alert-success alert-dismissible">          │
│    User deleted successfully                                  │
│    <button class="btn-close" ...></button>                    │
│  </div>                                                       │
└────────────────────────────────────────────────────────────────┘
```

### NavLinkTagHelper (Enhanced)

```
┌────────────────────────────────────────────────────────────────┐
│                      Any View                                  │
│  <a asp-page="@RouteNames.Users.Index"                        │
│     nav-active-section="Users">                               │
│    Users                                                       │
│  </a>                                                          │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                  NavLinkTagHelper                              │
│  public override void Process(...)                            │
│  {                                                             │
│    var currentPage = ViewContext.RouteData.Values["page"];    │
│                                                                │
│    if (IsActive(currentPage, NavSection))                     │
│    {                                                           │
│      // Add "active" class to existing classes                │
│      output.Attributes.SetAttribute("class",                  │
│        $"{existingClass} active");                            │
│    }                                                           │
│                                                                │
│    // Remove nav-active-section attribute (cleanup)           │
│    output.Attributes.RemoveAll("nav-active-section");         │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                     Rendered HTML                              │
│  <a href="/raytha/users" class="nav-link active">             │
│    Users                                                       │
│  </a>                                                          │
└────────────────────────────────────────────────────────────────┘
```

---

## Breadcrumbs Architecture

### Manual Breadcrumbs (Control)

```
┌────────────────────────────────────────────────────────────────┐
│                    Users/Edit.cshtml.cs                        │
│  public void OnGet(Guid id)                                    │
│  {                                                             │
│    ViewData["ActiveMenu"] = "Users";                          │
│    SetBreadcrumbs(                                            │
│      new BreadcrumbNode {                                     │
│        Label = "Dashboard",                                   │
│        RouteName = RouteNames.Dashboard.Index                 │
│      },                                                        │
│      new BreadcrumbNode {                                     │
│        Label = "Users",                                       │
│        RouteName = RouteNames.Users.Index                     │
│      },                                                        │
│      new BreadcrumbNode {                                     │
│        Label = "Edit User",                                   │
│        IsActive = true                                        │
│      }                                                         │
│    );                                                          │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                    Users/Edit.cshtml                           │
│  <breadcrumbs />                                              │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                BreadcrumbsTagHelper                            │
│  public override async Task ProcessAsync(...)                 │
│  {                                                             │
│    var breadcrumbs = ViewContext.ViewData["Breadcrumbs"]      │
│      as IEnumerable<BreadcrumbNode>;                          │
│                                                                │
│    if (breadcrumbs == null)                                   │
│      breadcrumbs = GenerateFromRoute(); // Auto-generate      │
│                                                                │
│    var html = RenderBreadcrumbs(breadcrumbs);                 │
│    output.Content.SetHtmlContent(html);                       │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                     Rendered HTML                              │
│  <nav aria-label="breadcrumb">                                │
│    <ol class="breadcrumb">                                    │
│      <li class="breadcrumb-item">                             │
│        <a href="/raytha/dashboard">Dashboard</a>              │
│      </li>                                                     │
│      <li class="breadcrumb-item">                             │
│        <a href="/raytha/users">Users</a>                      │
│      </li>                                                     │
│      <li class="breadcrumb-item active" aria-current="page">  │
│        Edit User                                              │
│      </li>                                                     │
│    </ol>                                                      │
│  </nav>                                                       │
└────────────────────────────────────────────────────────────────┘
```

---

## Stimulus Removal Strategy

### Before (Stimulus Controller)

```
┌────────────────────────────────────────────────────────────────┐
│              View with Stimulus                                │
│  <div data-controller="shared--confirmaction">                │
│    <button data-action="click->shared--confirmaction#confirm" │
│            data-shared--confirmaction-message-param="Delete?" │
│            data-shared--confirmaction-url-param="/delete">    │
│      Delete                                                    │
│    </button>                                                   │
│  </div>                                                       │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│         wwwroot/js/controllers/confirmaction.js                │
│  import { Controller } from 'stimulus'                        │
│                                                                │
│  export default class extends Controller {                    │
│    static values = { message: String, url: String }          │
│                                                                │
│    confirm(event) {                                           │
│      if (window.confirm(this.messageValue)) {                 │
│        window.location = this.urlValue;                       │
│      }                                                         │
│    }                                                           │
│  }                                                             │
└────────────────────────────────────────────────────────────────┘
```

### After (Bootstrap Modal)

```
┌────────────────────────────────────────────────────────────────┐
│                View with Bootstrap Modal                       │
│  <button type="button"                                        │
│          class="btn btn-danger"                               │
│          data-bs-toggle="modal"                               │
│          data-bs-target="#confirm-delete"                     │
│          data-action-url="/delete">                           │
│    Delete                                                      │
│  </button>                                                     │
│                                                                │
│  @await Html.PartialAsync("_Partials/_ConfirmDialog",        │
│    new ConfirmActionModel {                                   │
│      Id = "confirm-delete",                                   │
│      Title = "Confirm Delete",                                │
│      Message = "Are you sure?",                               │
│      ConfirmButtonText = "Delete",                            │
│      ConfirmButtonClass = "btn-danger"                        │
│    })                                                          │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│         _Partials/_ConfirmDialog.cshtml                        │
│  @model ConfirmActionModel                                    │
│                                                                │
│  <div class="modal fade" id="@Model.Id" tabindex="-1">       │
│    <div class="modal-dialog">                                 │
│      <div class="modal-content">                              │
│        <div class="modal-header">                             │
│          <h5 class="modal-title">@Model.Title</h5>           │
│          <button type="button" class="btn-close"              │
│                  data-bs-dismiss="modal"></button>            │
│        </div>                                                  │
│        <div class="modal-body">@Model.Message</div>           │
│        <div class="modal-footer">                             │
│          <button type="button" class="btn btn-secondary"      │
│                  data-bs-dismiss="modal">Cancel</button>      │
│          <form method="post" id="@(Model.Id)-form">           │
│            <button type="submit"                              │
│                    class="btn @Model.ConfirmButtonClass">     │
│              @Model.ConfirmButtonText                         │
│            </button>                                           │
│          </form>                                               │
│        </div>                                                  │
│      </div>                                                    │
│    </div>                                                      │
│  </div>                                                       │
│                                                                │
│  <script>                                                     │
│    document.getElementById('@Model.Id')                       │
│      .addEventListener('show.bs.modal', function(event) {     │
│        var button = event.relatedTarget;                      │
│        var url = button.getAttribute('data-action-url');      │
│        document.getElementById('@(Model.Id)-form')            │
│          .action = url;                                       │
│      });                                                       │
│  </script>                                                    │
└────────────────────────────────────────────────────────────────┘

Advantages:
✅ No Stimulus dependency
✅ Uses Bootstrap 5 (already included)
✅ Reusable partial
✅ Works without JavaScript (graceful degradation)
✅ Simpler, fewer abstractions
```

---

## Testing Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                    Unit Tests                                  │
│  RouteNamesTests.cs                                           │
│    ✓ All constants are non-null                               │
│    ✓ All constants match expected paths                       │
│                                                                │
│  NavMapTests.cs                                               │
│    ✓ All menu items have valid route names                    │
│    ✓ No orphaned menu items                                   │
│                                                                │
│  NavLinkTagHelperTests.cs                                     │
│    ✓ Adds active class when current page matches              │
│    ✓ Does not add class when page doesn't match               │
│    ✓ Removes nav-active-section attribute                     │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│              Integration Tests                                 │
│  SidebarViewComponentTests.cs                                 │
│    ✓ Renders all authorized menu items                        │
│    ✓ Hides unauthorized menu items                            │
│    ✓ Includes dynamic content types                           │
│                                                                │
│  BreadcrumbsTests.cs                                          │
│    ✓ Renders manual breadcrumbs correctly                     │
│    ✓ Auto-generates breadcrumbs when not set                  │
└────────────────────────────────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────┐
│                    CI Checks                                   │
│  scripts/check-routes.sh                                      │
│    ✓ No hardcoded routes in .cshtml files                     │
│                                                                │
│  scripts/check-stimulus.sh                                    │
│    ✓ No data-controller attributes                            │
│    ✓ No data-action attributes                                │
│    ✓ No data-turbo attributes                                 │
│                                                                │
│  scripts/check-route-coverage.sh                              │
│    ✓ All @page directives have RouteNames entries             │
│                                                                │
│  View Compilation Check                                       │
│    ✓ All .cshtml files compile without errors                 │
└────────────────────────────────────────────────────────────────┘
```

---

## Summary: Layers of Abstraction

```
┌─────────────────────────────────────────────────────────────────┐
│                    Feature Pages (.cshtml)                      │
│  ✓ Minimal logic                                               │
│  ✓ Use TagHelpers, ViewComponents, Partials                    │
│  ✓ Reference RouteNames constants                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│              TagHelpers & ViewComponents                        │
│  ✓ Reusable UI logic                                           │
│  ✓ Access ViewContext, services                                │
│  ✓ Generate HTML dynamically                                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                  Shared Partials                                │
│  ✓ Pure markup fragments                                       │
│  ✓ Accept models from parent                                   │
│  ✓ No data fetching                                            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                  Base PageModels                                │
│  ✓ Common functionality                                        │
│  ✓ Auth, alerts, pagination                                    │
│  ✓ Service locator pattern                                     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│              Infrastructure (RouteNames, NavMap)                │
│  ✓ Metadata and constants                                      │
│  ✓ Single source of truth                                      │
│  ✓ Testable, maintainable                                      │
└─────────────────────────────────────────────────────────────────┘

Principle: Each layer abstracts complexity from the layer above.
```

---

**End of Architecture Diagrams**

