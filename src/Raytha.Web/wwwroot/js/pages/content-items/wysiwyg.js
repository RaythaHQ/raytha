/**
 * Content Items - WYSIWYG Field Module
 * Initializes TipTap WYSIWYG editors for content item fields
 * 
 * TipTap is loaded via CDN with an importmap in the page.
 * This module provides the initialization logic for each field instance.
 */

/**
 * Initialize a WYSIWYG field with TipTap editor
 * @param {HTMLElement} fieldElement - The field container element
 * @param {Object} config - Configuration object
 * @param {string} config.developername - Field developer name
 * @param {string} config.initialValue - Initial editor content
 * @param {Array} config.imageMediaItems - Available image media items
 * @param {Array} config.videoMediaItems - Available video media items
 * @returns {Object} Editor instance
 */
export async function initWysiwygField(fieldElement, config) {
    const developername = config.developername || fieldElement.dataset.developername;
    const textarea = fieldElement.querySelector('textarea[data-developername]');
    const editorContainer = fieldElement.querySelector('.mb-3 > div:not(.invalid-feedback):not(.form-text)');

    if (!textarea || !editorContainer) {
        console.error('WYSIWYG field elements not found:', developername);
        return null;
    }

    // TipTap imports - these will be available via the importmap defined in the page
    const { Editor } = await import('https://esm.sh/@tiptap/core@2.1.13');
    const StarterKit = await import('https://esm.sh/@tiptap/starter-kit@2.1.13');
    const Link = await import('https://esm.sh/@tiptap/extension-link@2.1.13');
    const Image = await import('https://esm.sh/@tiptap/extension-image@2.1.13');
    const Table = await import('https://esm.sh/@tiptap/extension-table@2.1.13');
    const TableRow = await import('https://esm.sh/@tiptap/extension-table-row@2.1.13');
    const TableCell = await import('https://esm.sh/@tiptap/extension-table-cell@2.1.13');
    const TableHeader = await import('https://esm.sh/@tiptap/extension-table-header@2.1.13');

    // Create toolbar
    const toolbar = document.createElement('div');
    toolbar.className = 'tiptap-toolbar border border-bottom-0 p-2 bg-light d-flex flex-wrap gap-1';
    toolbar.innerHTML = `
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="bold" title="Bold">
            <strong>B</strong>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="italic" title="Italic">
            <em>I</em>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="strike" title="Strikethrough">
            <s>S</s>
        </button>
        <span class="border-start mx-1"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="heading-1" title="Heading 1">
            H1
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="heading-2" title="Heading 2">
            H2
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="heading-3" title="Heading 3">
            H3
        </button>
        <span class="border-start mx-1"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="bullet-list" title="Bullet List">
            •
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="ordered-list" title="Numbered List">
            1.
        </button>
        <span class="border-start mx-1"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="blockquote" title="Quote">
            "
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="code-block" title="Code Block">
            &lt;/&gt;
        </button>
        <span class="border-start mx-1"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="link" title="Insert Link">
            🔗
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="image" title="Insert Image">
            🖼️
        </button>
        <span class="border-start mx-1"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="undo" title="Undo">
            ↶
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="redo" title="Redo">
            ↷
        </button>
    `;

    // Create editor wrapper
    const editorWrapper = document.createElement('div');
    editorWrapper.className = 'tiptap-editor-wrapper border';
    editorWrapper.style.minHeight = '300px';

    editorContainer.appendChild(toolbar);
    editorContainer.appendChild(editorWrapper);

    // Initialize TipTap editor
    const editor = new Editor({
        element: editorWrapper,
        extensions: [
            StarterKit.default,
            Link.default.configure({
                openOnClick: false,
                HTMLAttributes: {
                    class: 'text-primary',
                },
            }),
            Image.default.configure({
                HTMLAttributes: {
                    class: 'img-fluid',
                },
            }),
            Table.default.configure({
                resizable: true,
            }),
            TableRow.default,
            TableCell.default,
            TableHeader.default,
        ],
        content: textarea.value || '',
        editorProps: {
            attributes: {
                class: 'prose max-w-none p-3 focus:outline-none',
                style: 'min-height: 250px;',
            },
        },
        onUpdate: ({ editor }) => {
            textarea.value = editor.getHTML();
        },
    });

    // Toolbar actions
    toolbar.addEventListener('click', (e) => {
        const button = e.target.closest('button[data-action]');
        if (!button) return;

        e.preventDefault();
        const action = button.dataset.action;

        switch (action) {
            case 'bold':
                editor.chain().focus().toggleBold().run();
                break;
            case 'italic':
                editor.chain().focus().toggleItalic().run();
                break;
            case 'strike':
                editor.chain().focus().toggleStrike().run();
                break;
            case 'heading-1':
                editor.chain().focus().toggleHeading({ level: 1 }).run();
                break;
            case 'heading-2':
                editor.chain().focus().toggleHeading({ level: 2 }).run();
                break;
            case 'heading-3':
                editor.chain().focus().toggleHeading({ level: 3 }).run();
                break;
            case 'bullet-list':
                editor.chain().focus().toggleBulletList().run();
                break;
            case 'ordered-list':
                editor.chain().focus().toggleOrderedList().run();
                break;
            case 'blockquote':
                editor.chain().focus().toggleBlockquote().run();
                break;
            case 'code-block':
                editor.chain().focus().toggleCodeBlock().run();
                break;
            case 'link':
                const url = prompt('Enter URL:');
                if (url) {
                    editor.chain().focus().setLink({ href: url }).run();
                }
                break;
            case 'image':
                showImagePicker(editor, config.imageMediaItems || []);
                break;
            case 'undo':
                editor.chain().focus().undo().run();
                break;
            case 'redo':
                editor.chain().focus().redo().run();
                break;
        }

        // Update button active state
        updateToolbarState(toolbar, editor);
    });

    // Update toolbar state when selection changes
    editor.on('selectionUpdate', () => {
        updateToolbarState(toolbar, editor);
    });

    return editor;
}

/**
 * Update toolbar button active states based on current editor state
 * @param {HTMLElement} toolbar - Toolbar element
 * @param {Object} editor - TipTap editor instance
 */
function updateToolbarState(toolbar, editor) {
    const buttons = toolbar.querySelectorAll('button[data-action]');
    buttons.forEach(button => {
        const action = button.dataset.action;
        let isActive = false;

        switch (action) {
            case 'bold':
                isActive = editor.isActive('bold');
                break;
            case 'italic':
                isActive = editor.isActive('italic');
                break;
            case 'strike':
                isActive = editor.isActive('strike');
                break;
            case 'heading-1':
                isActive = editor.isActive('heading', { level: 1 });
                break;
            case 'heading-2':
                isActive = editor.isActive('heading', { level: 2 });
                break;
            case 'heading-3':
                isActive = editor.isActive('heading', { level: 3 });
                break;
            case 'bullet-list':
                isActive = editor.isActive('bulletList');
                break;
            case 'ordered-list':
                isActive = editor.isActive('orderedList');
                break;
            case 'blockquote':
                isActive = editor.isActive('blockquote');
                break;
            case 'code-block':
                isActive = editor.isActive('codeBlock');
                break;
            case 'link':
                isActive = editor.isActive('link');
                break;
        }

        if (isActive) {
            button.classList.add('active', 'btn-primary');
            button.classList.remove('btn-outline-secondary');
        } else {
            button.classList.remove('active', 'btn-primary');
            button.classList.add('btn-outline-secondary');
        }
    });
}

/**
 * Show image picker modal
 * @param {Object} editor - TipTap editor instance
 * @param {Array} imageMediaItems - Available images
 */
function showImagePicker(editor, imageMediaItems) {
    // Simple prompt for now - can be enhanced with a modal later
    const url = prompt('Enter image URL or paste from media library:');
    if (url) {
        editor.chain().focus().setImage({ src: url }).run();
    }
}

/**
 * Initialize all WYSIWYG fields on the page
 * @param {Object} config - Global configuration
 */
export function initAllWysiwygFields(config) {
    const fields = document.querySelectorAll('[data-field-type="wysiwyg"]');
    const editors = [];

    fields.forEach(field => {
        const fieldConfig = {
            developername: field.dataset.developername,
            imageMediaItems: config.imageMediaItems || [],
            videoMediaItems: config.videoMediaItems || [],
        };

        initWysiwygField(field, fieldConfig).then(editor => {
            if (editor) {
                editors.push(editor);
            }
        });
    });

    return editors;
}

