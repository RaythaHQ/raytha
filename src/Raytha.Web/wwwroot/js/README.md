# Raytha Admin JavaScript Modules

This directory contains the JavaScript module system for Raytha CMS Admin area. **No build step required** - all modules are native ES modules that run directly in modern browsers.

## 🎯 Philosophy

- **Zero Build:** No Node.js, npm, Webpack, TypeScript, or build tools required
- **Native ES Modules:** Use standard `<script type="module">` with imports/exports
- **Vanilla JavaScript:** No frameworks (jQuery, Alpine, Stimulus, etc.)
- **Clean Architecture:** Organized by purpose with clear separation of concerns
- **Developer-Friendly:** Well-documented with JSDoc, easy to understand and extend

## 📁 Directory Structure

```
/wwwroot/js/
├── core/               # Fundamental utilities used everywhere
│   ├── dom.js         # DOM manipulation helpers ($, $$, delegate, etc.)
│   ├── net.js         # HTTP/fetch with CSRF handling (get, post, del, etc.)
│   ├── events.js      # Event utilities (debounce, throttle, pub/sub)
│   ├── validation.js  # Form validation with Bootstrap styling
│   ├── modals.js      # Modal/dialog utilities (Bootstrap modals)
│   ├── nav.js         # Navigation highlighting and state management
│   ├── utils.js       # General utilities (formatters, slugify, etc.)
│   └── tables.js      # Table helpers (sorting, filtering, bulk actions)
│
├── shared/            # Reusable components across multiple pages
│   ├── role-permissions.js      # Role permission management logic
│   ├── developer-name-sync.js   # Auto-generate developer names from labels
│   └── confirm-dialog.js        # Confirmation modal handlers
│
└── pages/             # Page-specific controllers (one per Razor Page)
    ├── content-types/
    │   ├── create.js
    │   └── fields-create.js
    ├── roles/
    │   ├── create.js
    │   └── edit.js
    ├── users-groups/
    │   ├── create.js
    │   └── edit.js
    └── ... (organized by Admin area)
```

## 🚀 Getting Started

### Adding a New Page Controller

1. **Create the controller file:**

```bash
touch /wwwroot/js/pages/{area}/{page}.js
```

2. **Write the controller:**

```javascript
/**
 * {Area} - {Page} Page
 * Brief description of what this page does
 */

import { $, on } from '/js/core/dom.js';
import { post } from '/js/core/net.js';
import { confirmDialog } from '/js/core/modals.js';
import { ready } from '/js/core/events.js';

function init() {
  const form = $('form');
  if (!form) return;
  
  // Your page-specific logic here
  on(form, 'submit', async (e) => {
    e.preventDefault();
    // Handle form submission
  });
}

// Run on DOM ready
ready(init);
```

3. **Reference in Razor Page:**

```cshtml
@section Scripts {
<script type="module" defer src="~/js/pages/{area}/{page}.js"></script>
}
```

### Common Patterns

#### Form with Auto-Generated Developer Name

```javascript
import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';

ready(() => {
  bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName', {
    onlyIfEmpty: true
  });
});
```

#### Confirmation Before Delete

```javascript
import { confirmDialog } from '/js/core/modals.js';
import { delegate } from '/js/core/dom.js';

delegate(document, 'click', '[data-confirm-delete]', async (e, target) => {
  e.preventDefault();
  
  const confirmed = await confirmDialog('Are you sure you want to delete this item?', {
    title: 'Confirm Delete',
    confirmText: 'Delete',
    confirmClass: 'btn-danger',
    showWarning: true
  });
  
  if (confirmed) {
    // Submit form or make DELETE request
    const form = target.closest('form');
    form.submit();
  }
});
```

#### Form Validation

```javascript
import { bindFormValidation } from '/js/core/validation.js';
import { submitForm } from '/js/core/net.js';

const form = $('form');
if (form) {
  bindFormValidation(form, async () => {
    // Form is valid, submit it
    const result = await submitForm(form);
    window.location.href = result.redirectUrl;
  });
}
```

#### Bulk Selection in Tables

```javascript
import { initBulkSelect } from '/js/core/tables.js';

initBulkSelect('table', {
  selectAllSelector: '[data-select-all]',
  rowCheckboxSelector: '[data-row-select]',
  onSelectionChange: (selectedIds) => {
    console.log('Selected:', selectedIds);
    // Update bulk action toolbar
  }
});
```

## 📦 Core Utilities Reference

### DOM Utilities (`/js/core/dom.js`)

```javascript
import { $, $$, on, delegate, show, hide, toggle } from '/js/core/dom.js';

// Query selectors
const el = $('selector');                    // querySelector
const elements = $$('selector');             // querySelectorAll as array

// Event handling
on(el, 'click', handler);                    // addEventListener
delegate(root, 'click', 'selector', fn);     // Event delegation

// Visibility
show(el);                                     // Remove hidden attribute
hide(el);                                     // Add hidden attribute
toggle(el);                                   // Toggle visibility

// Form helpers
const data = formToJson(form);               // Form data as JSON object
const query = serializeForm(form);           // URL-encoded query string
```

### Network Utilities (`/js/core/net.js`)

```javascript
import { get, post, put, del, postForm } from '/js/core/net.js';

// Automatically includes CSRF token in headers
const data = await get('/api/users');
const result = await post('/api/users', { name: 'John' });
await del('/api/users/123');

// For file uploads
await postForm('/api/upload', formData);
```

### Event Utilities (`/js/core/events.js`)

```javascript
import { debounce, throttle, ready, eventBus } from '/js/core/events.js';

// Debounce/throttle
const debouncedSearch = debounce((query) => search(query), 300);
const throttledScroll = throttle(handleScroll, 100);

// DOM ready
ready(() => console.log('DOM ready!'));

// Event bus (pub/sub)
eventBus.on('user:updated', (user) => console.log(user));
eventBus.emit('user:updated', { id: 1, name: 'John' });
```

### Validation Utilities (`/js/core/validation.js`)

```javascript
import { validateForm, bindFormValidation, showFieldError } from '/js/core/validation.js';

// Validate entire form
const isValid = validateForm(form);

// Bind validation on submit
bindFormValidation(form, (e) => {
  // Called only if form is valid
});

// Show custom error
showFieldError(input, 'This field is invalid');
```

### Modal Utilities (`/js/core/modals.js`)

```javascript
import { confirmDialog, alertDialog, showModal } from '/js/core/modals.js';

// Confirmation dialog
const confirmed = await confirmDialog('Delete this item?', {
  title: 'Confirm Delete',
  confirmText: 'Delete',
  confirmClass: 'btn-danger',
  showWarning: true
});

// Alert dialog
await alertDialog('Item saved successfully!', {
  title: 'Success',
  type: 'success'
});

// Show existing Bootstrap modal
showModal('#myModal');
```

### Utility Functions (`/js/core/utils.js`)

```javascript
import { formatDate, slugify, toDeveloperName, copyToClipboard } from '/js/core/utils.js';

// Formatting
formatDate(new Date());                      // "Nov 2, 2025"
formatDateTime(new Date());                  // "Nov 2, 2025, 3:45 PM"
formatCurrency(1234.56, 'USD');             // "$1,234.56"

// String manipulation
slugify('My Content Type');                  // "my-content-type"
toDeveloperName('My Label');                // "my_label"
truncate('Long text...', 20);               // "Long text..."

// Utilities
await copyToClipboard('Text to copy');
downloadFile(blob, 'filename.txt');
const obj = safeParseJSON(jsonString, {});
```

## 🔧 Shared Components

### Role Permissions (`/js/shared/role-permissions.js`)

Auto-initializes on forms with `data-role-permissions-form` attribute. Handles complex permission dependencies:
- System "content_types" permission disables all content type permissions
- "Edit" permission requires "Read"
- "Config" permission requires both "Read" and "Edit"

### Developer Name Sync (`/js/shared/developer-name-sync.js`)

```javascript
import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

// Auto-generate developer name from label
bindDeveloperNameSync('#labelInput', '#devNameInput', {
  onlyIfEmpty: true  // Only sync if developer name is empty
});

// Or use data attributes in HTML
<input data-sync-to="#devNameInput" />
```

### Confirm Dialog (`/js/shared/confirm-dialog.js`)

Auto-initializes all modals with forms. Sets form action dynamically based on `data-action-url` attribute on trigger button.

## 🎨 Coding Conventions

### Import Paths

Always use **absolute paths** starting with `/js/`:

```javascript
// ✅ Good
import { $ } from '/js/core/dom.js';

// ❌ Bad (relative paths are fragile)
import { $ } from '../../core/dom.js';
```

### File Naming

- Files: `kebab-case.js`
- Functions: `camelCase`
- Classes: `PascalCase`

### Documentation

Use JSDoc for all exported functions:

```javascript
/**
 * Description of what the function does
 * @param {string} param1 - Description of param1
 * @param {Object} options - Options object
 * @param {boolean} options.flag - Description of flag
 * @returns {Promise<string>} - Description of return value
 */
export const myFunction = (param1, options = {}) => {
  // Implementation
};
```

### Module Structure

```javascript
/**
 * Module Name
 * Brief description of the module's purpose
 */

// Imports
import { $ } from '/js/core/dom.js';

// Private functions/variables
const privateHelper = () => { };

// Public API (exported)
export const publicFunction = () => { };

// Auto-initialization (if needed)
import { ready } from '/js/core/events.js';
ready(() => {
  // Initialize module
});
```

## 🧪 Testing

Since there's no build step, you can test directly in the browser:

1. Open Developer Console (F12)
2. Use `import()` to test modules:

```javascript
const { $ } = await import('/js/core/dom.js');
const el = $('body');
console.log(el);
```

## 🚫 What NOT to Do

- ❌ Don't add build tools (Webpack, Rollup, esbuild, etc.)
- ❌ Don't use TypeScript (keep it vanilla JS)
- ❌ Don't add JavaScript frameworks (jQuery, Alpine, React, etc.)
- ❌ Don't use relative imports (use absolute `/js/...` paths)
- ❌ Don't create `src/` and `dist/` directories
- ❌ Don't commit `node_modules/`, `package.json`, etc.

## 📚 Additional Resources

- [MDN: JavaScript Modules](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.3/)
- [Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API)

## 🤝 Contributing

When adding new functionality:

1. **Determine the right location:**
   - Core utility? → `/js/core/`
   - Shared component? → `/js/shared/`
   - Page-specific? → `/js/pages/{area}/`

2. **Write clean, documented code:**
   - Add JSDoc comments
   - Use descriptive names
   - Keep functions small and focused

3. **Test in the browser:**
   - Test in Chrome, Firefox, and Safari
   - Check console for errors
   - Verify it works without build step

4. **Update this README** if you add new core utilities or patterns

---

**Questions?** Check existing modules for examples or refer to the codebase instructions in `.github/instructions/raytha.instructions.md`.

