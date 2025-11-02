# Developer Name Sync Fix

## Issue
Multiple pages had broken developer name synchronization due to:
1. **Stimulus attributes** (`data-controller="shared--developername"`) that were removed but not replaced
2. **Inline scripts** calling `bindDeveloperNameSync()` without importing the function
3. **Duplicate function definitions** in inline scripts

## Files Fixed

### Razor Pages Updated (7 files):
1. **`Themes/Create.cshtml`**
   - Removed Stimulus `data-controller` and `data-action` attributes
   - Removed inline script calling `bindDeveloperNameSync`
   - Added `@section Scripts` loading `/js/pages/themes/create.js`

2. **`Themes/Import.cshtml`**
   - Removed Stimulus attributes
   - Removed inline script
   - Added `@section Scripts` loading `/js/pages/themes/import.js`

3. **`Themes/WebTemplates/Create.cshtml`**
   - Removed duplicate `slugifyDeveloperName` and `bindDeveloperNameSync` functions
   - Kept page-specific logic (base layout toggle, copy asset links, CodeMirror)
   - Added script reference to `/js/pages/themes/web-templates-create.js`

4. **`NavigationMenus/MenuItems/Create.cshtml`**
   - Removed inline script
   - Added `@section Scripts` loading `/js/pages/navigation-menus/menu-items-create.js`

5. **`RaythaFunctions/Create.cshtml`**
   - Removed inline script calling `bindDeveloperNameSync`
   - Kept CodeMirror import map and initialization (page-specific)

6. **`RaythaFunctions/Edit.cshtml`**
   - Removed Stimulus `data-raythafunctions--codehighlighting-target` attributes
   - Removed inline script
   - Kept CodeMirror import map and initialization

7. **`NavigationMenus/SubActionLayout.cshtml`**
   - Fixed delete form: replaced `onsubmit="return confirmDelete();"` with native `confirm()`
   - Removed Stimulus `data-action` attribute from delete button

### JavaScript Modules Created (4 files):

1. **`/wwwroot/js/pages/themes/create.js`**
   ```javascript
   import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
   
   function initThemeCreate() {
       bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName');
   }
   ```

2. **`/wwwroot/js/pages/themes/import.js`**
   ```javascript
   import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
   
   function initThemeImport() {
       bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName');
   }
   ```

3. **`/wwwroot/js/pages/themes/web-templates-create.js`**
   ```javascript
   import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
   
   function initWebTemplateCreate() {
       bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName');
   }
   ```

4. **`/wwwroot/js/pages/navigation-menus/menu-items-create.js`**
   ```javascript
   import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
   
   function initMenuItemCreate() {
       bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName');
   }
   ```

### JavaScript Modules Updated (2 files):

1. **`/wwwroot/js/pages/raytha-functions/create.js`**
   - Updated to use `bindDeveloperNameSync('#Form_Name', '#Form_DeveloperName')`

2. **`/wwwroot/js/pages/raytha-functions/edit.js`**
   - Simplified (developer name not editable after creation)

## Key Learnings

### 1. CSS Selectors Required
The `bindDeveloperNameSync` function expects CSS selectors, not bare IDs:
- ✅ Correct: `bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName')`
- ❌ Wrong: `bindDeveloperNameSync('Form_Title', 'Form_DeveloperName')`

### 2. Shared Module Usage
All developer name sync logic should use the shared module:
```javascript
import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
```

### 3. Acceptable Inline Scripts
Some inline scripts are acceptable when they:
- Use `<script type="importmap">` (required to be inline)
- Initialize complex third-party libraries (CodeMirror, TipTap)
- Are tightly coupled to server-side Razor data (`@Html.Raw(Json.Serialize(...))`)

### 4. Delete Confirmations
Delete forms should use native `confirm()` instead of custom functions:
```html
<form method="post" onsubmit="return confirm('Are you sure?');">
```

## Remaining Work

### Files Still Needing Cleanup (14 files):
These files still have `onsubmit="return confirmDelete();"` that references a non-existent function:
1. Users/SubActionLayout.cshtml
2. Themes/WebTemplates/SubActionLayout.cshtml
3. NavigationMenus/MenuItems/SubActionLayout.cshtml
4. ContentItems/SubActionLayout.cshtml
5. RaythaFunctions/SubActionLayout.cshtml
6. Themes/SubActionLayout.cshtml
7. Admins/SubActionLayout.cshtml
8. Roles/Edit.cshtml
9. AuthenticationSchemes/Edit.cshtml
10. EmailTemplates/Revisions.cshtml
11. UserGroups/Edit.cshtml
12. Themes/MediaItems.cshtml
13. Themes/WebTemplates/Revisions.cshtml
14. Admins/ApiKeys.cshtml

**Fix needed:** Replace `onsubmit="return confirmDelete();"` with `onsubmit="return confirm('Are you sure you want to delete?');"`

### Stimulus Attributes Still Present
Some pages still have Stimulus `data-action` or `data-controller` attributes (mainly in ContentItems pages for TipTap/Uppy).

## Build Status
✅ **Build succeeds** - 0 errors

## Testing Checklist
- ✅ Theme Create: Developer name syncs from Title
- ✅ Theme Import: Developer name syncs from Title
- ✅ Web Template Create: Developer name syncs from Label
- ✅ Menu Item Create: Developer name syncs from Label
- ✅ Raytha Function Create: Developer name syncs from Name
- 🧪 Navigation Menu Delete: Confirmation dialog works
- ⚠️ Other delete confirmations: Need testing after fix

---

**Status:** 🟢 **Main issue resolved** - Developer name sync now works across all create pages.

**Priority:** Fix remaining `confirmDelete()` references in 14 files.

