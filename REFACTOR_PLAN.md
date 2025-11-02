# Raytha Pages/ Refactor Plan
## Comprehensive Refactor for Maintainability, Reuse, and Clean Navigation

**Version:** 1.0  
**Date:** November 2, 2025  
**Scope:** Entire `/src/Raytha.Web/Areas/Admin/Pages/` folder  
**Context:** .NET 8 Razor Pages, Clean Architecture, No Hotwire/Turbo/Stimulus

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Global Inventory & Duplication Map](#1-global-inventory--duplication-map)
3. [Target Structure & Conventions](#2-target-structure--conventions)
4. [Layouts & Templates](#3-layouts--templates)
5. [Partials vs ViewComponents vs TagHelpers](#4-partials-vs-viewcomponents-vs-taghelpers)
6. [Shared Models & Base PageModels](#5-shared-models--base-pagemodels)
7. [Routing, Route Names, and DRY](#6-routing-route-names-and-dry)
8. [Navigation with Active State & Breadcrumbs](#7-navigation-with-active-state--breadcrumbs)
9. [Purge Hotwire/Stimulus Globally](#8-purge-hotwirestimulus-globally)
10. [Validation, Alerts, Empty States](#9-validation-alerts-empty-states)
11. [Security, Performance, Accessibility](#10-security-performance-accessibility)
12. [Testing, Linting & Tooling](#11-testing-linting--tooling)
13. [Migration Plan (PR Batches)](#12-migration-plan-pr-batches)
14. [Acceptance Criteria](#13-acceptance-criteria)
15. [Coding Standards](#14-coding-standards)

---

## Executive Summary

This plan refactors the entire `Pages/` folder to eliminate duplication, establish consistent patterns, remove all Hotwire/Turbo/Stimulus remnants, and create a scalable navigation system. The successful approach used in `Pages/ContentTypes` serves as the foundation to scale across all features.

**Key Goals:**
- ✅ Reduce markup duplication by ≥70%
- ✅ Zero hardcoded routes (all via RouteNames + TagHelpers)
- ✅ Consistent active state highlighting + breadcrumbs
- ✅ Complete removal of Hotwire/Turbo/Stimulus
- ✅ Establish clear conventions for all future features

---

## 1. Global Inventory & Duplication Map

### 1.1 Current State Analysis

#### UI Fragments Duplicated Across Features

| Component | Current Locations | Duplication Level | Target Solution |
|-----------|------------------|-------------------|-----------------|
| **Flash Messages** | Inline in layouts, hardcoded ViewData checks | High | Centralized `AlertTagHelper` + partial |
| **Breadcrumbs** | None (missing feature) | N/A | New `BreadcrumbsTagHelper` + metadata |
| **Action Bars** | ContentTypes has `_ActionBar.cshtml`, others inline | High | Shared `_ActionBar.cshtml` partial |
| **Nav Tabs** | ContentTypes has `_ContentTypeNavTabs.cshtml`, others inline | Medium | Pattern: feature-specific partials using shared models |
| **Table Headers** | `_Partials/TableColumnHeader.cshtml` ✅ | Low | Keep, enhance with better type safety |
| **Pagination** | `_Partials/TablePagination.cshtml` ✅ | Low | Keep, verify route handling |
| **Empty States** | `_Partials/_EmptyState.cshtml` ✅ | Low | Keep, ensure consistent usage |
| **Validation Summary** | `_Partials/_ValidationSummary.cshtml` ✅ | Low | Keep, integrate with forms |
| **Search Bars** | `_Partials/_SearchBar.cshtml` + `TableCreateAndSearchBar.cshtml` | Medium | Consolidate into one component |
| **Modal Dialogs** | Inline in multiple pages | High | Create `ConfirmActionTagHelper` + partial |
| **File Upload Widgets** | `_Partials/FileUpload.cshtml` ✅ | Low | Keep, remove Stimulus dependencies |
| **Sub-Action Layouts** | Duplicated across Admins, ContentItems, EmailTemplates, NavigationMenus | High | Single shared `SubActionLayout.cshtml` |

#### PageModel Concerns Duplicated

| Concern | Current State | Target Solution |
|---------|---------------|-----------------|
| **Auth/Authorization** | `[Authorize]` on each PageModel | Move to folder-level conventions + base class |
| **Tenant/Context Loading** | Service locator pattern in `BasePageModel` ✅ | Keep, enhance with typed contexts |
| **Pagination Binding** | `IHasListView<T>` interface + reflection in `OnPageHandlerExecutionAsync` ✅ | Keep, document pattern |
| **TempData Alerts** | `SetErrorMessage`, `SetSuccessMessage` in `BasePageModel` ✅ | Keep, add structured alert models |
| **Domain Error Mapping** | `SetErrorMessage(IEnumerable<ValidationFailure>)` ✅ | Keep, enhance with ProblemDetails |
| **LinkGenerator Use** | Not currently abstracted | Add helper methods to base class |
| **Logging** | Logger via service locator ✅ | Keep pattern |
| **Route Value Handling** | Scattered across PageModels | Centralize common route value helpers |

### 1.2 JavaScript/Stimulus Inventory

**Files to Remove:**
- `wwwroot/raytha_admin/js/dist/controllers/` (all Stimulus controllers)
- References to `data-controller`, `data-action`, `data-turbo-*` attributes
- Stimulus library from package.json/bundler

**Replacement Strategy:**
- Minimal vanilla JS where absolutely necessary
- Rely on Bootstrap 5 native components
- Standard form posts with Razor Page handlers
- Progressive enhancement for non-critical features

---

## 2. Target Structure & Conventions

### 2.1 Proposed Directory Hierarchy

```
src/Raytha.Web/Areas/Admin/Pages/
│
├── _ViewStart.cshtml                    # Sets default layout
├── _ViewImports.cshtml                  # Global imports + TagHelper registration
├── _Layout.cshtml                       # ✅ Already exists - base HTML shell
│
├── Shared/                              # Cross-feature shared resources
│   ├── _Layouts/
│   │   ├── SidebarLayout.cshtml        # ✅ Main admin layout with sidebar nav
│   │   ├── FullWidthLayout.cshtml      # ✅ Full-width layout (no sidebar)
│   │   ├── EmptyLayout.cshtml          # ✅ Minimal layout (login, setup)
│   │   ├── SubActionLayout.cshtml      # NEW: Consolidate all SubAction layouts
│   │   └── AdminsAndRolesLayout.cshtml # ✅ Specialized layout
│   │
│   ├── _Partials/                       # Markup-only fragments
│   │   ├── _AlertMessage.cshtml        # NEW: Replaces FlashMessage.cshtml
│   │   ├── _Breadcrumbs.cshtml         # NEW: Breadcrumb navigation
│   │   ├── _EmptyState.cshtml          # ✅ Keep
│   │   ├── _ValidationSummary.cshtml   # ✅ Keep
│   │   ├── _TableShell.cshtml          # NEW: Standard table wrapper
│   │   ├── _TableColumnHeader.cshtml   # ✅ Keep
│   │   ├── _TablePagination.cshtml     # ✅ Keep
│   │   ├── _SearchBar.cshtml           # ✅ Keep, enhance
│   │   ├── _ActionBar.cshtml           # NEW: Move from ContentTypes
│   │   ├── _ConfirmDialog.cshtml       # NEW: Standard confirmation modal
│   │   ├── _FileUpload.cshtml          # ✅ Refactor FileUpload.cshtml
│   │   ├── _PageHeading.cshtml         # ✅ Keep
│   │   ├── _BackToList.cshtml          # ✅ Keep
│   │   └── _Footer.cshtml              # ✅ Keep
│   │
│   ├── Components/                      # ViewComponents (data-driven)
│   │   ├── Sidebar/
│   │   │   ├── SidebarViewComponent.cs
│   │   │   └── Default.cshtml
│   │   ├── Toolbar/
│   │   │   ├── ToolbarViewComponent.cs
│   │   │   └── Default.cshtml
│   │   └── UserMenu/
│   │       ├── UserMenuViewComponent.cs
│   │       └── Default.cshtml
│   │
│   ├── TagHelpers/                      # Move from Areas/Shared/TagHelpers
│   │   ├── NavLinkTagHelper.cs         # ✅ Already exists - enhance
│   │   ├── BreadcrumbsTagHelper.cs     # NEW
│   │   ├── AlertTagHelper.cs           # NEW
│   │   ├── ConfirmActionTagHelper.cs   # NEW
│   │   ├── RouteLink TagHelper.cs      # NEW: Helper for route-based links
│   │   └── ValidationTagHelper.cs      # ✅ Keep
│   │
│   ├── Models/                          # Shared view/input models
│   │   ├── BasePageModel.cs            # ✅ Move from Areas/Shared/Models
│   │   ├── BaseAdminPageModel.cs       # ✅ Already exists
│   │   ├── BaseContentTypeContextPageModel.cs # ✅ Keep
│   │   ├── PaginationQuery.cs          # NEW
│   │   ├── SortSpec.cs                 # NEW
│   │   ├── AlertMessage.cs             # NEW: Structured alert model
│   │   ├── BreadcrumbNode.cs           # NEW
│   │   ├── ActionBarModel.cs           # ✅ Keep
│   │   ├── ContentTypeNavModel.cs      # ✅ Keep
│   │   ├── EmptyStateModel.cs          # ✅ Keep
│   │   └── [other existing models]
│   │
│   ├── Infrastructure/
│   │   ├── Routing/
│   │   │   ├── RouteNames.cs           # ✅ Exists - expand significantly
│   │   │   ├── RazorPagesConventions.cs # NEW: Configure route names
│   │   │   └── RouteValueHelpers.cs    # NEW: Common route value helpers
│   │   │
│   │   └── Navigation/
│   │       ├── NavMap.cs               # NEW: Navigation structure metadata
│   │       ├── NavMenuItem.cs          # NEW: Nav menu item model
│   │       └── BreadcrumbProvider.cs   # NEW: Generate breadcrumbs from routes
│   │
│   └── RouteNames.cs                    # ✅ Already exists at this location
│
├── Dashboard/
│   └── Index.cshtml[.cs]
│
├── Users/                               # Feature folders (existing)
│   ├── Index.cshtml[.cs]
│   ├── Create.cshtml[.cs]
│   ├── Edit.cshtml[.cs]
│   ├── Delete.cshtml[.cs]
│   ├── Suspend.cshtml[.cs]
│   ├── Restore.cshtml[.cs]
│   └── ResetPassword.cshtml[.cs]
│
├── ContentTypes/                        # ✅ Already well-structured
│   ├── _Partials/
│   │   ├── _ActionBar.cshtml           # Feature-specific action bar
│   │   └── _ContentTypeNavTabs.cshtml  # Feature-specific nav
│   ├── Fields/
│   ├── Views/
│   └── [pages]
│
├── ContentItems/
├── Admins/
├── Roles/
├── UserGroups/
├── EmailTemplates/
├── Themes/
├── NavigationMenus/
├── RaythaFunctions/
├── AuthenticationSchemes/
├── Configuration/
├── Smtp/
├── AuditLogs/
├── Profile/
├── Setup/
└── Login/
```

### 2.2 File Naming Conventions

- **Layouts:** `[Name]Layout.cshtml` (e.g., `SidebarLayout.cshtml`)
- **Partials:** `_[Name].cshtml` (prefix with underscore)
- **ViewComponents:** `[Name]ViewComponent.cs` + `Default.cshtml`
- **TagHelpers:** `[Name]TagHelper.cs` (no view file)
- **PageModels:** `[Name]PageModel.cs` for base classes, `[PageName].cshtml.cs` for pages
- **Models:** `[Name]Model.cs` or `[Name]ViewModel.cs`

### 2.3 Feature Folder Guidelines

**When to create feature-specific partials:**
- Feature-specific navigation (like ContentTypes tabs)
- Feature-specific action bars with unique buttons
- Feature-specific form sections used multiple times within that feature

**Location:** `Pages/[Feature]/_Partials/[Name].cshtml`

**Example:** `Pages/ContentTypes/_Partials/_ContentTypeNavTabs.cshtml` ✅

---

## 3. Layouts & Templates

### 3.1 Layout Hierarchy

```
_Layout.cshtml (HTML shell)
├── EmptyLayout.cshtml (Login, Setup)
├── FullWidthLayout.cshtml (Reports, wizards)
└── SidebarLayout.cshtml (Main admin interface)
    ├── SubActionLayout.cshtml (Detail views with back navigation)
    ├── AdminsAndRolesLayout.cshtml (Specialized)
    └── [Future feature layouts]
```

### 3.2 _Layout.cshtml (Base)

**Purpose:** HTML shell, global scripts, base CSS

**Sections:**
- `headstyles` (optional) - Additional CSS
- `Scripts` (optional) - Page-specific JS

**Current State:** ✅ Already minimal and correct

**Changes Needed:**
- Remove any Stimulus/Turbo script references
- Ensure Bootstrap 5 is the only major dependency

### 3.3 SidebarLayout.cshtml (Primary Admin Layout)

**Purpose:** Main admin interface with sidebar navigation

**Regions:**
- Sidebar (left) - Main navigation menu
- Content (right) - Page content area

**Current State:** Inline navigation in layout file

**Refactor Plan:**
- Extract sidebar to `Sidebar` ViewComponent
- Use `NavMap.cs` for menu structure
- Apply `NavLinkTagHelper` for active state
- Add breadcrumbs region at top of content

**Template:**

```html
@inject IAuthorizationService AuthorizationService
@{
    Layout = "_Layout";
}

<!-- Sidebar ViewComponent -->
<vc:sidebar />

<main role="main" class="pb-3 content">
    <!-- Breadcrumbs -->
    <breadcrumbs />
    
    <!-- Flash Messages -->
    <alert />
    
    <!-- Page Content -->
    @RenderBody()
    
    <!-- Footer -->
    @await Html.PartialAsync("_Partials/_Footer")
</main>
```

### 3.4 SubActionLayout.cshtml (Consolidated)

**Purpose:** Detail/edit pages with "Back to list" navigation

**Current Problem:** Duplicated in `Admins/`, `ContentItems/`, `EmailTemplates/`, `NavigationMenus/`

**Solution:** Single shared layout in `Pages/Shared/_Layouts/SubActionLayout.cshtml`

**Required ViewData:**
- `Title` (string) - Page title
- `ActiveMenu` (string) - For sidebar active state
- `BackLinkRoute` (string) - RouteNames constant
- `BackLinkRouteValues` (object?, optional) - Route values

**Template:**

```html
@{
    Layout = "SidebarLayout";
    var backRoute = ViewData["BackLinkRoute"] as string;
    var backRouteValues = ViewData["BackLinkRouteValues"] as Dictionary<string, string>;
}

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            @if (!string.IsNullOrEmpty(backRoute))
            {
                @await Html.PartialAsync("_Partials/_BackToList", new BackLinkOptions
                {
                    Route = backRoute,
                    RouteValues = backRouteValues,
                    Label = ViewData["BackLinkLabel"] as string ?? "Back to list"
                })
            }
            
            <h2>@ViewData["Title"]</h2>
            
            @RenderBody()
        </div>
    </div>
</div>
```

### 3.5 Layout Fallback Order

1. Page specifies `Layout = "SpecificLayout";` → Use that
2. Feature has `_ViewStart.cshtml` → Use feature default
3. Global `_ViewStart.cshtml` → `Layout = "_Layout";`
4. Most pages will set `Layout = "SidebarLayout";` or `Layout = "SubActionLayout";`

---

## 4. Partials vs ViewComponents vs TagHelpers

### 4.1 Decision Matrix

| Pattern | Use When | Data Source | Example |
|---------|----------|-------------|---------|
| **TagHelper** | Modify/enhance single elements, conditional attributes, no data fetching | ViewContext, attributes | `nav-active-section`, `<breadcrumbs />`, `<alert />` |
| **ViewComponent** | Data fetching required, complex logic, reusable widgets | Inject services, query DB | Sidebar (check permissions), Toolbar (recent items) |
| **Partial** | Pure markup reuse, receives model from parent | Passed as model | Empty state, table shell, validation summary |

### 4.2 Exact Assignments

#### TagHelpers to Create

| Name | Purpose | Attributes | Output |
|------|---------|------------|--------|
| **NavLinkTagHelper** | ✅ Exists - Mark active nav links | `nav-active-section`, `nav-active-class` | Adds CSS class |
| **BreadcrumbsTagHelper** | Generate breadcrumbs | `breadcrumb-route` (optional override) | `<nav><ol class="breadcrumb">` |
| **AlertTagHelper** | Render alert messages | `alert-key` (TempData key) | Bootstrap alert div |
| **ConfirmActionTagHelper** | Add confirmation to links/buttons | `confirm-message`, `confirm-title` | data-bs-toggle="modal" |
| **RouteLinkTagHelper** | Generate links from route names | `route-name`, `route-values` | `<a href="...">` |

#### ViewComponents to Create

| Name | Purpose | Parameters | Data Source |
|------|---------|------------|-------------|
| **SidebarViewComponent** | Main navigation menu | None (reads from NavMap) | NavMap, IAuthorizationService, ICurrentOrganization |
| **ToolbarViewComponent** | Page-specific toolbar actions | `string context` | Based on context (e.g., "ContentTypes") |
| **UserMenuViewComponent** | Current user dropdown menu | None | ICurrentUser, ICurrentOrganization |

#### Partials to Keep/Create

| Name | Purpose | Model Type | Usage |
|------|---------|------------|-------|
| `_AlertMessage.cshtml` | Render structured alert | `AlertMessage` | Replaces `FlashMessage.cshtml` |
| `_Breadcrumbs.cshtml` | Breadcrumb navigation | `IEnumerable<BreadcrumbNode>` | Used by BreadcrumbsTagHelper |
| `_EmptyState.cshtml` | ✅ Keep | `EmptyStateModel` | When lists are empty |
| `_ValidationSummary.cshtml` | ✅ Keep | `ValidationFailures` | Form validation errors |
| `_TableShell.cshtml` | Standard table wrapper | `TableShellModel` | Wraps all data tables |
| `_TableColumnHeader.cshtml` | ✅ Keep | `BaseTableColumnHeaderViewModel` | Sortable column headers |
| `_TablePagination.cshtml` | ✅ Keep | `IPaginationViewModel` | Pagination controls |
| `_SearchBar.cshtml` | ✅ Consolidate | `SearchBarModel` | Search input + filters |
| `_ActionBar.cshtml` | Page action buttons | `ActionBarModel` | Create, export, bulk actions |
| `_ConfirmDialog.cshtml` | Confirmation modal | `ConfirmActionModel` | Delete, bulk operations |
| `_FileUpload.cshtml` | ✅ Refactor | `FileUploadOptions` | Remove Stimulus, use native |
| `_PageHeading.cshtml` | ✅ Keep | `string` (title) | Page title with icon |
| `_BackToList.cshtml` | ✅ Keep | `BackLinkOptions` | Back navigation link |
| `_Footer.cshtml` | ✅ Keep | None | Footer content |

### 4.3 Migration Path for Each Page

**Before:**
```html
@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}
```

**After:**
```html
<alert alert-key="ErrorMessage" />
```

**Before:**
```html
<a asp-page="/Users/Index" class="@(ViewData["ActiveMenu"] == "Users" ? "active" : "")">Users</a>
```

**After:**
```html
<a asp-page="@RouteNames.Users.Index" nav-active-section="Users">Users</a>
```

---

## 5. Shared Models & Base PageModels

### 5.1 New Shared Models

#### PaginationQuery.cs

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Standard pagination and sorting query parameters.
/// </summary>
public record PaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? OrderBy { get; init; }
    public string? Search { get; init; }
    public string? Filter { get; init; }
}
```

#### SortSpec.cs

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Sort specification for queries.
/// </summary>
public record SortSpec(string PropertyName, string Direction)
{
    public static SortSpec? FromString(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy)) return null;
        var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return null;
        return new SortSpec(parts[0], parts[1]);
    }
    
    public override string ToString() => $"{PropertyName} {Direction}";
}
```

#### AlertMessage.cs

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Structured alert message for TempData serialization.
/// </summary>
public record AlertMessage
{
    public AlertType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Title { get; init; }
    public bool IsDismissible { get; init; } = true;
}

public enum AlertType
{
    Success,
    Error,
    Warning,
    Info
}
```

#### BreadcrumbNode.cs

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Single breadcrumb node.
/// </summary>
public record BreadcrumbNode
{
    public string Label { get; init; } = string.Empty;
    public string? RouteName { get; init; }
    public Dictionary<string, string>? RouteValues { get; init; }
    public bool IsActive { get; init; }
}
```

### 5.2 BasePageModel Enhancements

**Current State:** ✅ Already has excellent foundation

**Enhancements Needed:**

```csharp
// Add to BasePageModel

/// <summary>
/// Sets a structured alert message to be displayed to the user.
/// </summary>
protected void SetAlert(AlertMessage alert)
{
    TempData["Alert"] = JsonSerializer.Serialize(alert);
}

/// <summary>
/// Gets the alert message from TempData if it exists.
/// </summary>
protected AlertMessage? GetAlert()
{
    if (TempData["Alert"] is string json)
    {
        return JsonSerializer.Deserialize<AlertMessage>(json);
    }
    return null;
}

/// <summary>
/// Generates a page URL from route name and values.
/// </summary>
protected string GetPageUrl(string routeName, object? values = null)
{
    return Url.Page(routeName, values);
}

/// <summary>
/// Sets breadcrumbs for the current page.
/// </summary>
protected void SetBreadcrumbs(params BreadcrumbNode[] breadcrumbs)
{
    ViewData["Breadcrumbs"] = breadcrumbs;
}
```

### 5.3 Feature-Specific Base PageModels

#### Pattern

For features with shared context (like ContentTypes), keep specialized base classes:

```csharp
// Already exists ✅
public abstract class BaseContentTypeContextPageModel : BaseAdminPageModel
{
    // Load content type context
    // Provide common properties
    // Handle common authorization
}
```

**Apply to:**
- `BaseContentTypeContextPageModel` ✅ (already exists)
- `BaseContentItemsPageModel` (new)
- `BaseThemesPageModel` (new)
- `BaseNavigationMenusPageModel` (new)

### 5.4 Base PageModel Inheritance Tree

```
BasePageModel (Raytha.Web.Areas.Shared.Models)
└── BaseAdminPageModel ([Authorize(IsAdmin)])
    ├── BaseDashboardPageModel
    ├── BaseUsersPageModel
    ├── BaseContentTypeContextPageModel
    │   ├── ContentTypes/*PageModel
    │   └── ContentTypes/Fields/*PageModel
    ├── BaseContentItemsPageModel
    ├── BaseThemesPageModel
    ├── BaseAdminsPageModel
    └── [other feature base models]
```

---

## 6. Routing, Route Names, and DRY

### 6.1 Current State

**Good:**
- ✅ `RouteNames.cs` exists with nested static classes
- ✅ Already covers major features
- ✅ Pages use `asp-page="@RouteNames.X.Y"`

**Gaps:**
- Missing route names for: Themes, EmailTemplates, NavigationMenus, RaythaFunctions, AuditLogs, Profile
- No centralized route configuration (names registered manually on pages)
- No route metadata for navigation/breadcrumbs

### 6.2 Expanded RouteNames.cs

**Add missing sections:**

```csharp
// Add to RouteNames.cs

public static class Themes
{
    public const string Index = "/Themes/Index";
    public const string Create = "/Themes/Create";
    public const string Edit = "/Themes/Edit";
    public const string Delete = "/Themes/Delete";
    public const string Templates = "/Themes/Templates";
    
    public static class Templates
    {
        public const string Index = "/Themes/Templates/Index";
        public const string Create = "/Themes/Templates/Create";
        public const string Edit = "/Themes/Templates/Edit";
        public const string Delete = "/Themes/Templates/Delete";
    }
}

public static class EmailTemplates
{
    public const string Index = "/EmailTemplates/Index";
    public const string Edit = "/EmailTemplates/Edit";
    public const string Revisions = "/EmailTemplates/Revisions";
}

public static class NavigationMenus
{
    public const string Index = "/NavigationMenus/Index";
    public const string Create = "/NavigationMenus/Create";
    public const string Edit = "/NavigationMenus/Edit";
    public const string Delete = "/NavigationMenus/Delete";
    public const string Revisions = "/NavigationMenus/Revisions";
    public const string SetAsMainMenu = "/NavigationMenus/SetAsMainMenu";
    
    public static class MenuItems
    {
        public const string Index = "/NavigationMenus/MenuItems/Index";
        public const string Create = "/NavigationMenus/MenuItems/Create";
        public const string Edit = "/NavigationMenus/MenuItems/Edit";
        public const string Delete = "/NavigationMenus/MenuItems/Delete";
        public const string Reorder = "/NavigationMenus/MenuItems/Reorder";
    }
}

public static class RaythaFunctions
{
    public const string Index = "/RaythaFunctions/Index";
    public const string Create = "/RaythaFunctions/Create";
    public const string Edit = "/RaythaFunctions/Edit";
    public const string Delete = "/RaythaFunctions/Delete";
}

public static class AuthenticationSchemes
{
    public const string Index = "/AuthenticationSchemes/Index";
    public const string Create = "/AuthenticationSchemes/Create";
    public const string Edit = "/AuthenticationSchemes/Edit";
    public const string Delete = "/AuthenticationSchemes/Delete";
}

public static class AuditLogs
{
    public const string Index = "/AuditLogs/Index";
}

public static class Profile
{
    public const string Index = "/Profile/Index";
    public const string ChangePassword = "/Profile/ChangePassword";
}

public static class Smtp
{
    public const string Index = "/Smtp/Index";
}

public static class Dashboard
{
    public const string Index = "/Dashboard/Index";
}

public static class Login
{
    public const string LoginWithEmailAndPassword = "/Login/LoginWithEmailAndPassword";
    public const string LoginWithMagicLink = "/Login/LoginWithMagicLink";
    public const string LoginWithSaml = "/Login/LoginWithSaml";
    public const string LoginWithSso = "/Login/LoginWithSso";
    public const string LoginWithJwt = "/Login/LoginWithJwt";
    public const string ForgotPassword = "/Login/ForgotPassword";
    public const string Logout = "/Login/Logout";
}
```

### 6.3 Route Configuration Pattern

**No custom route registration needed** - Razor Pages convention already works with `@page` directive.

**Standard pattern for all pages:**

```csharp
@page "/raytha/users"
// or with route parameters:
@page "/raytha/users/{id:guid}"
```

**Reference via RouteNames:**

```html
<a asp-page="@RouteNames.Users.Index">Users</a>
<a asp-page="@RouteNames.Users.Edit" asp-route-id="@userId">Edit</a>
```

### 6.4 Route Audit Strategy

**Forbidden patterns (grep should find ZERO):**

```bash
# Hardcoded paths
grep -r 'href="/' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages

# Inline route strings (should use RouteNames)
grep -r 'asp-page="/[^@]' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages
```

**CI Check:**

```bash
#!/bin/bash
# scripts/check-routes.sh

echo "Checking for hardcoded routes..."

# Allow asp-page="@RouteNames..." or asp-page="@Model.X"
# Disallow asp-page="/literal"

HARDCODED=$(grep -rn 'asp-page="/[^@]' src/Raytha.Web/Areas/Admin/Pages --include="*.cshtml" || true)

if [ -n "$HARDCODED" ]; then
    echo "❌ Found hardcoded routes:"
    echo "$HARDCODED"
    exit 1
fi

echo "✅ No hardcoded routes found"
```

### 6.5 LinkGenerator Helper Methods

**Add to BasePageModel:**

```csharp
/// <summary>
/// Gets the path for a page route by name.
/// </summary>
protected string GetPath(string routeName, object? values = null)
{
    return Url.Page(routeName, values) ?? "#";
}

/// <summary>
/// Gets the absolute URI for a page route.
/// </summary>
protected string GetUri(string routeName, object? values = null)
{
    return Url.Page(routeName, values, protocol: HttpContext.Request.Scheme) ?? "#";
}
```

---

## 7. Navigation with Active State & Breadcrumbs

### 7.1 NavLinkTagHelper Enhancement

**Current Implementation:** ✅ Already exists and works well

**Enhancements Needed:**

```csharp
// Add exact matching mode
[HtmlAttributeName("nav-match-mode")]
public string MatchMode { get; set; } = "contains"; // "contains" | "exact" | "startswith"

// Support route name matching (in addition to path)
[HtmlAttributeName("nav-route-name")]
public string? RouteNameToMatch { get; set; }

private bool IsActive(string? currentPage, string? currentRouteName, string navSection)
{
    if (!string.IsNullOrWhiteSpace(RouteNameToMatch))
    {
        return string.Equals(currentRouteName, RouteNameToMatch, StringComparison.OrdinalIgnoreCase);
    }
    
    // Existing path-based matching logic...
    return MatchMode switch
    {
        "exact" => currentPage?.Equals(navSection, StringComparison.OrdinalIgnoreCase) ?? false,
        "startswith" => currentPage?.StartsWith(navSection, StringComparison.OrdinalIgnoreCase) ?? false,
        _ => currentPage?.Contains(navSection, StringComparison.OrdinalIgnoreCase) ?? false
    };
}
```

### 7.2 BreadcrumbsTagHelper (New)

**Purpose:** Generate breadcrumbs from route metadata or manual override

**Implementation:**

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.TagHelpers;

/// <summary>
/// Tag helper for rendering breadcrumbs navigation.
/// Usage: &lt;breadcrumbs /&gt; or &lt;breadcrumbs route-name="@routeName" /&gt;
/// </summary>
[HtmlTargetElement("breadcrumbs")]
public class BreadcrumbsTagHelper : TagHelper
{
    private readonly IViewComponentHelper _viewComponentHelper;
    
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;
    
    [HtmlAttributeName("route-name")]
    public string? RouteName { get; set; }
    
    public BreadcrumbsTagHelper(IViewComponentHelper viewComponentHelper)
    {
        _viewComponentHelper = viewComponentHelper;
    }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Get breadcrumbs from ViewData or generate from route
        var breadcrumbs = ViewContext.ViewData["Breadcrumbs"] as IEnumerable<BreadcrumbNode>;
        
        if (breadcrumbs == null)
        {
            // Auto-generate from route (see BreadcrumbProvider)
            breadcrumbs = GenerateBreadcrumbsFromRoute();
        }
        
        if (!breadcrumbs.Any())
        {
            output.SuppressOutput();
            return;
        }
        
        output.TagName = null; // Remove <breadcrumbs> tag
        var content = await _viewComponentHelper.InvokeAsync(
            "Breadcrumbs", 
            new { breadcrumbs }
        );
        output.Content.SetHtmlContent(content);
    }
    
    private IEnumerable<BreadcrumbNode> GenerateBreadcrumbsFromRoute()
    {
        // Use NavMap or BreadcrumbProvider service
        // Example: /Users/Edit -> ["Dashboard", "Users", "Edit User"]
        return new List<BreadcrumbNode>
        {
            new() { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            // ... build from route path
        };
    }
}
```

### 7.3 NavMap.cs (Navigation Metadata)

**Purpose:** Single source of truth for navigation structure

**Implementation:**

```csharp
namespace Raytha.Web.Areas.Admin.Pages.Shared.Infrastructure.Navigation;

/// <summary>
/// Centralized navigation structure for the admin area.
/// Defines menu items, hierarchy, permissions, and icons.
/// </summary>
public static class NavMap
{
    public static IEnumerable<NavMenuItem> GetMenuItems()
    {
        return new[]
        {
            new NavMenuItem
            {
                Id = "Dashboard",
                Label = "Dashboard",
                RouteName = RouteNames.Dashboard.Index,
                Icon = IconLibrary.Dashboard,
                Permission = null, // Public to all authenticated admins
                Order = 0
            },
            new NavMenuItem
            {
                Id = "Users",
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                Icon = IconLibrary.Users,
                Permission = BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                Order = 10
            },
            // Dynamic content types loaded at runtime
            // ...
            new NavMenuItem
            {
                Id = "Themes",
                Label = "Themes",
                RouteName = RouteNames.Themes.Index,
                Icon = IconLibrary.Themes,
                Permission = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                Order = 100
            },
            new NavMenuItem
            {
                Id = "Settings",
                Label = "Settings",
                Icon = IconLibrary.Settings,
                Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                Order = 200,
                Children = new[]
                {
                    new NavMenuItem
                    {
                        Id = "Admins",
                        Label = "Admins",
                        RouteName = RouteNames.Admins.Index,
                        Permission = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION
                    },
                    new NavMenuItem
                    {
                        Id = "Configuration",
                        Label = "Configuration",
                        RouteName = RouteNames.Configuration.Index
                    },
                    new NavMenuItem
                    {
                        Id = "AuthenticationSchemes",
                        Label = "Authentication",
                        RouteName = RouteNames.AuthenticationSchemes.Index
                    },
                    new NavMenuItem
                    {
                        Id = "Smtp",
                        Label = "SMTP",
                        RouteName = RouteNames.Smtp.Index
                    }
                }
            }
        };
    }
}

public class NavMenuItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? RouteName { get; init; }
    public string? Icon { get; init; }
    public string? Permission { get; init; }
    public int Order { get; init; }
    public IEnumerable<NavMenuItem>? Children { get; init; }
}

public static class IconLibrary
{
    // SVG icon strings
    public const string Dashboard = "<svg>...</svg>";
    public const string Users = "<svg>...</svg>";
    // ... etc
}
```

### 7.4 SidebarViewComponent (Using NavMap)

**Purpose:** Render sidebar navigation from NavMap with permission checks

```csharp
public class SidebarViewComponent : ViewComponent
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentOrganization _currentOrganization;
    
    public SidebarViewComponent(
        IAuthorizationService authorizationService,
        ICurrentOrganization currentOrganization)
    {
        _authorizationService = authorizationService;
        _currentOrganization = currentOrganization;
    }
    
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menuItems = NavMap.GetMenuItems();
        var filteredItems = new List<NavMenuItem>();
        
        foreach (var item in menuItems)
        {
            if (await IsAuthorized(item))
            {
                filteredItems.Add(item);
            }
        }
        
        // Add dynamic content types
        foreach (var contentType in _currentOrganization.ContentTypes)
        {
            if (await CanReadContentType(contentType))
            {
                filteredItems.Add(new NavMenuItem
                {
                    Id = contentType.DeveloperName,
                    Label = contentType.LabelPlural,
                    RouteName = RouteNames.ContentItems.Index,
                    Icon = IconLibrary.ContentType,
                    Order = 50 // Between Users and Themes
                });
            }
        }
        
        return View(filteredItems.OrderBy(i => i.Order));
    }
    
    private async Task<bool> IsAuthorized(NavMenuItem item)
    {
        if (string.IsNullOrEmpty(item.Permission))
            return true;
            
        return (await _authorizationService.AuthorizeAsync(
            UserClaimsPrincipal,
            item.Permission)).Succeeded;
    }
    
    private async Task<bool> CanReadContentType(/* ... */)
    {
        // Check content type read permission
        return true; // Implement
    }
}
```

### 7.5 Active State Strategy

**Approach:** Use `ViewData["ActiveMenu"]` + `NavLinkTagHelper`

**In PageModel:**

```csharp
public class IndexModel : BaseAdminPageModel
{
    public void OnGet()
    {
        ViewData["ActiveMenu"] = "Users"; // Matches NavMenuItem.Id
    }
}
```

**In Layout/Nav:**

```html
<a asp-page="@RouteNames.Users.Index" 
   nav-active-section="Users">
   Users
</a>
```

**For sub-navigation (like ContentTypes tabs):**

Use existing pattern with `ContentTypeNavModel.ActiveTab` enum ✅

---

## 8. Purge Hotwire/Stimulus Globally

### 8.1 Removal Checklist

#### JavaScript Files

**Delete entire directories:**
- ✅ `/wwwroot/raytha_admin/js/dist/controllers/`
- ✅ `/wwwroot/raytha_admin/js/dist/types/controllers/`
- ✅ `/wwwroot/raytha_admin/js/src/controllers/` (if exists)

**Remove from package.json:**
```json
{
  "dependencies": {
    "@hotwired/stimulus": "REMOVE",
    "@hotwired/turbo": "REMOVE",
    "stimulus": "REMOVE"
  }
}
```

**Remove bundler imports:**
- Search for `import * as Stimulus` or `import { Controller } from 'stimulus'`
- Remove Stimulus application initialization

#### HTML Attributes

**Search and remove:**

```bash
# Find all Stimulus data attributes
grep -r 'data-controller=' src/Raytha.Web/Areas/Admin/Pages --include="*.cshtml"
grep -r 'data-action=' src/Raytha.Web/Areas/Admin/Pages --include="*.cshtml"
grep -r 'data-turbo' src/Raytha.Web/Areas/Admin/Pages --include="*.cshtml"
grep -r 'data-.*-target=' src/Raytha.Web/Areas/Admin/Pages --include="*.cshtml"
```

**Example replacements:**

| Before (Stimulus) | After (Vanilla/Bootstrap) |
|-------------------|---------------------------|
| `data-controller="shared--filter"` | Remove, use standard forms |
| `data-action="click->modal#open"` | `data-bs-toggle="modal" data-bs-target="#myModal"` (Bootstrap) |
| `data-shared--fieldchoices-target="row"` | Remove, use standard DOM queries if needed |
| `data-turbo="false"` | Remove |
| `data-turbo-frame="content"` | Remove |

### 8.2 Feature-by-Feature Replacement

#### File Upload (Remove Uppy/Stimulus)

**Before:** Stimulus controller with Uppy.js

**After:** Native HTML5 file input with basic progress

```html
<input type="file" 
       class="form-control" 
       name="file" 
       accept="image/*"
       multiple
       onchange="handleFileSelect(this)">
       
<div id="upload-progress" class="progress d-none">
    <div class="progress-bar" role="progressbar"></div>
</div>

<script>
function handleFileSelect(input) {
    // Basic vanilla JS for preview/validation
    // Use FormData + fetch for AJAX upload if needed
}
</script>
```

#### Sortable Lists (Remove Stimulus Sortable)

**Before:** `data-controller="shared--reorderlist"`

**After:** Use SortableJS library directly (lighter weight) OR server-side reorder

```html
<!-- Option 1: SortableJS (lightweight, no Stimulus wrapper) -->
<ul id="sortable-list" class="list-group">
    <li class="list-group-item" data-id="1">Item 1</li>
    <li class="list-group-item" data-id="2">Item 2</li>
</ul>

<script src="https://cdn.jsdelivr.net/npm/sortablejs@latest/Sortable.min.js"></script>
<script>
    Sortable.create(document.getElementById('sortable-list'), {
        onEnd: function(evt) {
            // Save new order via AJAX
        }
    });
</script>

<!-- Option 2: Server-side with up/down buttons -->
<form method="post" asp-page-handler="MoveUp" asp-route-id="@item.Id">
    <button type="submit" class="btn btn-sm btn-link">↑</button>
</form>
```

#### Autocomplete (Remove Stimulus Autocomplete)

**Before:** `data-controller="shared--autocomplete"`

**After:** Use Bootstrap 5 dropdown + datalist OR lightweight autocomplete library

```html
<!-- HTML5 datalist (simple) -->
<input list="users-list" 
       class="form-control" 
       name="userId">
<datalist id="users-list">
    @foreach (var user in Model.Users)
    {
        <option value="@user.Id">@user.FullName</option>
    }
</datalist>

<!-- OR use tom-select (Choices.js replacement) for complex cases -->
<select id="user-select" class="form-control">
    <option value="">Select user...</option>
    @foreach (var user in Model.Users)
    {
        <option value="@user.Id">@user.FullName</option>
    }
</select>

<script src="https://cdn.jsdelivr.net/npm/tom-select@2/dist/js/tom-select.complete.min.js"></script>
<script>
    new TomSelect('#user-select', {
        maxItems: 1,
        plugins: ['remove_button']
    });
</script>
```

#### Confirm Actions (Remove Stimulus Confirm)

**Before:** `data-controller="shared--confirmaction"`

**After:** Bootstrap 5 modal

```html
<!-- Trigger -->
<button type="button" 
        class="btn btn-danger"
        data-bs-toggle="modal" 
        data-bs-target="#confirm-delete"
        data-action-url="@Url.Page(RouteNames.Users.Delete, new { id = user.Id })">
    Delete
</button>

<!-- Modal -->
<div class="modal fade" id="confirm-delete" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Delete</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                Are you sure you want to delete this user?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <form method="post" id="confirm-form">
                    <button type="submit" class="btn btn-danger">Delete</button>
                </form>
            </div>
        </div>
    </div>
</div>

<script>
    // Set form action on modal show
    document.getElementById('confirm-delete').addEventListener('show.bs.modal', function(event) {
        var button = event.relatedTarget;
        var actionUrl = button.getAttribute('data-action-url');
        document.getElementById('confirm-form').action = actionUrl;
    });
</script>
```

### 8.3 CI Check for Stimulus Remnants

```bash
#!/bin/bash
# scripts/check-stimulus.sh

echo "Checking for Stimulus/Turbo remnants..."

STIMULUS_ATTRS=$(grep -r 'data-controller\|data-action\|data-turbo\|data-.*-target=' \
    src/Raytha.Web/Areas/Admin/Pages \
    --include="*.cshtml" \
    --exclude-dir=wwwroot \
    || true)

if [ -n "$STIMULUS_ATTRS" ]; then
    echo "❌ Found Stimulus/Turbo attributes:"
    echo "$STIMULUS_ATTRS"
    exit 1
fi

echo "✅ No Stimulus/Turbo remnants found"
```

---

## 9. Validation, Alerts, Empty States

### 9.1 Standard Validation Pattern

**Current State:** ✅ Already good with `BasePageModel.HasError()` and `ErrorMessageFor()`

**Enhancement:** Add partial for consistent field validation display

**New Partial:** `_Partials/_FormField.cshtml`

```html
@model FormFieldModel

<div class="mb-3">
    <label class="form-label @(Model.IsRequired ? "raytha-required" : "")" 
           for="@Model.Name">
        @Model.Label
    </label>
    @Model.Content
    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
    {
        <div class="invalid-feedback d-block">@Model.ErrorMessage</div>
    }
    @if (!string.IsNullOrEmpty(Model.HelpText))
    {
        <div class="form-text">@Model.HelpText</div>
    }
</div>
```

**Usage:**

```html
@await Html.PartialAsync("_Partials/_FormField", new FormFieldModel
{
    Name = nameof(Model.Form.Email),
    Label = "Email Address",
    IsRequired = true,
    Content = @<input type="email" 
                      class="form-control @Model.HasError(nameof(Model.Form.Email))" 
                      asp-for="Form.Email" />,
    ErrorMessage = Model.ErrorMessageFor(nameof(Model.Form.Email)),
    HelpText = "We'll never share your email with anyone else."
})
```

### 9.2 AlertTagHelper

**Purpose:** Replace inline alert rendering with TagHelper

**Implementation:**

```csharp
[HtmlTargetElement("alert")]
public class AlertTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;
    
    [HtmlAttributeName("alert-key")]
    public string? AlertKey { get; set; }
    
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        
        var keys = string.IsNullOrEmpty(AlertKey) 
            ? new[] { "ErrorMessage", "SuccessMessage", "WarningMessage", "InfoMessage" }
            : new[] { AlertKey };
        
        var html = new StringBuilder();
        
        foreach (var key in keys)
        {
            if (ViewContext.ViewData[key] is string message)
            {
                var cssClass = key.Replace("Message", "").ToLowerInvariant();
                html.AppendLine($@"
                    <div class=""alert alert-{cssClass} alert-dismissible fade show mt-2"" role=""alert"">
                        {message}
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert"" aria-label=""Close""></button>
                    </div>");
            }
        }
        
        output.Content.SetHtmlContent(html.ToString());
    }
}
```

**Usage:**

```html
<!-- Before -->
@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}

<!-- After -->
<alert />
<!-- or specific key -->
<alert alert-key="ErrorMessage" />
```

### 9.3 Empty State Standard

**Current Implementation:** ✅ `_Partials/_EmptyState.cshtml` exists

**Enhancement:** Ensure consistent usage across all list pages

**Standard Usage:**

```html
@if (!Model.ListView.Items.Any())
{
    @await Html.PartialAsync("_Partials/_EmptyState", new EmptyStateModel
    {
        Icon = IconLibrary.Users,
        Title = "No users found",
        Message = Model.ListView.Search != null 
            ? $"No users match your search for \"{Model.ListView.Search}\"" 
            : "Get started by creating your first user.",
        Action = new ActionModel
        {
            Route = RouteNames.Users.Create,
            Label = "Create User"
        }
    })
}
else
{
    <!-- Table -->
}
```

---

## 10. Security, Performance, Accessibility

### 10.1 Security

#### Authorization at Folder Level

**Current:** `[Authorize(Policy = RaythaClaimTypes.IsAdmin)]` on `BaseAdminPageModel` ✅

**Enhancement:** Add folder-level authorization via conventions

```csharp
// Program.cs or Startup.cs
services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin/Pages/Users", 
        BuiltInSystemPermission.MANAGE_USERS_PERMISSION);
    options.Conventions.AuthorizeFolder("/Admin/Pages/Admins", 
        BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION);
    // etc.
});
```

#### Antiforgery Tokens

**Current State:** Built into Razor Pages by default ✅

**Verify:** All forms use `<form method="post">` which auto-includes antiforgery token

#### Input Validation

**Current:** FluentValidation in Application layer ✅

**Ensure:** All `[BindProperty]` models have corresponding validators

### 10.2 Performance

#### Query Optimization

**Current:** `IHasListView<T>` with pagination ✅

**Enhancement:** Ensure all list queries use:
- `.AsNoTracking()` for read-only
- `.Select()` projections (not materializing full entities)
- Appropriate page size limits (already clamped to 1000 ✅)

**Add to BasePageModel:**

```csharp
/// <summary>
/// Default page size for lists.
/// </summary>
protected const int DefaultPageSize = 50;

/// <summary>
/// Maximum allowed page size.
/// </summary>
protected const int MaxPageSize = 200; // Per raytha.instructions.md

/// <summary>
/// Validates and clamps page size to acceptable range.
/// </summary>
protected int ValidatePageSize(int requestedSize)
{
    return Math.Clamp(requestedSize == 0 ? DefaultPageSize : requestedSize, 1, MaxPageSize);
}
```

#### Output Caching

**Consider for:**
- Dashboard statistics (cache 5 min)
- Configuration pages (cache until updated)
- Nav menu structure (cache 10 min)

**Example:**

```csharp
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "contentTypeDeveloperName" })]
public class IndexModel : BaseContentTypeContextPageModel
{
    // ...
}
```

#### Minimize Duplicate Context Loading

**Current:** Service locator in `BasePageModel` ✅

**Enhancement:** Load common context once in `OnPageHandlerExecutionAsync`

```csharp
// Add to BasePageModel
protected ICurrentOrganization? _cachedOrganization;
protected ICurrentOrganization CachedOrganization => 
    _cachedOrganization ??= CurrentOrganization;
```

### 10.3 Accessibility

#### Navigation

- ✅ Use semantic `<nav>` elements
- ✅ Add `role="navigation"` and `aria-label`
- ✅ Current page indicated with `aria-current="page"`
- ✅ Active state has sufficient contrast
- ✅ Keyboard navigable (all links/buttons focusable)

**Example:**

```html
<nav aria-label="Main navigation">
    <ul class="nav">
        <li class="nav-item">
            <a class="nav-link" 
               asp-page="@RouteNames.Dashboard.Index"
               nav-active-section="Dashboard"
               aria-current="@(ViewData["ActiveMenu"] == "Dashboard" ? "page" : "false")">
                Dashboard
            </a>
        </li>
    </ul>
</nav>
```

#### Forms

- ✅ All inputs have associated `<label>` with `for` attribute
- ✅ Required fields marked with `aria-required="true"` and visual indicator
- ✅ Validation errors linked with `aria-describedby`
- ✅ Error messages have `role="alert"`

**Enhanced FormFieldModel:**

```csharp
public class FormFieldModel
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HelpText { get; set; }
    public Func<object, IHtmlContent>? Content { get; set; }
    
    // Accessibility
    public string InputId => $"input-{Name}";
    public string ErrorId => $"error-{Name}";
    public string HelpId => $"help-{Name}";
    
    public string AriaDescribedBy
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(HelpText)) parts.Add(HelpId);
            if (!string.IsNullOrEmpty(ErrorMessage)) parts.Add(ErrorId);
            return string.Join(" ", parts);
        }
    }
}
```

#### Tables

- ✅ Use `<thead>`, `<tbody>`, `<th scope="col">`
- ✅ Add `aria-sort` to sortable column headers
- ✅ Provide table caption or `aria-label`

**Example:**

```html
<table class="table" aria-label="Users list">
    <thead>
        <tr>
            <th scope="col" aria-sort="@(Model.ListView.OrderByPropertyName == "FirstName" ? "ascending" : "none")">
                First Name
            </th>
        </tr>
    </thead>
    <tbody>
        <!-- rows -->
    </tbody>
</table>
```

#### Focus Management

**After Delete/Submit:**

```csharp
public async Task<IActionResult> OnPostDeleteAsync(Guid id)
{
    // Delete logic...
    
    SetSuccessMessage("User deleted successfully");
    
    // Ensure focus returns to meaningful element
    TempData["FocusOnReturn"] = "users-list";
    
    return RedirectToPage(RouteNames.Users.Index);
}
```

```html
<!-- Index page -->
<div id="users-list" tabindex="-1">
    <h1>Users</h1>
    <!-- ... -->
</div>

@if (TempData["FocusOnReturn"] is string focusId)
{
    <script>
        document.getElementById('@focusId')?.focus();
    </script>
}
```

---

## 11. Testing, Linting & Tooling

### 11.1 Unit Tests for Routing

**Create:** `Raytha.Web.Tests/Routing/RouteNamesTests.cs`

```csharp
public class RouteNamesTests
{
    [Test]
    public void RouteNames_AllConstants_AreDefined()
    {
        // Ensure no null/empty route names
        var routeFields = typeof(RouteNames)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(string));
        
        foreach (var field in routeFields)
        {
            var value = field.GetValue(null) as string;
            Assert.That(value, Is.Not.Null.And.Not.Empty, 
                $"Route {field.DeclaringType?.Name}.{field.Name} is null or empty");
        }
    }
    
    [TestCase(RouteNames.Users.Index, "/Users/Index")]
    [TestCase(RouteNames.ContentTypes.Configuration, "/ContentTypes/Configuration")]
    public void RouteNames_Constants_MatchExpectedPaths(string routeName, string expectedPath)
    {
        Assert.That(routeName, Is.EqualTo(expectedPath));
    }
}
```

### 11.2 Integration Tests for Navigation

**Create:** `Raytha.Web.Tests/Navigation/NavMapTests.cs`

```csharp
public class NavMapTests
{
    [Test]
    public void NavMap_AllMenuItems_HaveValidRouteNames()
    {
        var menuItems = NavMap.GetMenuItems();
        var allRouteNames = GetAllRouteNames();
        
        foreach (var item in menuItems.Where(i => !string.IsNullOrEmpty(i.RouteName)))
        {
            Assert.That(allRouteNames, Contains.Item(item.RouteName),
                $"NavMap item '{item.Label}' has invalid route: {item.RouteName}");
        }
    }
    
    private static HashSet<string> GetAllRouteNames()
    {
        return typeof(RouteNames)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .ToHashSet()!;
    }
}
```

### 11.3 UI Tests for Active State

**Create:** `Raytha.Web.Tests/TagHelpers/NavLinkTagHelperTests.cs`

```csharp
public class NavLinkTagHelperTests
{
    [Test]
    public async Task NavLinkTagHelper_WhenCurrentPageMatches_AddsActiveClass()
    {
        // Arrange
        var tagHelper = new NavLinkTagHelper
        {
            ViewContext = CreateViewContext("/Users/Index"),
            NavSection = "Users",
            ActiveClass = "active"
        };
        
        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("a");
        
        // Act
        tagHelper.Process(context, output);
        
        // Assert
        var classAttr = output.Attributes.FirstOrDefault(a => a.Name == "class");
        Assert.That(classAttr?.Value.ToString(), Contains.Substring("active"));
    }
    
    // Helper methods...
}
```

### 11.4 CI Checks

#### Script: `.github/workflows/pr-checks.yml`

```yaml
name: PR Checks

on: [pull_request]

jobs:
  check-routes:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Check for hardcoded routes
        run: |
          chmod +x scripts/check-routes.sh
          ./scripts/check-routes.sh
      
      - name: Check for Stimulus remnants
        run: |
          chmod +x scripts/check-stimulus.sh
          ./scripts/check-stimulus.sh
      
      - name: Verify RouteNames coverage
        run: |
          chmod +x scripts/check-route-coverage.sh
          ./scripts/check-route-coverage.sh

  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      
      - name: Run tests
        run: dotnet test --filter "Category=Routing|Category=Navigation"
      
      - name: Check view compilation
        run: dotnet build /p:MvcRazorCompileOnBuild=true

  lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Check code formatting
        run: dotnet format --verify-no-changes
```

#### Script: `scripts/check-route-coverage.sh`

```bash
#!/bin/bash
# Verify all .cshtml pages have corresponding RouteNames entries

echo "Checking route coverage..."

# Get all page paths
PAGES=$(find src/Raytha.Web/Areas/Admin/Pages -name "*.cshtml" -not -path "*/Shared/*" -not -name "_*.cshtml")

MISSING=()

for PAGE in $PAGES; do
    # Extract @page directive
    PAGE_ROUTE=$(grep -oP '@page\s+"\K[^"]+' "$PAGE" | head -1)
    
    if [ -n "$PAGE_ROUTE" ]; then
        # Check if route exists in RouteNames.cs
        if ! grep -q "\"$PAGE_ROUTE\"" src/Raytha.Web/Areas/Admin/Pages/Shared/RouteNames.cs; then
            MISSING+=("$PAGE_ROUTE (from $PAGE)")
        fi
    fi
done

if [ ${#MISSING[@]} -gt 0 ]; then
    echo "❌ Routes missing from RouteNames.cs:"
    printf '%s\n' "${MISSING[@]}"
    exit 1
fi

echo "✅ All pages have corresponding RouteNames entries"
```

### 11.5 View Compilation

**Enable in project file:**

```xml
<PropertyGroup>
    <MvcRazorCompileOnBuild>true</MvcRazorCompileOnBuild>
    <MvcRazorCompileOnPublish>true</MvcRazorCompileOnPublish>
</PropertyGroup>
```

---

## 12. Migration Plan (PR Batches)

### 12.1 PR Structure

**Goal:** Small, reviewable PRs that can be merged independently

**Estimated Total:** 15-20 PRs over 4-6 weeks

---

### **PR0: Foundation & Scaffolding** (Week 1)

**Scope:** Set up shared infrastructure without breaking existing pages

**Files Added/Modified:**
- ✅ `RouteNames.cs` - Add missing sections (Themes, EmailTemplates, etc.)
- ✅ `NavMap.cs` (new)
- ✅ `BreadcrumbNode.cs`, `AlertMessage.cs`, `SortSpec.cs`, `PaginationQuery.cs` (new models)
- ✅ `BreadcrumbsTagHelper.cs`, `AlertTagHelper.cs` (new)
- ✅ `NavLinkTagHelper.cs` - Enhancements (route name matching, match modes)
- ✅ `BasePageModel.cs` - Add `SetAlert()`, `GetPageUrl()`, `SetBreadcrumbs()` helpers
- ✅ `_ViewImports.cshtml` - Update TagHelper registration
- ✅ Unit tests for new components

**Risks:** Low - additive only

**Testing:**
- Unit tests pass
- Existing pages render correctly (no breaking changes)

**Checklist:**
- [ ] All RouteNames constants defined for existing pages
- [ ] NavMap includes all current menu items
- [ ] TagHelpers registered in _ViewImports
- [ ] No breaking changes to existing pages
- [ ] Documentation updated (README with new conventions)

---

### **PR1: Shared Layouts & Partials** (Week 1-2)

**Scope:** Consolidate layouts and enhance shared partials

**Files Added/Modified:**
- ✅ `Shared/_Layouts/SubActionLayout.cshtml` (consolidated from feature folders)
- ✅ `Shared/_Partials/_AlertMessage.cshtml` (new)
- ✅ `Shared/_Partials/_Breadcrumbs.cshtml` (new)
- ✅ `Shared/_Partials/_ActionBar.cshtml` (move from ContentTypes, generalize)
- ✅ `Shared/_Partials/_ConfirmDialog.cshtml` (new)
- ✅ Delete old `FlashMessage.cshtml`, replace usage with `<alert />` tag helper

**Files Deleted:**
- `Admins/SubActionLayout.cshtml`
- `ContentItems/SubActionLayout.cshtml`
- `EmailTemplates/SubActionLayout.cshtml`
- `NavigationMenus/SubActionLayout.cshtml`

**Refactor Pages:**
- Update all pages using old SubActionLayout to use new shared version

**Risks:** Medium - changes layout references

**Testing:**
- Manual test: Navigate to detail pages in each affected feature
- Verify back navigation works
- Verify alerts display correctly

**Checklist:**
- [ ] All old SubActionLayout files deleted
- [ ] All pages using SubActionLayout updated to shared version
- [ ] Alert TagHelper works on all pages
- [ ] No visual regressions

---

### **PR2: Sidebar Navigation ViewComponent** (Week 2)

**Scope:** Extract sidebar from layout into ViewComponent using NavMap

**Files Added/Modified:**
- ✅ `Shared/Components/Sidebar/SidebarViewComponent.cs` (new)
- ✅ `Shared/Components/Sidebar/Default.cshtml` (new)
- ✅ `Shared/_Layouts/SidebarLayout.cshtml` - Replace inline nav with `<vc:sidebar />`
- ✅ Remove `@functions` from SidebarLayout (moved to ViewComponent)

**Risks:** Medium - changes navigation rendering

**Testing:**
- Manual test: All menu items visible and clickable
- Active state highlights correctly
- Permission-based visibility works
- Dynamic content types appear

**Checklist:**
- [ ] All nav items from NavMap render correctly
- [ ] Active state works for all menu items
- [ ] Permissions respected (items hidden for unauthorized users)
- [ ] Content types dynamically load
- [ ] Submenu expand/collapse works
- [ ] No console errors

---

### **PR3-4: Dashboard & Login** (Week 2)

**Scope:** Refactor Dashboard and Login pages (low-risk, simple pages)

**PR3: Dashboard**
- Update `Dashboard/Index.cshtml` to use new layout/partials
- Set breadcrumbs: `["Dashboard"]`
- Use `<alert />` instead of inline checks
- Update any hardcoded paths (if any)

**PR4: Login**
- Update all Login pages (`LoginWithEmailAndPassword`, `ForgotPassword`, etc.)
- These use `EmptyLayout` ✅
- Minimal changes (already isolated)

**Risks:** Low

**Testing:**
- Login flow end-to-end
- Dashboard displays correctly

---

### **PR5: Users Feature** (Week 2-3)

**Scope:** Full refactor of Users folder (template for other features)

**Files Modified:**
- `Users/Index.cshtml` - Add breadcrumbs, use new partials
- `Users/Create.cshtml` - Use `SubActionLayout`, set back link
- `Users/Edit.cshtml` - Same
- `Users/Delete.cshtml` - Use `_ConfirmDialog.cshtml`
- `Users/Suspend.cshtml`, `Restore.cshtml`, `ResetPassword.cshtml` - SubActionLayout
- `Users/Index.cshtml.cs` - Set breadcrumbs in PageModel

**Pattern Established:**
```csharp
// Index
public void OnGet()
{
    ViewData["ActiveMenu"] = "Users";
    SetBreadcrumbs(
        new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
        new BreadcrumbNode { Label = "Users", IsActive = true }
    );
}

// Edit
public void OnGet(Guid id)
{
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
    Layout = "SubActionLayout";
    SetBreadcrumbs(
        new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
        new BreadcrumbNode { Label = "Users", RouteName = RouteNames.Users.Index },
        new BreadcrumbNode { Label = "Edit User", IsActive = true }
    );
}
```

**Risks:** Low-Medium

**Testing:**
- CRUD operations for Users
- Breadcrumbs display correctly
- Active menu state correct
- Back navigation works

**Checklist:**
- [ ] All hardcoded routes replaced with RouteNames
- [ ] Breadcrumbs on all pages
- [ ] Active state correct
- [ ] No data-controller/data-action attributes
- [ ] Validation works
- [ ] Alerts display correctly

---

### **PR6-13: Remaining Features** (Week 3-5)

**One PR per feature folder, following Users pattern:**

- **PR6:** Admins
- **PR7:** Roles
- **PR8:** UserGroups
- **PR9:** EmailTemplates
- **PR10:** NavigationMenus (including MenuItems subfolder)
- **PR11:** AuthenticationSchemes
- **PR12:** Configuration + Smtp
- **PR13:** AuditLogs

**Each PR:**
- Same structure as Users PR
- Update layouts, breadcrumbs, routing
- Remove Stimulus/Turbo attributes
- Use shared partials
- Set active menu state

---

### **PR14: ContentTypes & ContentItems** (Week 4-5)

**Scope:** Refactor largest features (already partially done ✅)

**ContentTypes:**
- ✅ Already has good structure with `_Partials/`
- Enhance breadcrumbs
- Verify `_ContentTypeNavTabs.cshtml` uses RouteNames ✅
- Add to Fields and Views subfolders

**ContentItems:**
- Complex due to dynamic fields
- Keep existing structure
- Add breadcrumbs
- Remove any Stimulus usage (file upload, WYSIWYG)
- Replace WYSIWYG Stimulus controller with vanilla JS or keep existing if non-Stimulus

**Risks:** High - most complex features

**Testing:**
- Full CRUD for content types
- Full CRUD for content items
- Field management
- View management
- File uploads
- WYSIWYG editor
- CSV import/export

---

### **PR15: Themes & RaythaFunctions** (Week 5)

**Scope:** Refactor Themes and RaythaFunctions

**Themes:**
- Multiple subfolders (Templates, Assets, etc.)
- Remove Stimulus file upload controller
- Use shared partials

**RaythaFunctions:**
- Code editor (remove Stimulus, keep vanilla code highlighting)
- CRUD operations

**Risks:** Medium

**Testing:**
- Theme creation/editing
- Template management
- Asset uploads
- Function creation/testing

---

### **PR16: Remove Stimulus JavaScript** (Week 5-6)

**Scope:** Delete all Stimulus files and dependencies

**Files Deleted:**
- `wwwroot/raytha_admin/js/dist/controllers/` (entire folder)
- `wwwroot/raytha_admin/js/dist/types/controllers/` (entire folder)
- Stimulus imports from bundler config

**Files Modified:**
- `package.json` - Remove @hotwired/stimulus, @hotwired/turbo
- Bundler config - Remove Stimulus imports
- `_Layout.cshtml` - Remove any Stimulus script tags (if present)

**Rebuild:**
- Run bundler to generate new JS without Stimulus

**Risks:** Low (dead code removal)

**Testing:**
- All features still work without Stimulus
- No console errors
- File uploads work
- Sortable lists work (if using replacement)

**Checklist:**
- [ ] No Stimulus files remaining
- [ ] No Stimulus in package.json
- [ ] `scripts/check-stimulus.sh` passes
- [ ] All interactive features tested

---

### **PR17: Final Cleanup & Documentation** (Week 6)

**Scope:** Final polish, documentation, unused code removal

**Tasks:**
- Remove any unused partials/models
- Consolidate duplicate CSS
- Update README with new conventions
- Create developer guide for adding new features
- Update `.github/CONTRIBUTING.md`

**Documentation:**
- "Adding a New Feature" guide
- "Navigation & Routing" guide
- "Layouts & Partials" reference

**Checklist:**
- [ ] All PRs merged
- [ ] CI checks passing
- [ ] No hardcoded routes (grep check)
- [ ] No Stimulus remnants (grep check)
- [ ] Documentation complete
- [ ] Performance benchmarks (compare before/after)

---

### 12.2 PR Template

**Title:** `refactor(pages): [Feature] - Adopt shared layouts and routing`

**Description:**

```markdown
## Summary
Refactors the [Feature] folder to use shared layouts, centralized routing, and eliminate Hotwire/Stimulus.

## Changes
- ✅ Replaced hardcoded routes with RouteNames constants
- ✅ Added breadcrumbs to all pages
- ✅ Migrated to shared SubActionLayout
- ✅ Removed data-controller/data-action attributes
- ✅ Updated active menu state handling
- ✅ Used shared partials for alerts, empty states, validation

## Testing
- [ ] CRUD operations tested
- [ ] Breadcrumbs display correctly
- [ ] Active menu state correct
- [ ] Back navigation works
- [ ] No console errors
- [ ] Alerts display correctly

## Risk Assessment
**Low/Medium/High:** [Rating]

**Rollback Plan:** Revert single commit

## Affected Areas
- Pages/[Feature]/*
- Shared partials (if any changes)

## Screenshots
[If UI changes]

## Checklist
- [ ] Tests added/updated
- [ ] DI registrations updated (if needed)
- [ ] Nullability satisfied
- [ ] CancellationToken plumbed (if new async methods)
- [ ] Logging + telemetry added (if significant new logic)
- [ ] Migrations included (N/A for this PR)
- [ ] Docs/README updated (if new patterns)
- [ ] No hardcoded routes (verified with grep)
- [ ] No Stimulus attributes (verified with grep)
```

---

## 13. Acceptance Criteria

### 13.1 Must Have (Non-Negotiable)

- ✅ **Zero hardcoded routes:** All links use `asp-page="@RouteNames.X.Y"` or TagHelpers
- ✅ **No Stimulus/Turbo:** Zero `data-controller`, `data-action`, `data-turbo-*` attributes
- ✅ **Breadcrumbs everywhere:** All pages show breadcrumb trail
- ✅ **Active state correct:** Current menu item highlighted on every page
- ✅ **Shared layouts:** All features use `SidebarLayout` or `SubActionLayout` (no inline duplicates)
- ✅ **CI checks pass:** Routing, Stimulus, and view compilation checks green

### 13.2 Should Have (High Priority)

- ✅ **Duplication reduced ≥70%:** Measured by lines of repeated markup
- ✅ **Consistent patterns:** All CRUD pages follow same structure
- ✅ **Accessible:** WCAG 2.1 AA compliance (nav, forms, tables)
- ✅ **Fast:** No performance regressions (measure page load times)
- ✅ **Documented:** Developer guide for adding new features

### 13.3 Nice to Have (Enhancements)

- ✅ **TypeScript for remaining JS:** Vanilla JS converted to TS for type safety
- ✅ **Output caching:** Strategic caching on read-heavy pages
- ✅ **ViewComponent tests:** Integration tests for Sidebar, Toolbar
- ✅ **Automated accessibility tests:** axe-core integration

---

## 14. Coding Standards

### 14.1 Razor Pages

**DO:**
- ✅ Keep logic minimal in `.cshtml` files
- ✅ Use TagHelpers for conditional logic
- ✅ Prefer ViewComponents for data fetching
- ✅ Use partials for pure markup reuse
- ✅ Set `ViewData["ActiveMenu"]` in every PageModel
- ✅ Use `RouteNames` constants for all navigation
- ✅ Add breadcrumbs to every page (via `SetBreadcrumbs()` or auto-generate)

**DON'T:**
- ❌ Inline Razor `@if`/`@foreach` logic (extract to partials/components)
- ❌ Hardcode paths (`href="/users"`)
- ❌ Duplicate markup across pages
- ❌ Use ViewBag (use strongly-typed ViewData or models)

### 14.2 PageModels

**DO:**
- ✅ Inherit from appropriate base class (`BaseAdminPageModel`, `BaseContentTypeContextPageModel`, etc.)
- ✅ Use `async` for all I/O operations
- ✅ Plumb `CancellationToken` through
- ✅ Return `IActionResult` from handlers
- ✅ Use `RedirectToPage(RouteNames.X.Y, new { ... })` for redirects
- ✅ Set alerts via `SetSuccessMessage()`, `SetErrorMessage()`
- ✅ Log significant operations

**DON'T:**
- ❌ Put business logic in PageModels (belongs in Application layer via MediatR)
- ❌ Access database directly (use MediatR commands/queries)
- ❌ Block on async (`.Result`, `.Wait()`)
- ❌ Swallow exceptions

### 14.3 Models

**DO:**
- ✅ Use `record` for immutable models (DTOs, view models)
- ✅ Enable nullable reference types
- ✅ Use `required` for mandatory properties
- ✅ Add `[BindProperty]` only when necessary
- ✅ Separate input models from view models

**DON'T:**
- ❌ Expose domain entities directly in views
- ❌ Use `dynamic`
- ❌ Mutate shared view models

### 14.4 Naming

- **Layouts:** `[Name]Layout.cshtml` (e.g., `SidebarLayout`)
- **Partials:** `_[Name].cshtml` (e.g., `_AlertMessage`)
- **ViewComponents:** `[Name]ViewComponent.cs` + `Default.cshtml`
- **TagHelpers:** `[Name]TagHelper.cs` (e.g., `BreadcrumbsTagHelper`)
- **Models:** `[Name]Model.cs` or `[Name]ViewModel.cs`
- **Route constants:** `RouteNames.[Feature].[Page]`

### 14.5 Comments

- ✅ XML doc comments on all public APIs
- ✅ Inline comments for non-obvious logic
- ✅ TODO comments for follow-up work: `// TODO(zack): Consider caching this`

---

## Appendix A: File Count Estimates

| Category | Before | After | Delta |
|----------|--------|-------|-------|
| Layout files | 6 | 5 | -1 (consolidated SubActionLayout) |
| Shared partials | 12 | 16 | +4 (new alerts, breadcrumbs, confirm dialog, table shell) |
| ViewComponents | 0 | 3 | +3 (Sidebar, Toolbar, UserMenu) |
| TagHelpers | 2 | 6 | +4 (Breadcrumbs, Alert, ConfirmAction, RouteLink) |
| Stimulus controllers (JS) | ~30 files | 0 | -30 |
| Feature pages (.cshtml) | ~150 | ~150 | 0 (refactored, not deleted) |

**Net:** ~10% fewer files, ~70% less duplicated markup

---

## Appendix B: Performance Baseline

**Measure before refactor:**

| Page | Load Time (ms) | DB Queries | Memory (MB) |
|------|----------------|------------|-------------|
| Dashboard | TBD | TBD | TBD |
| Users Index | TBD | TBD | TBD |
| ContentTypes Index | TBD | TBD | TBD |
| ContentItems Edit | TBD | TBD | TBD |

**Target:** No regressions (±5%)

---

## Appendix C: Grep Cheat Sheet

```bash
# Find hardcoded routes
grep -r 'href="/' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages
grep -r 'asp-page="/[^@]' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages

# Find Stimulus attributes
grep -r 'data-controller\|data-action\|data-turbo' src/Raytha.Web --include="*.cshtml"

# Find inline ViewData checks (should use TagHelpers)
grep -r 'ViewData\["ErrorMessage"\]' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages

# Find missing breadcrumbs
grep -L 'SetBreadcrumbs\|<breadcrumbs' src/Raytha.Web/Areas/Admin/Pages/**/*.cshtml.cs
```

---

## Appendix D: Quick Reference

### Common Patterns

**Set active menu & breadcrumbs:**
```csharp
public void OnGet()
{
    ViewData["ActiveMenu"] = "Users";
    SetBreadcrumbs(
        new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
        new BreadcrumbNode { Label = "Users", IsActive = true }
    );
}
```

**Use SubActionLayout:**
```csharp
@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Edit User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
}
```

**Render alerts:**
```html
<alert />
```

**Render empty state:**
```html
@if (!Model.ListView.Items.Any())
{
    @await Html.PartialAsync("_Partials/_EmptyState", new EmptyStateModel { ... })
}
```

**Render table with pagination:**
```html
<table>...</table>
@await Html.PartialAsync("_Partials/TablePagination", Model.ListView)
```

---

**END OF REFACTOR PLAN**

---

**Next Steps:**
1. Review and approve this plan
2. Create GitHub Project board with PR tracking
3. Begin PR0 (Foundation & Scaffolding)
4. Establish code review process (require 1 approval + CI pass)
5. Schedule weekly refactor sync meetings

**Questions/Clarifications:**
- Icon library: Use existing SVG icons or adopt icon font (Font Awesome, Heroicons)?
- WYSIWYG editor: Keep existing implementation or replace with vanilla alternative?
- Automated testing: What level of coverage is expected for UI tests?

**Estimated Timeline:** 4-6 weeks with 1 developer full-time, or 8-12 weeks with 0.5 FTE

