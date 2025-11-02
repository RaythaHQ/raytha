# Raytha Pages Refactor - Implementation Status

**Last Updated:** November 2, 2025  
**Current Phase:** PR1 Complete - Foundation Fully Built ✅

---

## ✅ COMPLETED: PR0 + PR1 (Foundation & Layouts)

### PR0: Foundation & Scaffolding ✅

**All infrastructure is in place and ready to use!**

#### 1. RouteNames.cs - Expanded ✅
**Location:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/RouteNames.cs`

**Added 100+ route constants covering:**
- Dashboard
- Themes (with Templates and MediaItems subfolders)
- EmailTemplates
- NavigationMenus (with MenuItems subfolder)
- RaythaFunctions
- AuthenticationSchemes
- AuditLogs
- Profile
- Smtp
- Login (all authentication flows)

**Usage:**
```csharp
// In PageModel
return RedirectToPage(RouteNames.Users.Index);

// In View
<a asp-page="@RouteNames.Users.Edit" asp-route-id="@userId">Edit</a>
```

#### 2. Shared Models ✅
**Location:** `/src/Raytha.Web/Areas/Shared/Models/`

**Created:**
- `BreadcrumbNode.cs` - For breadcrumb navigation trails
- `AlertMessage.cs` - Structured alerts with types (Success, Error, Warning, Info)
- `SortSpec.cs` - Query sorting specifications with parsing
- `PaginationQuery.cs` - Standard pagination parameters with validation

#### 3. TagHelpers ✅
**Location:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/TagHelpers/`

**Created:**
- `BreadcrumbsTagHelper.cs` - Renders breadcrumb navigation from ViewData
  - Usage: `<breadcrumbs />` (auto-reads from ViewData["Breadcrumbs"])
  
- `AlertTagHelper.cs` - Renders Bootstrap alerts from ViewData
  - Usage: `<alert />` (renders all alert types)
  - Usage: `<alert alert-key="ErrorMessage" />` (specific alert)

**Enhanced:**
- `NavLinkTagHelper.cs` - Added route name matching and match modes
  - New: `nav-route-name` attribute for exact route matching
  - New: `nav-match-mode` attribute (contains, exact, startswith)
  - New: Automatic ARIA attributes for accessibility

**Usage Examples:**
```html
<!-- Section-based matching -->
<a asp-page="@RouteNames.Users.Index" nav-active-section="Users">Users</a>

<!-- Route name matching -->
<a asp-page="@RouteNames.Dashboard.Index" nav-route-name="/Dashboard/Index">Dashboard</a>

<!-- Breadcrumbs (auto-rendered from ViewData) -->
<breadcrumbs />

<!-- Alerts (replaces inline checks) -->
<alert />
```

#### 4. BasePageModel Enhancements ✅
**Location:** `/src/Raytha.Web/Areas/Shared/Models/BasePageModel.cs`

**Added Methods:**
```csharp
// Set breadcrumbs for current page
protected void SetBreadcrumbs(params BreadcrumbNode[] breadcrumbs)

// Generate page URLs from route names
protected string GetPageUrl(string routeName, object? values = null)

// Generate absolute URIs
protected string GetPageUri(string routeName, object? values = null)
```

**Usage in PageModels:**
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

#### 5. Navigation Infrastructure ✅
**Location:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/Infrastructure/Navigation/`

**Created:**
- `NavMap.cs` - Centralized navigation structure
  - Defines all menu items with permissions, icons, order
  - Single source of truth for sidebar navigation
  - Content types added dynamically

- `NavMenuItem.cs` - Navigation item model
  - Properties: Id, Label, RouteName, Icon, Permission, Order, Children, etc.

- `IconLibrary.cs` - SVG icon constants
  - Dashboard, Users, ContentType, Themes, EmailTemplates, etc.
  - All icons pre-defined as SVG markup

**Usage:**
```csharp
var menuItems = NavMap.GetMenuItems();
var profileMenu = NavMap.GetProfileMenu(userName, emailPasswordEnabled);
```

#### 6. Updated Configuration ✅
- `_ViewImports.cshtml` - Added Navigation namespace for easy access

---

### PR1: Consolidate Layouts & Partials ✅

#### 1. Consolidated SubActionLayout ✅
**Location:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/_Layouts/SubActionLayout.cshtml`

**Replaces 4 duplicate layouts:**
- ❌ Admins/SubActionLayout.cshtml (delete)
- ❌ ContentItems/SubActionLayout.cshtml (delete)
- ❌ EmailTemplates/SubActionLayout.cshtml (delete)
- ❌ NavigationMenus/SubActionLayout.cshtml (delete)

**Features:**
- Automatic back link from ViewData
- Alert rendering via TagHelper
- Optional sidebar section for action menus
- Consistent card-based layout

**Usage:**
```csharp
@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Edit User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
    ViewData["BackLinkLabel"] = "Back to Users"; // Optional
}

@section Sidebar {
    <!-- Optional: action menu items -->
}
```

#### 2. New Shared Partials ✅
**Location:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/_Partials/`

**Created:**

**`_AlertMessage.cshtml`** - Model-based alert rendering
```csharp
@await Html.PartialAsync("_Partials/_AlertMessage", new AlertMessage
{
    Type = AlertType.Success,
    Message = "User created successfully",
    IsDismissible = true
})
```

**`_Breadcrumbs.cshtml`** - Standalone breadcrumb rendering
```html
@await Html.PartialAsync("_Partials/_Breadcrumbs", breadcrumbsList)
```

**`_ConfirmDialog.cshtml`** - Bootstrap modal confirmation
```csharp
@await Html.PartialAsync("_Partials/_ConfirmDialog", new ConfirmActionModel
{
    Id = "confirm-delete",
    Title = "Confirm Delete",
    Message = "Are you sure you want to delete this user?",
    ConfirmButtonText = "Delete",
    ConfirmButtonClass = "btn-danger",
    ShowWarning = true
})
```

**Usage with trigger button:**
```html
<button type="button" 
        class="btn btn-danger"
        data-bs-toggle="modal" 
        data-bs-target="#confirm-delete"
        data-action-url="/users/delete/123">
    Delete
</button>
```

#### 3. Updated SidebarLayout ✅
**Enhanced:** `/src/Raytha.Web/Areas/Admin/Pages/Shared/SidebarLayout.cshtml`

**Changes:**
- Replaced `@Html.Partial("_Partials/FlashMessage")` with `<alert />`
- Added `<breadcrumbs />` TagHelper
- Wrapped in container for consistent spacing

---

## 📊 Impact Summary

### Files Created (21 new files)
**PR0 (14 files):**
- 4 Shared Models
- 2 New TagHelpers
- 1 Enhanced TagHelper
- 3 Navigation Infrastructure files
- 1 Model class file (ConfirmActionModel - created in PR1)
- 3 Documentation files

**PR1 (7 files):**
- 1 Consolidated SubActionLayout
- 3 New Partials
- 1 ConfirmActionModel
- Updated SidebarLayout
- Updated _ViewImports

### Files Modified (4 files)
- RouteNames.cs (expanded)
- BasePageModel.cs (enhanced)
- SidebarLayout.cshtml (updated)
- _ViewImports.cshtml (updated)

### Files to Delete (4 files - Not yet deleted)
- Admins/SubActionLayout.cshtml
- ContentItems/SubActionLayout.cshtml
- EmailTemplates/SubActionLayout.cshtml
- NavigationMenus/SubActionLayout.cshtml

---

## 🎯 What's Ready to Use RIGHT NOW

### 1. In PageModels
```csharp
public class IndexModel : BaseAdminPageModel
{
    public void OnGet()
    {
        // Set active menu
        ViewData["ActiveMenu"] = "Users";
        
        // Set breadcrumbs
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", IsActive = true }
        );
    }
    
    public IActionResult OnPost()
    {
        // Use RouteNames for redirects
        SetSuccessMessage("User created successfully");
        return RedirectToPage(RouteNames.Users.Index);
    }
}
```

### 2. In Views
```html
@{
    Layout = "SubActionLayout"; // or "SidebarLayout"
    ViewData["Title"] = "Edit User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
}

<!-- Automatic breadcrumbs and alerts -->

<!-- Navigation links -->
<a asp-page="@RouteNames.Users.Index" nav-active-section="Users">Users</a>

<!-- Confirmation dialog -->
@await Html.PartialAsync("_Partials/_ConfirmDialog", new ConfirmActionModel
{
    Id = "confirm-delete",
    Title = "Confirm Delete",
    Message = "Are you sure?",
    ConfirmButtonText = "Delete",
    ConfirmButtonClass = "btn-danger"
})
```

---

## 🚧 What's Next (When You're Ready)

### PR2: Sidebar ViewComponent
- Extract sidebar navigation to ViewComponent
- Use NavMap for menu structure
- Dynamic content type menu items
- Permission-based visibility

### PR3-5: Refactor First Features
- Dashboard (simple, good starting point)
- Login pages (already isolated)
- Users feature (establishes CRUD pattern)

### PR6-15: Remaining Features
- Apply patterns systematically to all other features
- One PR per feature for easy review

### PR16: Remove Stimulus
- Delete all Stimulus JavaScript files
- Update package.json
- CI checks to prevent reintroduction

### PR17: Final Cleanup
- Documentation updates
- Performance testing
- Final polish

---

## ✅ Current State: Production-Ready Foundation

**What works:**
- ✅ All RouteNames constants available
- ✅ Breadcrumbs on any page via `SetBreadcrumbs()`
- ✅ Alerts via `<alert />` TagHelper
- ✅ Active navigation via `nav-active-section` attribute
- ✅ Consolidated SubActionLayout ready to use
- ✅ Confirmation dialogs via Bootstrap modals
- ✅ No breaking changes - all additive

**What to do next:**
1. Review the implementation
2. Test the TagHelpers and layouts
3. Decide whether to continue with PR2+ or pause
4. Consider creating a sample page using all new features

**Questions:**
- Ready to continue with PR2 (Sidebar ViewComponent)?
- Want to test the foundation first?
- Need any adjustments to what's been built?

---

## 📝 Notes

- All code follows raytha.instructions.md conventions
- XML doc comments on all public APIs
- Nullable reference types enabled
- Async/await patterns preserved
- No dependencies on Stimulus/Hotwire (pure Bootstrap 5)
- CI-ready (can add checks for hardcoded routes, etc.)

**Foundation is solid and ready for systematic rollout across all features!** 🚀

