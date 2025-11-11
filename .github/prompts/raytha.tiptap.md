Here’s a copy-paste prompt for Cursor. It tells the agent exactly what to change and how, with Bootstrap-5 UI, Uppy uploads, and TipTap config.

---

**PROMPT FOR CURSOR**

You’re working in a .NET Razor Pages app. TipTap v2 is already loaded via CDN importmap. We have `/wwwroot/raytha_admin/js/wysiwyg.js` and the WYSIWYG field partial shown below.

### Goals

Implement full link, image, and video dialogs (Bootstrap 5), a right-click link context menu, fix the Quote action, and bring the toolbar to parity with the screenshots. Use vanilla JS only. Use our existing Uppy setup for uploads (same module and init pattern we already use elsewhere in the app). Follow Bootstrap 5 styling.

### Files to modify or add

* **Edit**: `wwwroot/raytha_admin/js/wysiwyg.js`
* **Add**: `wwwroot/raytha_admin/css/wysiwyg.css` (new stylesheet for editor UI tweaks; include from the Razor Page where the editor is used)
* **No markup changes to the partial** other than appending modal HTML at runtime.

### Required TipTap extensions

Import these (all @2.1.13 to match existing):

* `@tiptap/core`, `@tiptap/starter-kit`
* `@tiptap/extension-link`
* `@tiptap/extension-image`
* `@tiptap/extension-table`, `@tiptap/extension-table-row`, `@tiptap/extension-table-cell`, `@tiptap/extension-table-header`
* `@tiptap/extension-underline`
* `@tiptap/extension-subscript`
* `@tiptap/extension-superscript`
* `@tiptap/extension-text-style`
* `@tiptap/extension-font-family`
* `@tiptap/extension-text-align` (configure for headings and paragraphs)
* `@tiptap/extension-video` (for simple <video>/<iframe> embeds)

If `@tiptap/extension-video` is not available from CDN, implement a minimal custom `Video` node that renders an HTML5 `<video controls src="...">` or an `<iframe>` for YouTube/Vimeo URLs.

### Toolbar parity (match screenshots)

Add buttons or dropdowns for:

* **Bold, Italic, Underline, Strike**
* **Subscript, Superscript**
* **Headings** (Paragraph, H1, H2, H3)
* **Font family** dropdown (at least: Default, Helvetica, Georgia, Monospace)
* **Font size** dropdown (12, 14, 16, 18, 24, 32 px) using `TextStyle` by setting `style="font-size: …px"`
* **Lists** (bullet, ordered)
* **Align** (left, center, right, justify) using `TextAlign`
* **Blockquote** (the “Quote” button)
* **Code block**
* **Link**, **Image**, **Video**
* **Undo / Redo**

Keep Bootstrap 5 small outline buttons; group with separators as in current code.

### Fix the Quote action

Current “Quote” appears to do nothing when the cursor is in an empty paragraph. Change handler so:

* If selection is collapsed, execute `editor.chain().focus().setBlockquote().run()` which inserts a quoted paragraph and places caret inside.
* If selection spans text/blocks, `toggleBlockquote()` is fine.
  Also ensure `blockquote` has visible styling via CSS (see CSS section).

### Link: modal dialog and behavior

Replace the current `prompt()` flow with a Bootstrap 5 modal that matches the **Insert link** screenshot:

Tabs: **General** (default), **Upload**.

* **General** fields:

  * URL (required)
  * Text to display (prefill with selection text; if none, leave blank and insert the URL as text)
  * Title (optional)
  * Checkbox: “Open in new window” (sets `target="_blank" rel="noopener noreferrer"`)
* **Upload**: Use existing Uppy initializer from our codebase to upload a file and return a public URL. When upload completes, populate the URL field and focus the “Insert link” button.

Buttons:

* Primary: **Insert link** (enabled only if URL is non-empty)
* Dismiss: “×” close

On submit:

* If selection is over an existing link, update it via `extendMarkRange('link')` then `setLink({ href, target, rel, title })`.
* Else, insert link text then apply `setLink`.
* Set styling via Link extension HTMLAttributes (see “Link styling”).

### Right-click context menu on existing links

When the user right-clicks inside an `<a>` in the editor:

* Prevent the default context menu.
* Show a small Bootstrap dropdown (or a custom absolutely-positioned menu) anchored near the click with three actions:

  1. **Edit link** → opens the same link modal prefilled from the clicked `<a>` attributes (`href`, `title`, `target`).
  2. **Unlink** → `editor.chain().focus().extendMarkRange('link').unsetLink().run()`.
  3. **Open link** → `window.open(href, '_blank')`.
     Close the menu on outside click or Escape.

### Image: modal dialog with URL and Upload

Create a Bootstrap modal **Insert image** matching the screenshots with tabs **General** and **Upload**; ignore Gallery.

* **General** fields: Image URL (required), Alt text, Width, Height.
* **Upload**: Uppy dropzone. On upload success, set Image URL field.
* Insert with `editor.chain().focus().setImage({ src, alt, width, height }).run()`.

### Video: modal dialog with URL and Upload

Create **Insert video** modal mirroring the image modal; tabs **General** and **Upload**, ignore Gallery.

* **General**: Video URL. If YouTube/Vimeo, store URL and render as `<iframe>`; if file URL, render `<video controls src="…">`.
* **Upload**: Uppy; on success, set URL field.
* Insert with the video extension (or custom node) accordingly.

### Link styling

Make links look like real links:

* In `Editor` config, set `Link.configure({ openOnClick: false, HTMLAttributes: { class: 'link-primary' } })`.
* In `wwwroot/raytha_admin/css/wysiwyg.css`, add:

```css
.tiptap-editor-wrapper .ProseMirror a {
  color: var(--bs-link-color);
  text-decoration: underline;
}
.tiptap-editor-wrapper .ProseMirror a:hover {
  color: var(--bs-link-hover-color);
  text-decoration: underline;
}
.tiptap-toolbar .btn.active { /* already toggled in JS */ }
blockquote {
  border-left: .25rem solid var(--bs-border-color);
  margin: 0 0 1rem 0;
  padding: .5rem 1rem;
  color: var(--bs-secondary-color);
  background: var(--bs-light);
}
```

### Bootstrap 5 modal helpers

Implement a small helper in `wysiwyg.js` to **create the modal markup once per page** and reuse it per editor instance:

* `ensureModal(id, htmlString)` that appends to `document.body` if not already present.
* Each modal gets a unique id prefix: `tiptap-link-modal`, `tiptap-image-modal`, `tiptap-video-modal`.
* Use `new bootstrap.Modal(...)` to show/hide.

### Uppy integration

* Reuse our existing Uppy initializer. For each modal’s Upload tab:

  * Mount a drop area.
  * On successful upload, receive the public URL and populate the URL input.
  * Do not submit automatically; user still clicks **Insert**.

### Editor wiring changes in `wysiwyg.js`

1. **Imports**: add required TipTap extensions above.
2. **Extensions**: include `Underline`, `Subscript`, `Superscript`, `TextStyle`, `FontFamily`, `TextAlign`, and `Video` in `new Editor({ extensions: [...] })`. Configure `TextAlign` for `types: ['heading', 'paragraph']`.
3. **Toolbar**: expand to include required buttons and dropdowns. For font family and size, build `<select>` controls that call `editor.chain().focus().setFontFamily(...).run()` and `editor.chain().focus().setMark('textStyle', { fontSize: '16px' }).run()` (or `updateAttributes('textStyle', ...)`).
4. **Quote button**: see “Fix the Quote action” above.
5. **Link button**: opens the link modal, not `prompt()`.
6. **Image/Video buttons**: open their modals.
7. **Active state**: update `updateToolbarState()` to reflect underline, subscript, superscript, align, lists, codeBlock, link, headings, etc.
8. **Context menu**: add `editor.options.element.addEventListener('contextmenu', ...)` to detect anchors and show the custom menu.

### Partial view compatibility

Do not change the Razor partial. We still store HTML back into the hidden `<textarea>` in `onUpdate`.

### Accessibility and UX

* Focus first input when a modal opens.
* Disable primary button until required fields are valid.
* Close modals on success.
* Escape closes menus and modals.
* Keep keyboard focus within modals while open.

### Acceptance tests

* Quote button inserts a visible blockquote when caret is in an empty paragraph; toggles on/off with selection.
* Link button opens Bootstrap modal that matches fields in the screenshot. Inserting sets link with proper styling. `Open in new window` sets `target="_blank" rel="noopener noreferrer"`.
* Right-click on an existing link shows a small menu with **Edit link**, **Unlink**, **Open link**; each action works.
* Links in the editor render blue and underlined, hover style follows Bootstrap 5 variables.
* Image dialog: URL insert works; Upload tab uploads via Uppy and inserts the uploaded image. Alt/width/height respected.
* Video dialog: URL insert works; Upload tab uploads and inserts playable `<video>`; YouTube/Vimeo URL renders with `<iframe>`.
* Toolbar includes and correctly toggles all items listed in “Toolbar parity.”
* No Stimulus/Hotwire. Vanilla JS only.

### Deliverables

* Updated `wysiwyg.js` with all logic.
* New `wysiwyg.css`.
* Any small helper functions inline in `wysiwyg.js`.
* Notes in code comments where we reused our Uppy initializer and any assumptions about import paths.

Run these changes and stop for review.
