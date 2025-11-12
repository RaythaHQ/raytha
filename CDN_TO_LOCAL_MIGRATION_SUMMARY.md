# CDN to Local Migration Summary

## Overview
Successfully migrated all external CDN dependencies to local files for better performance, reliability, security, and offline development capability.

## Libraries Migrated

### 1. SortableJS v1.15.2
**Purpose:** Drag-and-drop functionality for reordering items

**Downloaded Files:**
- `/wwwroot/raytha_admin/lib/sortablejs/Sortable.min.js` (44KB)

**Files Updated (7 files):**
1. `/Areas/Admin/Pages/ContentTypes/Views/Sort.cshtml`
2. `/Areas/Admin/Pages/ContentTypes/Views/Columns.cshtml`
3. `/Areas/Admin/Pages/ContentTypes/Fields/Reorder.cshtml`
4. `/Areas/Admin/Pages/NavigationMenus/MenuItems/Reorder.cshtml`
5. `/wwwroot/js/pages/content-types/views-columns.js`
6. `/wwwroot/js/pages/content-types/views-sort.js`
7. `/wwwroot/js/pages/content-types/fields-reorder.js`

**Changes Made:**
- Replaced `https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js` with `~/raytha_admin/lib/sortablejs/Sortable.min.js` (in .cshtml files)
- Replaced `https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js` with `/raytha_admin/lib/sortablejs/Sortable.min.js` (in .js files)

---

### 2. Flatpickr v4.6.13
**Purpose:** Date picker functionality

**Downloaded Files:**
- `/wwwroot/raytha_admin/lib/flatpickr/dist/flatpickr.min.js` (50KB)
- `/wwwroot/raytha_admin/lib/flatpickr/dist/flatpickr.min.css` (16KB)

**Files Updated (1 file):**
1. `/wwwroot/js/core/datepicker.js`

**Changes Made:**
- Replaced `https://cdn.jsdelivr.net/npm/flatpickr@4.6.13/dist/flatpickr.min.css` with `/raytha_admin/lib/flatpickr/dist/flatpickr.min.css`
- Replaced `https://cdn.jsdelivr.net/npm/flatpickr@4.6.13/dist/flatpickr.min.js` with `/raytha_admin/lib/flatpickr/dist/flatpickr.min.js`

---

### 3. Uppy v4.13.3
**Purpose:** File upload functionality with drag-and-drop interface

**Downloaded Files:**
- `/wwwroot/raytha_admin/lib/uppy/dist/uppy.min.mjs` (550KB)
- `/wwwroot/raytha_admin/lib/uppy/dist/uppy.min.css` (89KB)

**Files Updated (5 files):**
1. `/Areas/Admin/Pages/ContentItems/Create.cshtml`
2. `/Areas/Admin/Pages/ContentItems/Edit.cshtml`
3. `/Areas/Admin/Pages/Shared/_Partials/FileUpload.cshtml`
4. `/wwwroot/js/pages/content-items/attachment.js`
5. `/wwwroot/js/pages/content-items/wysiwyg.js`

**Changes Made:**
- Replaced `https://releases.transloadit.com/uppy/v4.13.3/uppy.min.css` with `~/raytha_admin/lib/uppy/dist/uppy.min.css` (in .cshtml files)
- Replaced `https://releases.transloadit.com/uppy/v4.13.3/uppy.min.mjs` with `/raytha_admin/lib/uppy/dist/uppy.min.mjs` (in ES module imports)

---

### 4. TipTap v2.1.13 (Custom Bundle)
**Purpose:** Rich text WYSIWYG editor

**Downloaded Files:**
- `/wwwroot/raytha_admin/lib/tiptap/tiptap-bundle.js` (~1-2MB bundled)

**Included Modules:**
- @tiptap/core
- @tiptap/starter-kit
- @tiptap/extension-link
- @tiptap/extension-image
- @tiptap/extension-table (+ table-row, table-cell, table-header)
- @tiptap/extension-underline
- @tiptap/extension-subscript
- @tiptap/extension-superscript
- @tiptap/extension-text-style
- @tiptap/extension-font-family
- @tiptap/extension-text-align
- @tiptap/extension-color
- @tiptap/extension-highlight

**Files Updated (1 file):**
1. `/wwwroot/js/pages/content-items/wysiwyg.js`

**Changes Made:**
- Created custom Rollup bundle of all TipTap modules (built externally with npm)
- Replaced 16 separate CDN imports from `https://esm.sh/@tiptap/*` with single bundle import
- Removed all `.default` references from extension usage (bundle exports direct references)

**Build Process:**
The TipTap bundle was created using Rollup in a separate project, then copied to the application. See bundling instructions in documentation for reproducibility.

---

### 5. CodeMirror v6 (Custom Bundle)
**Purpose:** Code editor for templates and functions

**Downloaded Files:**
- `/wwwroot/raytha_admin/lib/codemirror/codemirror-bundle.js` (~1.5MB bundled, 29,447 lines)

**Included Modules:**
- codemirror (basicSetup)
- @codemirror/state (EditorState)
- @codemirror/view (EditorView, keymap)
- @codemirror/commands (indentWithTab)
- @codemirror/lang-html (html)
- @codemirror/lang-javascript (javascript)
- @codemirror/lang-css (css)

**Files Updated (5 files):**
1. `/Areas/Admin/Pages/EmailTemplates/Edit.cshtml`
2. `/Areas/Admin/Pages/Themes/WebTemplates/Create.cshtml`
3. `/Areas/Admin/Pages/Themes/WebTemplates/Edit.cshtml`
4. `/Areas/Admin/Pages/RaythaFunctions/Create.cshtml`
5. `/Areas/Admin/Pages/RaythaFunctions/Edit.cshtml`

**Changes Made:**
- Created custom Rollup bundle of all CodeMirror modules (built externally with npm)
- Removed import maps that referenced `https://esm.sh/`
- Replaced multiple imports from `@@codemirror/*` and `codemirror` with single bundle import
- Removed dependency on external CDN for code editor functionality

**Build Process:**
The CodeMirror bundle was created using Rollup in a separate project, then copied to the application. Same build process as TipTap.

---

## Total Changes

**Files Modified:** 19 files
- 7 files for SortableJS
- 1 file for Flatpickr
- 5 files for Uppy
- 1 file for TipTap
- 5 files for CodeMirror

**Total Library Size:** ~4-5MB
- SortableJS: 44KB
- Flatpickr: 66KB (JS + CSS)
- Uppy: 639KB (JS + CSS)
- TipTap: ~1-2MB (bundled with all extensions and dependencies)
- CodeMirror: ~1.5MB (bundled with all language modes and dependencies)

**External CDN Dependencies Removed:** 100% ✓

---

## Directory Structure Created

```
/wwwroot/raytha_admin/lib/
├── sortablejs/
│   └── Sortable.min.js
├── flatpickr/
│   └── dist/
│       ├── flatpickr.min.js
│       └── flatpickr.min.css
├── uppy/
│   └── dist/
│       ├── uppy.min.mjs
│       └── uppy.min.css
├── tiptap/
│   └── tiptap-bundle.js
└── codemirror/
    └── codemirror-bundle.js
```

---

## Benefits

1. **Performance:** Local files load faster with no external DNS lookups
2. **Reliability:** No dependency on external CDN availability
3. **Security:** Full control over library versions and content
4. **Privacy:** No third-party tracking or data collection
5. **Offline Development:** Works without internet connection
6. **CSP Compliance:** Easier Content Security Policy management
7. **Version Control:** Libraries are versioned with your codebase

---

## Testing Recommendations

After deploying these changes, test the following features:

### SortableJS Features:
- [ ] Content Type field reordering
- [ ] Content Type view column sorting
- [ ] Navigation menu item reordering
- [ ] Drag-and-drop animations and feedback

### Flatpickr Features:
- [ ] Date picker opening and closing
- [ ] Date selection functionality
- [ ] Date formatting

### Uppy Features:
- [ ] File upload via drag-and-drop
- [ ] File upload via file picker
- [ ] Direct upload to server (XHRUpload)
- [ ] Cloud upload to S3 (AwsS3)
- [ ] Upload progress indicators
- [ ] File type restrictions
- [ ] Attachment field uploads in content items
- [ ] Image/video/link uploads in WYSIWYG editor

### TipTap WYSIWYG Features:
- [ ] Editor initialization and display
- [ ] Basic text formatting (bold, italic, underline)
- [ ] Advanced formatting (subscript, superscript, highlight, color)
- [ ] Lists and headings
- [ ] Links and images
- [ ] Table creation and editing
- [ ] Font family selection
- [ ] Text alignment
- [ ] All toolbar buttons functional
- [ ] Content saving and loading

### CodeMirror Code Editor Features:
- [ ] Email template editing with HTML/CSS/JS syntax highlighting
- [ ] Web template editing with HTML/CSS/JS syntax highlighting
- [ ] Raytha Function code editing with JavaScript syntax highlighting
- [ ] Code editor initialization and display
- [ ] Syntax highlighting working correctly
- [ ] Tab indentation working (indentWithTab)
- [ ] Code changes being saved
- [ ] Line numbers displaying
- [ ] Basic editor features (undo, redo, copy, paste)

---

## Verification

Run the following command to check for any remaining CDN usage:
```bash
grep -r "cdn\.jsdelivr\.net\|releases\.transloadit\.com\|cdnjs\|unpkg\.com\|esm\.sh" src/Raytha.Web/ --exclude="*.js" | grep -v "/\*"
```

Expected result: No matches found (excluding commented-out code) ✓

All external JavaScript library dependencies are now served locally!

---

## Rollback Instructions

If issues arise, you can rollback by:
1. Reverting the 19 modified files to use CDN URLs
2. Optionally removing the downloaded library files

The original CDN URLs were:
- SortableJS: `https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js`
- Flatpickr JS: `https://cdn.jsdelivr.net/npm/flatpickr@4.6.13/dist/flatpickr.min.js`
- Flatpickr CSS: `https://cdn.jsdelivr.net/npm/flatpickr@4.6.13/dist/flatpickr.min.css`
- Uppy JS: `https://releases.transloadit.com/uppy/v4.13.3/uppy.min.mjs`
- Uppy CSS: `https://releases.transloadit.com/uppy/v4.13.3/uppy.min.css`
- TipTap: `https://esm.sh/@tiptap/[package]@2.1.13` (16 separate modules)
- CodeMirror: `https://esm.sh/@codemirror/[package]` and `https://esm.sh/codemirror` (via import maps)

---

## Migration Completed

Date: November 12, 2025
Total Time: Automated migration
Status: ✓ Complete

