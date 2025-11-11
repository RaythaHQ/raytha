# Set As Active Theme - POST-Only Correction

## Issue
The migration from legacy MVC to Razor Pages incorrectly implemented the "Set as Active Theme" functionality as a GET handler when it should have been POST-only.

## Root Cause
The legacy MVC controller had a **POST-only** action that would:
1. Check if web template matching is needed
2. If yes: return a view with a matching form
3. If no: directly set the theme as active and redirect

The Razor Pages migration incorrectly converted this to a `OnGet` handler, which doesn't match the original POST-only semantics.

## Changes Made

### 1. SetAsActive.cshtml.cs

#### Before (Incorrect)
```csharp
public async Task<IActionResult> OnGet(string id)
{
    // Logic to check templates and show matching form or set active
}

public async Task<IActionResult> OnPost(string id)
{
    // Process the matching form
}
```

#### After (Correct)
```csharp
public IActionResult OnGet(string id)
{
    // This page is POST-only. Redirect to themes list if accessed via GET.
    return RedirectToPage(RouteNames.Themes.Index);
}

public async Task<IActionResult> OnPost(string id)
{
    // Initial POST: Check if templates need matching
    // If yes: Show matching form (return Page())
    // If no: Set active and redirect
}

public async Task<IActionResult> OnPostMatch(string id)
{
    // Process the matching form submission
}
```

### 2. SetAsActive.cshtml

Changed form to use the `Match` page handler:

```html
<!-- Before -->
<form method="post" asp-page="@RouteNames.Themes.SetAsActive" ...>

<!-- After -->
<form method="post" asp-page-handler="Match" ...>
```

### 3. Nullability Fixes

Added proper initializers to satisfy nullable reference type requirements:

```csharp
public FormModel Form { get; set; } = default!;
public string Id { get; set; } = string.Empty;

public record FormModel
{
    public string ThemeId { get; set; } = string.Empty;
    public IEnumerable<string> ActiveThemeWebTemplateDeveloperNames { get; set; } = Array.Empty<string>();
    public IEnumerable<string> NewActiveThemeWebTemplateDeveloperNames { get; set; } = Array.Empty<string>();
    // ...
}
```

## Flow Diagram

### Correct Flow (POST-only)

```
Themes Edit Page
     |
     | [User clicks "Set as Active" button]
     | POST to SetAsActive
     v
SetAsActive.OnPost(id)
     |
     ├─ Templates need matching?
     |  YES → return Page() (show matching form)
     |         |
     |         | [User fills form and submits]
     |         | POST with handler="Match"
     |         v
     |    OnPostMatch(id)
     |         |
     |         └─→ BeginMatchWebTemplates command
     |             └─→ Redirect to BackgroundTaskStatus
     |
     └─ NO → SetAsActiveTheme command
             └─→ Redirect to Themes Edit
```

## Verification

### Entry Point
**File**: `/Areas/Admin/Pages/Themes/Edit.cshtml` (line 48)

```html
<form asp-page="@RouteNames.Themes.SetAsActive" 
      method="post" 
      asp-route-id="@Model.Form.Id">
    <button type="submit">Set as active theme</button>
</form>
```

✅ Already correctly uses POST method

### Handler Separation
- `OnGet`: Redirects to themes list (POST-only page)
- `OnPost`: Initial handler (checks if matching needed)
- `OnPostMatch`: Form submission handler (processes template matching)

## Testing Checklist

- [ ] Click "Set as Active" on a theme that doesn't need template matching
  - Should directly set as active and redirect to Edit page
  - Success message should appear

- [ ] Click "Set as Active" on a theme that needs template matching
  - Should show the matching form
  - Form should have dropdowns for each template mapping
  - Submit button should be present

- [ ] Fill out template matching form and submit
  - Should redirect to BackgroundTaskStatus page
  - "Set as the active theme in progress" message should appear

- [ ] Try accessing SetAsActive page via GET (direct URL)
  - Should redirect to Themes Index page

## Related Files

### Modified
- `/Areas/Admin/Pages/Themes/SetAsActive.cshtml.cs`
- `/Areas/Admin/Pages/Themes/SetAsActive.cshtml`

### Referenced (No Changes)
- `/Areas/Admin/Pages/Themes/Edit.cshtml` (trigger point)
- `/Application/Themes/Commands/SetAsActiveTheme.cs`
- `/Application/Themes/Commands/BeginMatchWebTemplates.cs`

## Legacy MVC Reference

The original MVC controller action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Route($"{RAYTHA_ROUTE_PREFIX}/themes/set-as-active-theme/{{id}}", 
       Name = "themessetasactivetheme")]
public async Task<IActionResult> SetAsActiveTheme(string id)
{
    var webTemplateDeveloperNamesWithoutRelationResponse = 
        await Mediator.Send(new GetWebTemplateDeveloperNamesWithoutRelation.Query
        {
            ThemeId = id,
        });

    if (webTemplateDeveloperNamesWithoutRelationResponse.Result.Any())
    {
        // Show matching view
        return View("~/Areas/Admin/Views/Themes/MatchingWebTemplates.cshtml", model);
    }

    // Set as active directly
    var input = new SetAsActiveTheme.Command { Id = id };
    var response = await Mediator.Send(input);
    // ...
    return RedirectToAction(nameof(Edit), new { id });
}
```

**Key Observation**: POST-only, no GET handler

## Anti-Forgery Tokens

Both implementations use anti-forgery protection:
- **MVC**: `[ValidateAntiForgeryToken]` attribute
- **Razor Pages**: Automatic via `asp-page` and `asp-page-handler` tag helpers

## Conclusion

The "Set as Active Theme" functionality has been corrected to match the original POST-only semantics from the legacy MVC implementation. The page now properly:

1. ✅ Rejects GET requests (redirects to Themes Index)
2. ✅ Handles initial POST to determine if matching is needed
3. ✅ Shows matching form when templates need mapping
4. ✅ Processes form submission via named handler
5. ✅ Maintains anti-forgery protection
6. ✅ Follows Razor Pages conventions

The implementation is now consistent with the original behavior and follows proper POST-redirect-GET patterns where applicable.

