# Quick Reference: Local JavaScript Libraries

## How to Use Local Libraries in Future Development

### For Static Script Tags (in .cshtml files)

```html
<!-- SortableJS -->
<script src="~/raytha_admin/lib/sortablejs/Sortable.min.js"></script>

<!-- Flatpickr -->
<link href="~/raytha_admin/lib/flatpickr/dist/flatpickr.min.css" rel="stylesheet">
<script src="~/raytha_admin/lib/flatpickr/dist/flatpickr.min.js"></script>

<!-- Uppy -->
<link href="~/raytha_admin/lib/uppy/dist/uppy.min.css" rel="stylesheet">

<!-- TipTap (no CSS needed, bundle includes all) -->
<!-- Used via ES module import in JavaScript -->
```

### For Dynamic Loading (in .js files)

```javascript
// SortableJS - Dynamic loading
const script = document.createElement('script');
script.src = '/raytha_admin/lib/sortablejs/Sortable.min.js';
script.onload = () => { /* use Sortable here */ };
document.head.appendChild(script);

// Flatpickr - Dynamic loading
const link = document.createElement('link');
link.rel = 'stylesheet';
link.href = '/raytha_admin/lib/flatpickr/dist/flatpickr.min.css';
document.head.appendChild(link);

const script = document.createElement('script');
script.src = '/raytha_admin/lib/flatpickr/dist/flatpickr.min.js';
script.onload = () => { /* use flatpickr here */ };
document.head.appendChild(script);

// Uppy - ES Module Import
const { Uppy, Dashboard, XHRUpload, AwsS3 } = await import('/raytha_admin/lib/uppy/dist/uppy.min.mjs');

// TipTap - ES Module Import (all extensions in one bundle)
const { 
  Editor, Node, Extension,
  StarterKit, Link, Image, Table, TableRow, TableCell, TableHeader,
  Underline, Subscript, Superscript, TextStyle, FontFamily, TextAlign,
  Color, Highlight 
} = await import('/raytha_admin/lib/tiptap/tiptap-bundle.js');
```

### For ES Module Imports (in <script type="module">)

```javascript
// Uppy
import { Uppy, Dashboard, XHRUpload, AwsS3 } from "/raytha_admin/lib/uppy/dist/uppy.min.mjs"

// TipTap
import { 
  Editor, Node, Extension,
  StarterKit, Link, Image, Table, TableRow, TableCell, TableHeader,
  Underline, Subscript, Superscript, TextStyle, FontFamily, TextAlign,
  Color, Highlight 
} from "/raytha_admin/lib/tiptap/tiptap-bundle.js"
```

## CodeMirror Bundle Usage

```html
<script type="module">
    import { 
        EditorState,
        EditorView,
        basicSetup,
        html,
        javascript,
        css,
        indentWithTab,
        keymap
    } from '/raytha_admin/lib/codemirror/codemirror-bundle.js';
    
    // Now use these exports to create your editor...
</script>
```

## Directory Structure

```
/wwwroot/raytha_admin/lib/
├── sortablejs/
│   └── Sortable.min.js
├── flatpickr/
│   └── dist/
│       ├── flatpickr.min.js
│       └── flatpickr.min.css
└── uppy/
    └── dist/
        ├── uppy.min.mjs
        └── uppy.min.css
```

## Adding New Libraries

When adding new third-party JavaScript libraries:

1. **Download the library files:**
   ```bash
   mkdir -p /wwwroot/raytha_admin/lib/<library-name>/dist
   cd /wwwroot/raytha_admin/lib/<library-name>/dist
   curl -O <library-url>
   ```

2. **Reference using local paths:**
   - For Razor views: Use `~/raytha_admin/lib/<library-name>/...`
   - For JS files: Use `/raytha_admin/lib/<library-name>/...`

3. **Update this document** with the new library reference

## Important Notes

- Always use `~/` prefix in Razor views (.cshtml files)
- Always use `/` prefix in JavaScript files (.js files)
- Keep library versions documented in filenames or separate VERSION.txt files
- Test after adding/updating libraries to ensure they load correctly
- Consider using npm/yarn for more complex dependency management in the future

## Library Versions

- **SortableJS:** v1.15.2
- **Flatpickr:** v4.6.13
- **Uppy:** v4.13.3

Last Updated: November 12, 2025
