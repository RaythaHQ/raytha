# Quick Start Guide - Using the New Refactor Infrastructure

**Status:** PR0 + PR1 Complete ✅  
**Ready to use immediately!**

---

## 🎯 How to Use the New Features

### 1. Index/List Pages

**PageModel:**
```csharp
public class Index : BaseAdminPageModel, IHasListView<UsersListItemViewModel>
{
    public ListViewModel<UsersListItemViewModel> ListView { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        // Set active menu (for sidebar highlighting)
        ViewData["ActiveMenu"] = "Users";
        
        // Set breadcrumbs
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", IsActive = true }
        );
        
        // Load data...
        ListView = await Mediator.Send(new GetUsersQuery { /* ... */ });
    }
}
```

**View:**
```html
@page "/raytha/users"
@model Raytha.Web.Areas.Admin.Pages.Users.Index

@{
    Layout = "SidebarLayout";
    ViewData["Title"] = "Users";
    ViewData["ActiveMenu"] = "Users";
}

<!-- Automatic breadcrumbs and alerts via TagHelpers -->

<h2>@ViewData["Title"]</h2>

<!-- Table with data -->
<table class="table">
    <thead>
        <tr>
            <th><a asp-page="@RouteNames.Users.Index" asp-route-orderBy="FirstName asc">First Name</a></th>
            <!-- More columns -->
        </tr>
    </thead>
    <tbody>
        @foreach (var user in Model.ListView.Items)
        {
            <tr>
                <td>@user.FirstName</td>
                <td>
                    <a asp-page="@RouteNames.Users.Edit" asp-route-id="@user.Id">Edit</a>
                    <a asp-page="@RouteNames.Users.Delete" asp-route-id="@user.Id">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>

<!-- Pagination -->
@await Html.PartialAsync("_Partials/TablePagination", Model.ListView)
```

---

### 2. Create/Edit Pages (Simple Forms)

**PageModel:**
```csharp
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public EditUserDto Form { get; set; } = new();
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        // Set navigation
        ViewData["ActiveMenu"] = "Users";
        ViewData["BackLinkRoute"] = RouteNames.Users.Index;
        
        // Set breadcrumbs
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
            new BreadcrumbNode { Label = "Users", RouteName = RouteNames.Users.Index },
            new BreadcrumbNode { Label = "Edit User", IsActive = true }
        );
        
        // Load data...
        var result = await Mediator.Send(new GetUserByIdQuery { Id = id });
        if (!result.Success)
        {
            SetErrorMessage(result.Error);
            return RedirectToPage(RouteNames.Users.Index);
        }
        
        Form = MapToDto(result.Result);
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var result = await Mediator.Send(new UpdateUserCommand
        {
            Id = id,
            FirstName = Form.FirstName,
            // ...
        });
        
        if (!result.Success)
        {
            SetErrorMessage(result.Errors);
            return Page();
        }
        
        SetSuccessMessage("User updated successfully");
        return RedirectToPage(RouteNames.Users.Index);
    }
}
```

**View:**
```html
@page "/raytha/users/{id:guid}/edit"
@model Raytha.Web.Areas.Admin.Pages.Users.Edit

@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Edit User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
    ViewData["BackLinkLabel"] = "Back to Users"; // Optional
}

<!-- Breadcrumbs and alerts auto-rendered by SubActionLayout -->

<form asp-page="@RouteNames.Users.Edit" asp-route-id="@Model.Form.Id" method="post" novalidate>
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
        <label class="form-label raytha-required" asp-for="Form.EmailAddress"></label>
        <input type="email" 
               class="form-control @Model.HasError(nameof(Model.Form.EmailAddress))" 
               asp-for="Form.EmailAddress"
               required>
        <div class="invalid-feedback d-block">
            @Model.ErrorMessageFor(nameof(Model.Form.EmailAddress))
        </div>
    </div>
    
    <div class="mt-4">
        <button type="submit" class="btn btn-primary">Save Changes</button>
        <a asp-page="@RouteNames.Users.Index" class="btn btn-secondary">Cancel</a>
    </div>
</form>
```

---

### 3. Pages with Action Sidebar

**View:**
```html
@page "/raytha/users/{id:guid}/edit"
@model Raytha.Web.Areas.Admin.Pages.Users.Edit

@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Edit User";
    ViewData["ActiveMenu"] = "Users";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
}

<!-- Main content (form) goes in body -->
<form asp-page="@RouteNames.Users.Edit" asp-route-id="@Model.UserId" method="post">
    <!-- Form fields -->
</form>

<!-- Action sidebar -->
@section Sidebar {
    <div class="nav-wrapper position-relative">
        <ul class="nav nav-pills square nav-fill flex-column vertical-tab">
            <li class="nav-item">
                <a class="nav-link" 
                   asp-page="@RouteNames.Users.Edit" 
                   asp-route-id="@Model.UserId"
                   nav-active-section="Edit">
                    Edit User
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link" 
                   asp-page="@RouteNames.Users.ResetPassword" 
                   asp-route-id="@Model.UserId"
                   nav-active-section="Reset Password">
                    Reset Password
                </a>
            </li>
            <li class="nav-item">
                <button type="button" 
                        class="nav-link text-danger"
                        data-bs-toggle="modal" 
                        data-bs-target="#confirm-delete">
                    Delete User
                </button>
            </li>
        </ul>
    </div>
}

<!-- Confirmation dialog (at bottom of page) -->
@await Html.PartialAsync("_Partials/_ConfirmDialog", new ConfirmActionModel
{
    Id = "confirm-delete",
    Title = "Confirm Delete",
    Message = "Are you sure you want to delete this user?",
    ConfirmButtonText = "Delete User",
    ConfirmButtonClass = "btn-danger",
    ShowWarning = true,
    ActionUrl = Url.Page(RouteNames.Users.Delete, new { id = Model.UserId })
})
```

---

### 4. Confirmation Dialogs

**Option 1: Separate Delete Page (Traditional)**
```html
@page "/raytha/users/{id:guid}/delete"
@model Delete

@{
    Layout = "SubActionLayout";
    ViewData["Title"] = "Delete User";
    ViewData["BackLinkRoute"] = RouteNames.Users.Index;
}

<div class="alert alert-warning">
    <strong>⚠️ Warning!</strong> This action cannot be undone.
</div>

<p>Are you sure you want to delete <strong>@Model.User.FullName</strong>?</p>

<form asp-page="@RouteNames.Users.Delete" asp-route-id="@Model.Id" method="post">
    <button type="submit" class="btn btn-danger">Delete User</button>
    <a asp-page="@RouteNames.Users.Index" class="btn btn-secondary">Cancel</a>
</form>
```

**Option 2: Modal from Index Page (Modern, Preferred)**
```html
<!-- In your table row -->
<td>
    <a asp-page="@RouteNames.Users.Edit" asp-route-id="@user.Id">Edit</a>
    <button type="button"
            class="btn btn-link text-danger p-0"
            data-bs-toggle="modal"
            data-bs-target="#confirm-delete-@user.Id">
        Delete
    </button>
</td>

<!-- At bottom of page -->
@await Html.PartialAsync("_Partials/_ConfirmDialog", new ConfirmActionModel
{
    Id = $"confirm-delete-{user.Id}",
    Title = "Confirm Delete",
    Message = $"Are you sure you want to delete {user.FullName}?",
    ConfirmButtonText = "Delete",
    ConfirmButtonClass = "btn-danger",
    ActionUrl = Url.Page(RouteNames.Users.Delete, new { id = user.Id })
})

<!-- In PageModel, add handler -->
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

---

### 5. Navigation Links with Active State

**Automatic active highlighting:**
```html
<!-- Will automatically add "active" class when on Users page -->
<a asp-page="@RouteNames.Users.Index" 
   nav-active-section="Users" 
   class="nav-link">
    Users
</a>

<!-- Exact route matching -->
<a asp-page="@RouteNames.Dashboard.Index" 
   nav-route-name="/Dashboard/Index" 
   class="nav-link">
    Dashboard
</a>

<!-- Custom active class -->
<a asp-page="@RouteNames.Users.Index" 
   nav-active-section="Users" 
   nav-active-class="current"
   class="nav-link">
    Users
</a>
```

---

### 6. Breadcrumbs

**Automatic (via TagHelper in Layout):**
```csharp
// In PageModel
public void OnGet()
{
    SetBreadcrumbs(
        new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
        new BreadcrumbNode { Label = "Users", RouteName = RouteNames.Users.Index },
        new BreadcrumbNode { Label = "Edit User", IsActive = true }
    );
}

// In View - breadcrumbs auto-render via <breadcrumbs /> in layout
```

**Manual (using partial):**
```html
@{
    var breadcrumbs = new List<BreadcrumbNode>
    {
        new BreadcrumbNode { Label = "Dashboard", RouteName = RouteNames.Dashboard.Index },
        new BreadcrumbNode { Label = "Users", IsActive = true }
    };
}

@await Html.PartialAsync("_Partials/_Breadcrumbs", breadcrumbs)
```

---

### 7. Alerts

**Automatic (via TagHelper):**
```html
<!-- In layout or page -->
<alert />  <!-- Renders all alert types -->

<!-- Or specific alert -->
<alert alert-key="ErrorMessage" />
```

**Manual (model-based):**
```html
@await Html.PartialAsync("_Partials/_AlertMessage", new AlertMessage
{
    Type = AlertType.Success,
    Title = "Success!",
    Message = "Your changes have been saved.",
    IsDismissible = true
})
```

---

## 🎨 Common Patterns

### Empty States
```html
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
```

### Route Generation
```csharp
// In PageModel
string editUrl = GetPageUrl(RouteNames.Users.Edit, new { id = userId });
string absoluteUri = GetPageUri(RouteNames.Users.Index);

// In View
var deleteUrl = Url.Page(RouteNames.Users.Delete, new { id = user.Id });
```

---

## ✅ Checklist for New Pages

When creating a new page:

- [ ] Use `RouteNames` constants for all navigation
- [ ] Set `ViewData["ActiveMenu"]` in PageModel
- [ ] Call `SetBreadcrumbs()` in PageModel OnGet
- [ ] Use `Layout = "SidebarLayout"` or `Layout = "SubActionLayout"`
- [ ] Use `<alert />` instead of inline alert checks
- [ ] Use `asp-page="@RouteNames.X.Y"` for all links
- [ ] Use `RedirectToPage(RouteNames.X.Y)` for redirects
- [ ] Add `nav-active-section` to navigation links
- [ ] Use `_ConfirmDialog` partial for delete confirmations
- [ ] Validate with `Model.HasError()` and `Model.ErrorMessageFor()`

---

## 🚀 Ready to Go!

All infrastructure is in place. Start using these patterns on new pages or when refactoring existing pages. The foundation is solid, tested, and production-ready!

**Questions?** See `REFACTOR_PLAN.md` for full details or `IMPLEMENTATION_STATUS.md` for progress tracking.

