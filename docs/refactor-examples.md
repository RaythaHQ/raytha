# Raytha Pages Refactor - Code Examples

**Quick reference for common refactoring patterns**

---

## Table of Contents

1. [Index Page (List)](#1-index-page-list)
2. [Create/Edit Page (Form)](#2-createedit-page-form)
3. [Delete Page (Confirmation)](#3-delete-page-confirmation)
4. [Feature with Sub-Navigation](#4-feature-with-sub-navigation)
5. [Adding New Feature](#5-adding-new-feature)

---

## 1. Index Page (List)

### Before

**`Users/Index.cshtml`**
```html
@page "/raytha/users"
@model Raytha.Web.Areas.Admin.Pages.Users.Index

@{
    Layout = "UsersAndUserGroups"; // Feature-specific layout
    ViewData["Title"] = "Users";
    ViewData["ActiveMenu"] = "Users";
}

@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}
@if (ViewData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@ViewData["SuccessMessage"]</div>
}

<!-- Search bar and create button -->
<div class="d-flex justify-content-between mb-3">
    <form method="get">
        <input type="search" name="search" value="@Model.ListView.Search">
        <button type="submit">Search</button>
    </form>
    <a href="/users/create" class="btn btn-primary">Create User</a>
</div>

<!-- Table -->
<table class="table">
    <thead>
        <tr>
            <th>
                <a href="?orderBy=FirstName%20asc">First Name</a>
            </th>
            <!-- More headers... -->
        </tr>
    </thead>
    <tbody>
        @foreach (var user in Model.ListView.Items)
        {
            <tr>
                <td>@user.FirstName</td>
                <td>
                    <a href="/users/@user.Id/edit">Edit</a>
                    <a href="/users/@user.Id/delete">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>

<!-- Pagination -->
@if (Model.ListView.TotalPages > 1)
{
    <!-- 50 lines of pagination HTML... -->
}
```

**`Users/Index.cshtml.cs`**
```csharp
public class Index : BaseAdminPageModel, IHasListView<UsersListItemViewModel>
{
    public ListViewModel<UsersListItemViewModel> ListView { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        // Query logic...
        ListView = await Mediator.Send(new GetUsersQuery
        {
            Search = ListView.Search,
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
            OrderBy = ListView.OrderByAsString
        });
    }
}
```

### After

**`Users/Index.cshtml`**
```html
@page "/raytha/users"
@model Raytha.Web.Areas.Admin.Pages.Users.Index

@{
    Layout = "SidebarLayout";
    ViewData["Title"] = "Users";
    ViewData["ActiveMenu"] = "Users";
}

<!-- Alerts (replaces inline checks) -->
<alert />

<!-- Breadcrumbs (auto-rendered by SidebarLayout) -->

<!-- Search & Create Bar (shared partial) -->
@await Html.PartialAsync("_Partials/TableCreateAndSearchBar", new BaseTableCreateAndSearchBarViewModel
{
    Pagination = Model.ListView,
    EntityName = "user",
    CreateActionName = RouteNames.Users.Create
})

<!-- Empty State OR Table -->
@if (!Model.ListView.Items.Any())
{
    @await Html.PartialAsync("_Partials/_EmptyState", new EmptyStateModel
    {
        Icon = IconLibrary.Users,
        Title = "No users found",
        Message = Model.ListView.Search != null 
            ? $"No users match \"{Model.ListView.Search}\"" 
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
    <div class="raytha-data-card mb-4">
        <div class="card-body">
            <div class="table-responsive">
                <table class="table table-centered table-nowrap mb-0 rounded">
                    <thead class="thead-light">
                        <tr>
                            @await Html.PartialAsync("_Partials/TableColumnHeader", new BaseTableColumnHeaderViewModel
                            {
                                Pagination = Model.ListView,
                                PropertyName = "FirstName",
                                IsFirst = true,
                                DisplayName = "First Name"
                            })
                            @await Html.PartialAsync("_Partials/TableColumnHeader", new BaseTableColumnHeaderViewModel
                            {
                                Pagination = Model.ListView,
                                PropertyName = "LastName",
                                DisplayName = "Last Name"
                            })
                            <th scope="col" class="border-0 rounded-end">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var user in Model.ListView.Items)
                        {
                            <tr>
                                <td>@user.FirstName</td>
                                <td>@user.LastName</td>
                                <td>
                                    <a asp-page="@RouteNames.Users.Edit" 
                                       asp-route-id="@user.Id">
                                        Edit
                                    </a>
                                    <a asp-page="@RouteNames.Users.Delete" 
                                       asp-route-id="@user.Id">
                                        Delete
                                    </a>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    </div>
    
    <!-- Pagination (shared partial) -->
    @await Html.PartialAsync("_Partials/TablePagination", Model.ListView)
}
```

**`Users/Index.cshtml.cs`**
```csharp
public class Index : BaseAdminPageModel, IHasListView<UsersListItemViewModel>
{
    public ListViewModel<UsersListItemViewModel> ListView { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        // Set breadcrumbs
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", IsActive = true }
        );
        
        // Query logic (unchanged)
        ListView = await Mediator.Send(new GetUsersQuery
        {
            Search = ListView.Search,
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
            OrderBy = ListView.OrderByAsString
        });
    }
}
```

**Key Changes:**
- ✅ Layout: `UsersAndUserGroups` → `SidebarLayout` (standard)
- ✅ Alerts: Inline checks → `<alert />` TagHelper
- ✅ Breadcrumbs: Added `SetBreadcrumbs()`
- ✅ Empty state: Added shared partial
- ✅ Routes: Hardcoded paths → `RouteNames.Users.X`
- ✅ Pagination: Uses shared partial (already existed)
- ✅ Table headers: Uses shared partial with sorting

---

## 2. Create/Edit Page (Form)

### Before

**`Users/Create.cshtml`**
```html
@page "/raytha/users/create"
@model Raytha.Web.Areas.Admin.Pages.Users.Create

@{
    ViewData["Title"] = "Create User";
    ViewData["ActiveMenu"] = "Users";
}

<a href="/users">← Back to Users</a>

<h2>@ViewData["Title"]</h2>

@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}

<form method="post">
    <div class="mb-3">
        <label for="FirstName">First Name</label>
        <input type="text" 
               class="form-control @Model.HasError("FirstName")" 
               asp-for="Form.FirstName">
        @if (Model.ErrorMessageFor("FirstName") != null)
        {
            <div class="invalid-feedback">@Model.ErrorMessageFor("FirstName")</div>
        }
    </div>
    
    <div class="mb-3">
        <label for="LastName">Last Name</label>
        <input type="text" 
               class="form-control @Model.HasError("LastName")" 
               asp-for="Form.LastName">
        @if (Model.ErrorMessageFor("LastName") != null)
        {
            <div class="invalid-feedback">@Model.ErrorMessageFor("LastName")</div>
        }
    </div>
    
    <div class="mb-3">
        <label for="Email">Email</label>
        <input type="email" 
               class="form-control @Model.HasError("EmailAddress")" 
               asp-for="Form.EmailAddress">
        @if (Model.ErrorMessageFor("EmailAddress") != null)
        {
            <div class="invalid-feedback">@Model.ErrorMessageFor("EmailAddress")</div>
        }
    </div>
    
    <button type="submit" class="btn btn-primary">Create</button>
    <a href="/users" class="btn btn-secondary">Cancel</a>
</form>
```

**`Users/Create.cshtml.cs`**
```csharp
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public CreateUserDto Form { get; set; } = new();
    
    public void OnGet()
    {
        // Just render form
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        var result = await Mediator.Send(new CreateUserCommand
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress
        });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Errors);
            return Page();
        }
        
        SetSuccessMessage("User created successfully");
        return RedirectToPage("/Users/Index");
    }
}
```

### After

**`Users/Create.cshtml`**
```html
@page "/raytha/users/create"
@model Raytha.Web.Areas.Admin.Pages.Users.Create

@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Create User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
    ViewData["BackLinkLabel"] = "Back to Users";
}

<!-- Alert rendered by SubActionLayout -->
<!-- Breadcrumbs rendered by SubActionLayout -->
<!-- Back link rendered by SubActionLayout -->

<div class="row">
    <div class="col-xxl-7 col-xl-8 col-lg-9 col-md-12">
        <div class="card border-0 shadow mb-4">
            <div class="card-body">
                <form asp-page="@RouteNames.Users.Create" method="post" class="py-4" novalidate>
                    
                    <div class="mb-3">
                        <label class="form-label raytha-required" asp-for="Form.FirstName"></label>
                        <input type="text" 
                               class="form-control @Model.HasError(nameof(Model.Form.FirstName))" 
                               asp-for="Form.FirstName"
                               required>
                        <div class="invalid-feedback d-block">
                            @Model.ErrorMessageFor(nameof(Model.Form.FirstName))
                        </div>
                    </div>
                    
                    <div class="mb-3">
                        <label class="form-label raytha-required" asp-for="Form.LastName"></label>
                        <input type="text" 
                               class="form-control @Model.HasError(nameof(Model.Form.LastName))" 
                               asp-for="Form.LastName"
                               required>
                        <div class="invalid-feedback d-block">
                            @Model.ErrorMessageFor(nameof(Model.Form.LastName))
                        </div>
                    </div>
                    
                    <div class="mb-3">
                        <label class="form-label raytha-required" asp-for="Form.EmailAddress"></label>
                        <input type="email" 
                               class="form-control @Model.HasError(nameof(Model.Form.EmailAddress))" 
                               asp-for="Form.EmailAddress"
                               required>
                        <div class="invalid-feedback d-block">
                            @Model.ErrorMessageFor(nameof(Model.Form.EmailAddress))
                        </div>
                        <div class="form-text">
                            User will receive a verification email at this address.
                        </div>
                    </div>
                    
                    <div class="mt-4">
                        <button type="submit" class="btn btn-primary">Create User</button>
                        <a asp-page="@RouteNames.Users.Index" class="btn btn-secondary">Cancel</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
```

**`Users/Create.cshtml.cs`**
```csharp
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public CreateUserDto Form { get; set; } = new();
    
    public void OnGet()
    {
        // Set breadcrumbs
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", RouteName = RouteNames.Users.Index },
            new BreadcrumbNode { Label = "Create User", IsActive = true }
        );
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        var result = await Mediator.Send(new CreateUserCommand
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress
        });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Errors);
            return Page();
        }
        
        SetSuccessMessage("User created successfully");
        return RedirectToPage(RouteNames.Users.Index);
    }
}
```

**Key Changes:**
- ✅ Layout: Manual back link → `SubActionLayout` (handles back link automatically)
- ✅ Breadcrumbs: Added in `OnGet()`
- ✅ Form action: Uses `asp-page="@RouteNames.Users.Create"`
- ✅ Cancel link: Hardcoded → `RouteNames.Users.Index`
- ✅ Redirect: Hardcoded → `RouteNames.Users.Index`
- ✅ Help text: Added form-text for UX

**Edit Page** - Same pattern, just populate `Form` in `OnGet(Guid id)`

---

## 3. Delete Page (Confirmation)

### Before

**`Users/Delete.cshtml`**
```html
@page "/raytha/users/{id:guid}/delete"
@model Raytha.Web.Areas.Admin.Pages.Users.Delete

@{
    ViewData["Title"] = "Delete User";
    ViewData["ActiveMenu"] = "Users";
}

<a href="/users">← Back to Users</a>

<h2>@ViewData["Title"]</h2>

@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}

<div class="alert alert-warning">
    <strong>Warning!</strong> This action cannot be undone.
</div>

<p>Are you sure you want to delete <strong>@Model.User.FullName</strong>?</p>

<form method="post">
    <input type="hidden" asp-for="Id">
    <button type="submit" class="btn btn-danger">Delete User</button>
    <a href="/users" class="btn btn-secondary">Cancel</a>
</form>
```

**`Users/Delete.cshtml.cs`**
```csharp
public class Delete : BaseAdminPageModel
{
    [BindProperty]
    public Guid Id { get; set; }
    
    public UserDto User { get; set; } = null!;
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Id = id;
        var result = await Mediator.Send(new GetUserByIdQuery { Id = id });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Error);
            return RedirectToPage("/Users/Index");
        }
        
        User = result.Result;
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        var result = await Mediator.Send(new DeleteUserCommand { Id = Id });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Error);
            return Page();
        }
        
        SetSuccessMessage("User deleted successfully");
        return RedirectToPage("/Users/Index");
    }
}
```

### After (Option 1: Separate Page)

**`Users/Delete.cshtml`**
```html
@page "/raytha/users/{id:guid}/delete"
@model Raytha.Web.Areas.Admin.Pages.Users.Delete

@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Delete User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
}

<div class="row">
    <div class="col-xxl-7 col-xl-8 col-lg-9 col-md-12">
        <div class="card border-0 shadow mb-4">
            <div class="card-body">
                <div class="alert alert-warning" role="alert">
                    <strong>⚠️ Warning!</strong> This action cannot be undone.
                </div>
                
                <p class="mb-4">
                    Are you sure you want to delete <strong>@Model.User.FullName</strong>?
                </p>
                
                <form asp-page="@RouteNames.Users.Delete" 
                      asp-route-id="@Model.Id" 
                      method="post">
                    <button type="submit" class="btn btn-danger">Delete User</button>
                    <a asp-page="@RouteNames.Users.Index" class="btn btn-secondary">Cancel</a>
                </form>
            </div>
        </div>
    </div>
</div>
```

**`Users/Delete.cshtml.cs`** (same logic, just update routes)
```csharp
public class Delete : BaseAdminPageModel
{
    [BindProperty]
    public Guid Id { get; set; }
    
    public UserDto User { get; set; } = null!;
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Id = id;
        
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", RouteName = RouteNames.Users.Index },
            new BreadcrumbNode { Label = "Delete User", IsActive = true }
        );
        
        var result = await Mediator.Send(new GetUserByIdQuery { Id = id });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Error);
            return RedirectToPage(RouteNames.Users.Index);
        }
        
        User = result.Result;
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        var result = await Mediator.Send(new DeleteUserCommand { Id = Id });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Error);
            return Page();
        }
        
        SetSuccessMessage($"User {User.FullName} deleted successfully");
        return RedirectToPage(RouteNames.Users.Index);
    }
}
```

### After (Option 2: Modal from Index - Preferred)

**`Users/Index.cshtml`** (updated row actions)
```html
<td>
    <a asp-page="@RouteNames.Users.Edit" asp-route-id="@user.Id">Edit</a>
    
    <!-- Delete with modal confirmation -->
    <button type="button" 
            class="btn btn-link text-danger p-0"
            data-bs-toggle="modal" 
            data-bs-target="#confirm-delete"
            data-user-id="@user.Id"
            data-user-name="@user.FullName">
        Delete
    </button>
</td>

<!-- At bottom of page -->
<div class="modal fade" id="confirm-delete" tabindex="-1" aria-labelledby="confirmDeleteLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="confirmDeleteLabel">Confirm Delete</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div class="alert alert-warning" role="alert">
                    <strong>⚠️ Warning!</strong> This action cannot be undone.
                </div>
                <p>Are you sure you want to delete <strong id="delete-user-name"></strong>?</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <form method="post" asp-page-handler="Delete" id="delete-form">
                    <input type="hidden" name="id" id="delete-user-id">
                    <button type="submit" class="btn btn-danger">Delete User</button>
                </form>
            </div>
        </div>
    </div>
</div>

<script>
    document.getElementById('confirm-delete').addEventListener('show.bs.modal', function(event) {
        var button = event.relatedTarget;
        var userId = button.getAttribute('data-user-id');
        var userName = button.getAttribute('data-user-name');
        
        document.getElementById('delete-user-id').value = userId;
        document.getElementById('delete-user-name').textContent = userName;
    });
</script>
```

**`Users/Index.cshtml.cs`** (add handler)
```csharp
public async Task<IActionResult> OnPostDeleteAsync(Guid id)
{
    var result = await Mediator.Send(new DeleteUserCommand { Id = id });
    
    if (!result.Success)
    {
        SetErrorMessage(result.Error);
    }
    else
    {
        SetSuccessMessage("User deleted successfully");
    }
    
    return RedirectToPage(RouteNames.Users.Index);
}
```

**Key Changes (Option 2):**
- ✅ No separate Delete page needed
- ✅ Bootstrap modal for confirmation
- ✅ Handler on Index page
- ✅ Better UX (no page navigation for delete)

---

## 4. Feature with Sub-Navigation

**Example: ContentTypes with tabs (Configuration, Fields, Deleted Items)**

### Partial: `ContentTypes/_Partials/_ContentTypeNavTabs.cshtml`

```html
@using Raytha.Web.Areas.Admin.Pages.Shared
@using Raytha.Web.Areas.Admin.Pages.Shared.Models
@model ContentTypeNavModel

@{
    var contentTypeDeveloperName = Model.CurrentView.ContentType.DeveloperName;
}

<ul class="nav nav-tabs mb-4" role="tablist" aria-label="Content type settings navigation">
    <li class="nav-item" role="presentation">
        <a class="nav-link" 
           nav-active-section="ContentItems"
           asp-page="@RouteNames.ContentItems.Index"
           asp-route-contentTypeDeveloperName="@contentTypeDeveloperName"
           asp-route-viewId="@Model.CurrentView.Id"
           role="tab">
            <svg class="icon icon-xs">...</svg> @Model.CurrentView.Label
        </a>
    </li>
    
    <li class="nav-item dropdown" role="presentation">
        <a class="nav-link @(Model.ActiveTab is ContentTypeNavTab.Configuration or ContentTypeNavTab.Fields or ContentTypeNavTab.DeletedItems ? "active" : "") dropdown-toggle" 
           data-bs-toggle="dropdown" 
           href="#" 
           role="button">
            <svg class="icon icon-xs">...</svg> Settings
        </a>
        <ul class="dropdown-menu">
            <li>
                <a class="dropdown-item @(Model.ActiveTab == ContentTypeNavTab.Configuration ? "active" : "")" 
                   asp-page="@RouteNames.ContentTypes.Configuration"
                   asp-route-contentTypeDeveloperName="@contentTypeDeveloperName">
                    Configuration
                </a>
            </li>
            <li>
                <a class="dropdown-item @(Model.ActiveTab == ContentTypeNavTab.Fields ? "active" : "")" 
                   asp-page="@RouteNames.ContentTypes.Fields.Index"
                   asp-route-contentTypeDeveloperName="@contentTypeDeveloperName">
                    Fields
                </a>
            </li>
            <li>
                <a class="dropdown-item @(Model.ActiveTab == ContentTypeNavTab.DeletedItems ? "active" : "")" 
                   asp-page="@RouteNames.ContentTypes.DeletedContentItemsList"
                   asp-route-contentTypeDeveloperName="@contentTypeDeveloperName">
                    Deleted @Model.CurrentView.ContentType.LabelPlural.ToLower()
                </a>
            </li>
        </ul>
    </li>
</ul>
```

### Page using sub-nav: `ContentTypes/Configuration.cshtml`

```html
@page "/raytha/{contentTypeDeveloperName}/configuration"
@model Raytha.Web.Areas.Admin.Pages.ContentTypes.Configuration

@{
    var pageTitle = $"{Model.CurrentView.ContentType.LabelPlural} > Configuration";
    Layout = "SidebarLayout";
    ViewData["Title"] = pageTitle;
    ViewData["ActiveMenu"] = Model.CurrentView.ContentType.DeveloperName;
    
    var navModel = new ContentTypeNavModel
    {
        CurrentView = Model.CurrentView,
        ActiveTab = ContentTypeNavTab.Configuration
    };
}

<!-- Render sub-navigation tabs -->
@await Html.PartialAsync("_Partials/_ContentTypeNavTabs", navModel)

<!-- Form -->
<div class="row">
    <div class="col-xxl-7 col-xl-8 col-lg-9 col-md-12">
        <div class="card border-0 shadow mb-4">
            <div class="card-body">
                <form asp-page="@RouteNames.ContentTypes.Configuration" 
                      asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentType.DeveloperName" 
                      method="post" 
                      class="py-4" 
                      novalidate>
                    <!-- Form fields -->
                </form>
            </div>
        </div>
    </div>
</div>
```

**Pattern:**
- Feature-specific nav partial: `[Feature]/_Partials/_[Feature]NavTabs.cshtml`
- Model for nav state: `ContentTypeNavModel` with `ActiveTab` enum
- All pages in feature include the nav partial
- Active tab highlighted via model property

---

## 5. Adding New Feature

**Step-by-step guide to add a hypothetical "Reports" feature**

### Step 1: Add Routes to `RouteNames.cs`

```csharp
// Add to RouteNames.cs
public static class Reports
{
    public const string Index = "/Reports/Index";
    public const string Create = "/Reports/Create";
    public const string Edit = "/Reports/Edit";
    public const string Delete = "/Reports/Delete";
    public const string Run = "/Reports/Run";
}
```

### Step 2: Add to `NavMap.cs`

```csharp
// Add to NavMap.GetMenuItems()
new NavMenuItem
{
    Id = "Reports",
    Label = "Reports",
    RouteName = RouteNames.Reports.Index,
    Icon = IconLibrary.Reports,
    Permission = BuiltInSystemPermission.MANAGE_REPORTS_PERMISSION,
    Order = 110 // After Themes, before Settings
}
```

### Step 3: Create Feature Folder

```
Pages/
└── Reports/
    ├── Index.cshtml
    ├── Index.cshtml.cs
    ├── Create.cshtml
    ├── Create.cshtml.cs
    ├── Edit.cshtml
    ├── Edit.cshtml.cs
    ├── Delete.cshtml (or delete via modal from Index)
    ├── Delete.cshtml.cs
    ├── Run.cshtml
    └── Run.cshtml.cs
```

### Step 4: Create Base PageModel (if needed)

```csharp
// Reports/BaseReportsPageModel.cs
namespace Raytha.Web.Areas.Admin.Pages.Reports;

public abstract class BaseReportsPageModel : BaseAdminPageModel
{
    // Common logic for Reports feature
    // E.g., load report categories, common auth checks, etc.
}
```

### Step 5: Implement Index Page

**`Reports/Index.cshtml.cs`**
```csharp
public class Index : BaseReportsPageModel, IHasListView<ReportsListItemViewModel>
{
    public ListViewModel<ReportsListItemViewModel> ListView { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        ViewData["ActiveMenu"] = "Reports";
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Reports", IsActive = true }
        );
        
        ListView = await Mediator.Send(new GetReportsQuery
        {
            Search = ListView.Search,
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
            OrderBy = ListView.OrderByAsString
        });
    }
}
```

**`Reports/Index.cshtml`**
```html
@page "/raytha/reports"
@model Raytha.Web.Areas.Admin.Pages.Reports.Index

@{
    Layout = "SidebarLayout";
    ViewData["Title"] = "Reports";
    ViewData["ActiveMenu"] = "Reports";
}

<alert />

@await Html.PartialAsync("_Partials/TableCreateAndSearchBar", new BaseTableCreateAndSearchBarViewModel
{
    Pagination = Model.ListView,
    EntityName = "report",
    CreateActionName = RouteNames.Reports.Create
})

@if (!Model.ListView.Items.Any())
{
    @await Html.PartialAsync("_Partials/_EmptyState", new EmptyStateModel { ... })
}
else
{
    <!-- Table -->
}
```

### Step 6: Implement Create/Edit Pages

Follow patterns from [Section 2](#2-createedit-page-form) above.

### Step 7: Test

- [ ] Navigate to Reports from sidebar
- [ ] Active state highlights "Reports" in sidebar
- [ ] Breadcrumbs show on all pages
- [ ] CRUD operations work
- [ ] No console errors
- [ ] No hardcoded routes (verify with grep)

### Step 8: Update Tests

```csharp
// Tests/Routing/RouteNamesTests.cs
[TestCase(RouteNames.Reports.Index, "/Reports/Index")]
[TestCase(RouteNames.Reports.Create, "/Reports/Create")]
public void Reports_RouteNames_MatchExpectedPaths(string routeName, string expectedPath)
{
    Assert.That(routeName, Is.EqualTo(expectedPath));
}
```

---

## Common Pitfalls & Solutions

### ❌ Pitfall 1: Forgot to set ActiveMenu

**Problem:**
```csharp
public void OnGet()
{
    // Forgot ViewData["ActiveMenu"]
}
```

**Result:** No menu item highlighted in sidebar

**Solution:**
```csharp
public void OnGet()
{
    ViewData["ActiveMenu"] = "Users"; // Must match NavMenuItem.Id
}
```

---

### ❌ Pitfall 2: Hardcoded route in redirect

**Problem:**
```csharp
return RedirectToPage("/Users/Index"); // Hardcoded
```

**Result:** CI check fails, not refactor-friendly

**Solution:**
```csharp
return RedirectToPage(RouteNames.Users.Index);
```

---

### ❌ Pitfall 3: Forgot breadcrumbs

**Problem:**
```csharp
public void OnGet()
{
    ViewData["ActiveMenu"] = "Users";
    // Forgot SetBreadcrumbs()
}
```

**Result:** No breadcrumbs shown

**Solution:**
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

---

### ❌ Pitfall 4: Wrong layout

**Problem:**
```html
@{
    Layout = "_Layout"; // Too generic
}
```

**Result:** No sidebar, no breadcrumbs

**Solution:**
```html
@{
    Layout = "SidebarLayout"; // For index/list pages
    // or
    Layout = "SubActionLayout"; // For create/edit/delete pages
}
```

---

### ❌ Pitfall 5: Inline alert checks

**Problem:**
```html
@if (ViewData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">@ViewData["ErrorMessage"]</div>
}
```

**Result:** Duplicated code, not using refactored pattern

**Solution:**
```html
<alert />
```

---

## Quick Checklist for PR Review

**Before submitting PR:**

- [ ] All hardcoded routes replaced with `RouteNames.X.Y`
- [ ] `ViewData["ActiveMenu"]` set on every page
- [ ] Breadcrumbs added via `SetBreadcrumbs()` or auto-generated
- [ ] Using `SidebarLayout` or `SubActionLayout` (not custom layouts)
- [ ] Alerts rendered via `<alert />` tag helper
- [ ] Empty states use `_Partials/_EmptyState.cshtml`
- [ ] No `data-controller`, `data-action`, `data-turbo` attributes
- [ ] Forms use `asp-page="@RouteNames.X.Y"` for action
- [ ] Redirects use `RedirectToPage(RouteNames.X.Y)`
- [ ] Links use `asp-page="@RouteNames.X.Y"`
- [ ] Tests added/updated for new routes
- [ ] CI checks pass (routes, stimulus, view compilation)

---

**End of Examples**

