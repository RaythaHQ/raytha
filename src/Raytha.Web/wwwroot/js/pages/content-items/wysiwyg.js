/**
 * Content Items - WYSIWYG Field Module
 * Initializes TipTap WYSIWYG editors for content item fields
 * 
 * TipTap is loaded via CDN with an importmap in the page.
 * This module provides the initialization logic for each field instance.
 */

// Store modal instances and Uppy instances globally
const modalInstances = {};
const uppyInstances = {};

/**
 * Detect video provider from URL
 * @param {string} url - Video URL
 * @returns {Object} Provider info { provider, id }
 */
function detectVideoProvider(url) {
    const yt = /(?:youtube\.com\/watch\?v=|youtu\.be\/)([A-Za-z0-9_\-]+)/i.exec(url);
    if (yt) return { provider: 'youtube', id: yt[1] };
    const vm = /vimeo\.com\/(\d+)/i.exec(url);
    if (vm) return { provider: 'vimeo', id: vm[1] };
    return { provider: 'file', id: null };
}

/**
 * Custom Video Node for TipTap
 * Handles both direct video files and YouTube/Vimeo embeds
 */
function createVideoNode(Node) {
    return Node.create({
        name: 'video',
        group: 'block',
        atom: true,
        addAttributes() {
            return {
                src: { default: null },
                provider: { default: 'file' },
                videoId: { default: null },
                width: { default: 640 },
                height: { default: 480 },
            };
        },
        parseHTML() {
            return [
                { tag: 'video' },
                { tag: 'iframe[src*="youtube.com"]' },
                { tag: 'iframe[src*="youtu.be"]' },
                { tag: 'iframe[src*="vimeo.com"]' },
            ];
        },
        renderHTML({ node, HTMLAttributes }) {
            const { src, provider, videoId, width, height } = node.attrs;
            if (!src && !videoId) return ['div'];

            const videoWidth = width || 640;
            const videoHeight = height || 480;

            // Remove src from HTMLAttributes to prevent override
            const { src: _removedSrc, ...cleanHTMLAttributes } = HTMLAttributes;

            // Render YouTube embed
            if (provider === 'youtube' && videoId) {
                return ['iframe', {
                    ...cleanHTMLAttributes,
                    src: `https://www.youtube.com/embed/${videoId}`,
                    width: videoWidth,
                    height: videoHeight,
                    frameborder: '0',
                    allow: 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture',
                    allowfullscreen: 'true',
                    loading: 'lazy'
                }];
            }

            // Render Vimeo embed
            if (provider === 'vimeo' && videoId) {
                return ['iframe', {
                    ...cleanHTMLAttributes,
                    src: `https://player.vimeo.com/video/${videoId}`,
                    width: videoWidth,
                    height: videoHeight,
                    frameborder: '0',
                    allow: 'autoplay; fullscreen; picture-in-picture',
                    allowfullscreen: 'true',
                    loading: 'lazy'
                }];
            }

            // Regular video file
            return ['video', {
                ...cleanHTMLAttributes,
                src: src || '',
                width: videoWidth,
                height: videoHeight,
                controls: true
            }];
        },
    });
}

/**
 * Ensure modal exists in DOM (create once, reuse)
 * @param {string} id - Modal element ID
 * @param {string} htmlString - Modal HTML
 * @returns {HTMLElement} Modal element
 */
function ensureModal(id, htmlString) {
    let modal = document.getElementById(id);
    if (!modal) {
        const wrapper = document.createElement('div');
        wrapper.innerHTML = htmlString.trim();
        modal = wrapper.firstChild;
        document.body.appendChild(modal);
    }
    return modal;
}

/**
 * Create Link modal HTML
 * @returns {string} Modal HTML
 */
function createLinkModalHTML() {
    return `
        <div class="modal fade tiptap-modal" id="tiptap-link-modal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Insert link</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <ul class="nav nav-tabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <button class="nav-link active" id="link-general-tab" data-bs-toggle="tab" 
                                    data-bs-target="#link-general" type="button" role="tab">General</button>
                            </li>
                            <li class="nav-item" role="presentation">
                                <button class="nav-link" id="link-upload-tab" data-bs-toggle="tab" 
                                    data-bs-target="#link-upload" type="button" role="tab">Upload</button>
                            </li>
                        </ul>
                        <div class="tab-content">
                            <div class="tab-pane fade show active" id="link-general" role="tabpanel">
                                <div class="mb-3 mt-3">
                                    <label for="link-url" class="form-label">URL</label>
                                    <input type="text" class="form-control" id="link-url" placeholder="https://">
                                </div>
                                <div class="mb-3">
                                    <label for="link-text" class="form-label">Text to display</label>
                                    <input type="text" class="form-control" id="link-text" placeholder="Link text">
                                </div>
                                <div class="mb-3">
                                    <label for="link-title" class="form-label">Title</label>
                                    <input type="text" class="form-control" id="link-title" placeholder="Optional title">
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="link-new-window">
                                    <label class="form-check-label" for="link-new-window">
                                        Open in new window
                                    </label>
                                </div>
                            </div>
                            <div class="tab-pane fade" id="link-upload" role="tabpanel">
                                <div id="link-uppy-container" class="uppy-container mt-3"></div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-primary" id="link-insert-btn" disabled>Insert link</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Create Image modal HTML
 * @returns {string} Modal HTML
 */
function createImageModalHTML() {
    return `
        <div class="modal fade tiptap-modal" id="tiptap-image-modal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Insert image</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <ul class="nav nav-tabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <button class="nav-link active" id="image-general-tab" data-bs-toggle="tab" 
                                    data-bs-target="#image-general" type="button" role="tab">General</button>
                            </li>
                            <li class="nav-item" role="presentation">
                                <button class="nav-link" id="image-upload-tab" data-bs-toggle="tab" 
                                    data-bs-target="#image-upload" type="button" role="tab">Upload</button>
                            </li>
                        </ul>
                        <div class="tab-content">
                            <div class="tab-pane fade show active" id="image-general" role="tabpanel">
                                <div class="mb-3 mt-3">
                                    <label for="image-url" class="form-label">Image URL</label>
                                    <input type="text" class="form-control" id="image-url" placeholder="https://">
                                </div>
                                <div class="mb-3">
                                    <label for="image-alt" class="form-label">Alternative description</label>
                                    <input type="text" class="form-control" id="image-alt" placeholder="Image description">
                                </div>
                                <div class="row">
                                    <div class="col-6">
                                        <label for="image-width" class="form-label">Width</label>
                                        <input type="number" class="form-control" id="image-width" placeholder="640">
                                    </div>
                                    <div class="col-6">
                                        <label for="image-height" class="form-label">Height</label>
                                        <input type="number" class="form-control" id="image-height" placeholder="480">
                                    </div>
                                </div>
                            </div>
                            <div class="tab-pane fade" id="image-upload" role="tabpanel">
                                <div id="image-uppy-container" class="uppy-container mt-3"></div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-primary" id="image-insert-btn" disabled>Insert image</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Create Video modal HTML
 * @returns {string} Modal HTML
 */
function createVideoModalHTML() {
    return `
        <div class="modal fade tiptap-modal" id="tiptap-video-modal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Insert video</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <ul class="nav nav-tabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <button class="nav-link active" id="video-general-tab" data-bs-toggle="tab" 
                                    data-bs-target="#video-general" type="button" role="tab">General</button>
                            </li>
                            <li class="nav-item" role="presentation">
                                <button class="nav-link" id="video-upload-tab" data-bs-toggle="tab" 
                                    data-bs-target="#video-upload" type="button" role="tab">Upload</button>
                            </li>
                        </ul>
                        <div class="tab-content">
                            <div class="tab-pane fade show active" id="video-general" role="tabpanel">
                                <div class="mb-3 mt-3">
                                    <label for="video-url" class="form-label">Video URL (youtube, vimeo, file storage)</label>
                                    <input type="text" class="form-control" id="video-url" placeholder="https://">
                                </div>
                                <div class="row">
                                    <div class="col-6">
                                        <label for="video-width" class="form-label">Width</label>
                                        <input type="number" class="form-control" id="video-width" value="640">
                                    </div>
                                    <div class="col-6">
                                        <label for="video-height" class="form-label">Height</label>
                                        <input type="number" class="form-control" id="video-height" value="480">
                                    </div>
                                </div>
                            </div>
                            <div class="tab-pane fade" id="video-upload" role="tabpanel">
                                <div id="video-uppy-container" class="uppy-container mt-3"></div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-primary" id="video-insert-btn" disabled>Insert video</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Create Table modal HTML
 * @returns {string} Modal HTML
 */
function createTableModalHTML() {
    return `
        <div class="modal fade tiptap-modal" id="tiptap-table-modal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Insert table</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-6">
                                <label for="table-rows" class="form-label">Rows</label>
                                <input type="number" class="form-control" id="table-rows" value="3" min="1" max="20">
                            </div>
                            <div class="col-6">
                                <label for="table-cols" class="form-label">Columns</label>
                                <input type="number" class="form-control" id="table-cols" value="3" min="1" max="10">
                            </div>
                        </div>
                        <div class="form-check mt-3">
                            <input class="form-check-input" type="checkbox" id="table-header" checked>
                            <label class="form-check-label" for="table-header">
                                First row as header
                            </label>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-primary" id="table-insert-btn">Insert table</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Initialize Uppy for modal file upload
 * @param {string} containerId - Container element ID
 * @param {Object} config - Configuration
 * @returns {Promise<Object>} Uppy instance
 */
async function initUppyForModal(containerId, config) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Uppy container not found:', containerId);
        return null;
    }

    // Import Uppy from CDN (same version as attachment.js)
    const { Uppy, Dashboard, XHRUpload, AwsS3 } = await import('https://releases.transloadit.com/uppy/v4.13.3/uppy.min.mjs');

    // Parse allowed MIME types
    const allowedFileTypes = config.allowedFileTypes || null;

    // Initialize Uppy
    const uppy = new Uppy({
        autoProceed: true,
        allowMultipleUploads: false,
        restrictions: {
            maxFileSize: config.maxFileSize || null,
            allowedFileTypes: allowedFileTypes,
            maxNumberOfFiles: 1,
        },
    });

    // Add Dashboard UI
    uppy.use(Dashboard, {
        inline: true,
        target: container,
        height: 250,
        hideUploadButton: false,
        showProgressDetails: true,
        note: config.note || null,
    });

    if (!config.useDirectUploadToCloud) {
        // Direct upload to application server
        uppy.use(XHRUpload, {
            endpoint: `${config.pathBase}/raytha/media-items/upload`,
            fieldName: 'file',
        });

        uppy.on('upload-success', (file, response) => {
            const body = response && response.body ? response.body : {};
            const fields = body.fields || body;
            const objectKey = fields.objectKey || '';

            if (objectKey && config.onSuccess) {
                const fileUrl = `${config.pathBase}/raytha/media-items/objectkey/${objectKey}`;
                config.onSuccess(fileUrl, file);
            }
        });
    } else {
        // Cloud upload via presigned URL
        uppy.use(AwsS3, {
            getUploadParameters: async (file) => {
                const presignUrl = `${config.pathBase}/raytha/media-items/presign`;
                const ext = getFileExtension(file.name);

                const res = await fetch(presignUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        filename: file.name,
                        contentType: file.type,
                        extension: ext,
                    }),
                });

                const data = await res.json();

                // Store metadata for create-after-upload
                if (data && data.fields) {
                    file.meta.uploadId = data.fields.id;
                    file.meta.objectKey = data.fields.objectKey;
                }

                return {
                    method: 'PUT',
                    url: data.url,
                    fields: data.fields,
                    headers: { 'x-ms-blob-type': 'BlockBlob' },
                };
            },
        });

        uppy.on('upload-success', async (file, _response) => {
            try {
                const createUrl = `${config.pathBase}/raytha/media-items/create-after-upload`;
                const ext = getFileExtension(file.name);

                const payload = {
                    filename: file.name,
                    contentType: file.type,
                    extension: ext,
                    id: file.meta.uploadId,
                    objectKey: file.meta.objectKey,
                    length: file.size,
                };

                const res = await fetch(createUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload),
                });

                await res.json().catch(() => null);

                if (file.meta.objectKey && config.onSuccess) {
                    const fileUrl = `${config.pathBase}/raytha/media-items/objectkey/${file.meta.objectKey}`;
                    config.onSuccess(fileUrl, file);
                }
            } catch (err) {
                console.error('Error in create-after-upload:', err);
            }
        });
    }

    // Handle errors
    uppy.on('restriction-failed', (_file, error) => {
        console.error('File restriction:', error);
        if (config.onError) {
            config.onError(error.message);
        }
    });

    uppy.on('upload-error', (file, error, _response) => {
        console.error('Upload error:', file && file.id, error);
        if (config.onError) {
            config.onError('Upload failed. Please try again.');
        }
    });

    return uppy;
}

/**
 * Get file extension from filename
 * @param {string} filename - File name
 * @returns {string} File extension
 */
function getFileExtension(filename) {
    const i = (filename || '').lastIndexOf('.');
    return i >= 0 ? filename.substring(i + 1) : '';
}

/**
 * Show link context menu on right-click
 * @param {MouseEvent} e - Mouse event
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 */
function showLinkContextMenu(e, editor, config) {
    e.preventDefault();

    // Remove existing menu if any
    const existingMenu = document.querySelector('.tiptap-context-menu');
    if (existingMenu) {
        existingMenu.remove();
    }

    // Get link href
    const linkAttrs = editor.getAttributes('link');
    const href = linkAttrs.href;
    if (!href) return;

    // Create menu
    const menu = document.createElement('div');
    menu.className = 'tiptap-context-menu';
    menu.style.left = `${e.pageX}px`;
    menu.style.top = `${e.pageY}px`;

    menu.innerHTML = `
        <button type="button" class="context-menu-edit">Edit link</button>
        <button type="button" class="context-menu-unlink">Unlink</button>
        <button type="button" class="context-menu-open">Open link</button>
    `;

    document.body.appendChild(menu);

    // Edit link
    menu.querySelector('.context-menu-edit').addEventListener('click', () => {
        menu.remove();
        showLinkModal(editor, config, linkAttrs);
    });

    // Unlink
    menu.querySelector('.context-menu-unlink').addEventListener('click', () => {
        menu.remove();
        editor.chain().focus().extendMarkRange('link').unsetLink().run();
    });

    // Open link
    menu.querySelector('.context-menu-open').addEventListener('click', () => {
        menu.remove();
        window.open(href, '_blank', 'noopener,noreferrer');
    });

    // Close on outside click
    const closeMenu = (event) => {
        if (!menu.contains(event.target)) {
            menu.remove();
            document.removeEventListener('click', closeMenu);
        }
    };
    setTimeout(() => document.addEventListener('click', closeMenu), 0);

    // Close on Escape
    const closeOnEscape = (event) => {
        if (event.key === 'Escape') {
            menu.remove();
            document.removeEventListener('keydown', closeOnEscape);
        }
    };
    document.addEventListener('keydown', closeOnEscape);
}

/**
 * Show image context menu on right-click
 * @param {MouseEvent} e - Mouse event
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {HTMLElement} imgElement - Image element
 */
function showImageContextMenu(e, editor, config, imgElement) {
    e.preventDefault();

    // Remove existing menu if any
    const existingMenu = document.querySelector('.tiptap-context-menu');
    if (existingMenu) {
        existingMenu.remove();
    }

    // Create menu
    const menu = document.createElement('div');
    menu.className = 'tiptap-context-menu';
    menu.style.left = `${e.pageX}px`;
    menu.style.top = `${e.pageY}px`;

    menu.innerHTML = `
        <button type="button" class="context-menu-edit">Edit image</button>
    `;

    document.body.appendChild(menu);

    // Edit image
    menu.querySelector('.context-menu-edit').addEventListener('click', () => {
        menu.remove();
        const attrs = {
            src: imgElement.getAttribute('src'),
            alt: imgElement.getAttribute('alt') || '',
            width: imgElement.getAttribute('width') || imgElement.naturalWidth || '',
            height: imgElement.getAttribute('height') || imgElement.naturalHeight || ''
        };
        showImageModal(editor, config, attrs);
    });

    // Close on outside click
    const closeMenu = (event) => {
        if (!menu.contains(event.target)) {
            menu.remove();
            document.removeEventListener('click', closeMenu);
        }
    };
    setTimeout(() => document.addEventListener('click', closeMenu), 0);

    // Close on Escape
    const closeOnEscape = (event) => {
        if (event.key === 'Escape') {
            menu.remove();
            document.removeEventListener('keydown', closeOnEscape);
        }
    };
    document.addEventListener('keydown', closeOnEscape);
}

/**
 * Show video context menu on right-click
 * @param {MouseEvent} e - Mouse event
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {HTMLElement} videoElement - Video or iframe element
 */
function showVideoContextMenu(e, editor, config, videoElement) {
    e.preventDefault();

    // Remove existing menu if any
    const existingMenu = document.querySelector('.tiptap-context-menu');
    if (existingMenu) {
        existingMenu.remove();
    }

    // Create menu
    const menu = document.createElement('div');
    menu.className = 'tiptap-context-menu';
    menu.style.left = `${e.pageX}px`;
    menu.style.top = `${e.pageY}px`;

    menu.innerHTML = `
        <button type="button" class="context-menu-edit">Edit video</button>
    `;

    document.body.appendChild(menu);

    // Edit video
    menu.querySelector('.context-menu-edit').addEventListener('click', () => {
        menu.remove();
        // Find the video node in TipTap to get original attributes
        const pos = editor.view.posAtDOM(videoElement, 0);
        const $pos = editor.state.doc.resolve(pos);
        const node = $pos.parent;

        let attrs = {
            src: videoElement.getAttribute('src') || '',
            width: videoElement.getAttribute('width') || '640',
            height: videoElement.getAttribute('height') || '480'
        };

        // If we found the video node, use its attributes (includes original URL)
        if (node && node.type.name === 'video') {
            attrs = {
                src: node.attrs.src || '',
                width: node.attrs.width || '640',
                height: node.attrs.height || '480'
            };
        }

        showVideoModal(editor, config, attrs);
    });

    // Close on outside click
    const closeMenu = (event) => {
        if (!menu.contains(event.target)) {
            menu.remove();
            document.removeEventListener('click', closeMenu);
        }
    };
    setTimeout(() => document.addEventListener('click', closeMenu), 0);

    // Close on Escape
    const closeOnEscape = (event) => {
        if (event.key === 'Escape') {
            menu.remove();
            document.removeEventListener('keydown', closeOnEscape);
        }
    };
    document.addEventListener('keydown', closeOnEscape);
}

/**
 * Show link modal
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {Object} existingAttrs - Existing link attributes (for editing)
 */
function showLinkModal(editor, config, existingAttrs = null) {
    const modalEl = ensureModal('tiptap-link-modal', createLinkModalHTML());
    const modal = new bootstrap.Modal(modalEl);

    const urlInput = modalEl.querySelector('#link-url');
    const textInput = modalEl.querySelector('#link-text');
    const titleInput = modalEl.querySelector('#link-title');
    const newWindowCheckbox = modalEl.querySelector('#link-new-window');
    const insertBtn = modalEl.querySelector('#link-insert-btn');

    // Prefill for editing
    if (existingAttrs) {
        urlInput.value = existingAttrs.href || '';
        titleInput.value = existingAttrs.title || '';
        newWindowCheckbox.checked = existingAttrs.target === '_blank';
        textInput.value = '';  // Can't easily get selected text for editing
    } else {
        // Prefill with selection text
        const { from, to } = editor.state.selection;
        const text = editor.state.doc.textBetween(from, to, '');
        urlInput.value = '';
        textInput.value = text;
        titleInput.value = '';
        newWindowCheckbox.checked = false;
    }

    // Enable/disable insert button based on URL
    const validateForm = () => {
        insertBtn.disabled = !urlInput.value.trim();
    };
    urlInput.addEventListener('input', validateForm);
    validateForm();

    // Focus first input
    modalEl.addEventListener('shown.bs.modal', () => {
        urlInput.focus();
    });

    // Initialize Uppy for upload tab (only once)
    const uppyContainerId = 'link-uppy-container';
    if (!uppyInstances[uppyContainerId]) {
        initUppyForModal(uppyContainerId, {
            pathBase: config.pathBase || '',
            useDirectUploadToCloud: config.useDirectUploadToCloud || false,
            maxFileSize: config.maxFileSize || null,
            allowedFileTypes: null,  // Any file type for links
            note: 'Upload any file',
            onSuccess: (fileUrl, file) => {
                urlInput.value = fileUrl;
                if (!textInput.value) {
                    textInput.value = file.name;
                }
                validateForm();
                // Switch back to General tab
                const generalTab = modalEl.querySelector('#link-general-tab');
                bootstrap.Tab.getInstance(generalTab)?.show() || new bootstrap.Tab(generalTab).show();
            },
            onError: (message) => {
                alert(message);
            }
        }).then(uppy => {
            uppyInstances[uppyContainerId] = uppy;
        });
    }

    // Insert link handler
    const handleInsert = () => {
        const url = urlInput.value.trim();
        if (!url) return;

        const text = textInput.value.trim();
        const title = titleInput.value.trim();
        const openInNewWindow = newWindowCheckbox.checked;

        const linkAttrs = {
            href: url,
            ...(title && { title }),
            ...(openInNewWindow && { target: '_blank', rel: 'noopener noreferrer' })
        };

        if (existingAttrs) {
            // Update existing link
            editor.chain().focus().extendMarkRange('link').setLink(linkAttrs).run();
        } else {
            // Insert new link
            if (text && !editor.state.selection.empty) {
                // Replace selection with link
                editor.chain().focus().setLink(linkAttrs).run();
            } else if (text) {
                // Insert new text with link
                editor.chain().focus().insertContent(`<a href="${url}">${text}</a>`).run();
            } else {
                // Just use URL as text
                editor.chain().focus().insertContent(`<a href="${url}">${url}</a>`).run();
            }
        }

        modal.hide();
    };

    insertBtn.onclick = handleInsert;

    // Handle Enter key in inputs
    [urlInput, textInput, titleInput].forEach(input => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !insertBtn.disabled) {
                e.preventDefault();
                handleInsert();
            }
        });
    });

    modal.show();
}

/**
 * Show image modal
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {Object} existingAttrs - Existing image attributes (for editing)
 */
function showImageModal(editor, config, existingAttrs = null) {
    const modalEl = ensureModal('tiptap-image-modal', createImageModalHTML());
    const modal = new bootstrap.Modal(modalEl);

    const urlInput = modalEl.querySelector('#image-url');
    const altInput = modalEl.querySelector('#image-alt');
    const widthInput = modalEl.querySelector('#image-width');
    const heightInput = modalEl.querySelector('#image-height');
    const insertBtn = modalEl.querySelector('#image-insert-btn');

    // Prefill for editing or reset form
    if (existingAttrs) {
        urlInput.value = existingAttrs.src || '';
        altInput.value = existingAttrs.alt || '';
        widthInput.value = existingAttrs.width || '';
        heightInput.value = existingAttrs.height || '';
    } else {
        urlInput.value = '';
        altInput.value = '';
        widthInput.value = '';
        heightInput.value = '';
    }

    // Enable/disable insert button based on URL
    const validateForm = () => {
        insertBtn.disabled = !urlInput.value.trim();
    };
    urlInput.addEventListener('input', validateForm);
    validateForm();

    // Focus first input
    modalEl.addEventListener('shown.bs.modal', () => {
        urlInput.focus();
    });

    // Initialize Uppy for upload tab (only once)
    const uppyContainerId = 'image-uppy-container';
    if (!uppyInstances[uppyContainerId]) {
        initUppyForModal(uppyContainerId, {
            pathBase: config.pathBase || '',
            useDirectUploadToCloud: config.useDirectUploadToCloud || false,
            maxFileSize: config.maxFileSize || null,
            allowedFileTypes: ['image/*'],
            note: 'Images only',
            onSuccess: (fileUrl, file) => {
                urlInput.value = fileUrl;
                validateForm();
                // Switch back to General tab
                const generalTab = modalEl.querySelector('#image-general-tab');
                bootstrap.Tab.getInstance(generalTab)?.show() || new bootstrap.Tab(generalTab).show();
            },
            onError: (message) => {
                alert(message);
            }
        }).then(uppy => {
            uppyInstances[uppyContainerId] = uppy;
        });
    }

    // Insert image handler
    const handleInsert = () => {
        const url = urlInput.value.trim();
        if (!url) return;

        const attrs = {
            src: url,
            ...(altInput.value && { alt: altInput.value }),
            ...(widthInput.value && { width: parseInt(widthInput.value) }),
            ...(heightInput.value && { height: parseInt(heightInput.value) })
        };

        editor.chain().focus().setImage(attrs).run();
        modal.hide();
    };

    insertBtn.onclick = handleInsert;

    // Handle Enter key in URL input
    urlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !insertBtn.disabled) {
            e.preventDefault();
            handleInsert();
        }
    });

    modal.show();
}

/**
 * Show video modal
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {Object} existingAttrs - Existing video attributes (for editing)
 */
function showVideoModal(editor, config, existingAttrs = null) {
    const modalEl = ensureModal('tiptap-video-modal', createVideoModalHTML());
    const modal = new bootstrap.Modal(modalEl);

    const urlInput = modalEl.querySelector('#video-url');
    const widthInput = modalEl.querySelector('#video-width');
    const heightInput = modalEl.querySelector('#video-height');
    const insertBtn = modalEl.querySelector('#video-insert-btn');

    // Prefill for editing or reset form
    if (existingAttrs) {
        urlInput.value = existingAttrs.src || '';
        widthInput.value = existingAttrs.width || '640';
        heightInput.value = existingAttrs.height || '480';
    } else {
        urlInput.value = '';
        widthInput.value = '640';
        heightInput.value = '480';
    }

    // Enable/disable insert button based on URL
    const validateForm = () => {
        insertBtn.disabled = !urlInput.value.trim();
    };
    urlInput.addEventListener('input', validateForm);
    validateForm();

    // Focus first input
    modalEl.addEventListener('shown.bs.modal', () => {
        urlInput.focus();
    });

    // Initialize Uppy for upload tab (only once)
    const uppyContainerId = 'video-uppy-container';
    if (!uppyInstances[uppyContainerId]) {
        initUppyForModal(uppyContainerId, {
            pathBase: config.pathBase || '',
            useDirectUploadToCloud: config.useDirectUploadToCloud || false,
            maxFileSize: config.maxFileSize || null,
            allowedFileTypes: ['video/*'],
            note: 'Videos only',
            onSuccess: (fileUrl, file) => {
                urlInput.value = fileUrl;
                validateForm();
                // Switch back to General tab
                const generalTab = modalEl.querySelector('#video-general-tab');
                bootstrap.Tab.getInstance(generalTab)?.show() || new bootstrap.Tab(generalTab).show();
            },
            onError: (message) => {
                alert(message);
            }
        }).then(uppy => {
            uppyInstances[uppyContainerId] = uppy;
        });
    }

    // Insert video handler
    const handleInsert = () => {
        const url = urlInput.value.trim();
        if (!url) return;

        // Detect provider and extract video ID
        const providerInfo = detectVideoProvider(url);

        const attrs = {
            src: url,  // Always store the original URL
            provider: providerInfo.provider,
            videoId: providerInfo.id,
            width: parseInt(widthInput.value) || 640,
            height: parseInt(heightInput.value) || 480
        };

        editor.commands.insertContent({
            type: 'video',
            attrs
        });

        modal.hide();
    };

    insertBtn.onclick = handleInsert;

    // Handle Enter key in URL input
    urlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !insertBtn.disabled) {
            e.preventDefault();
            handleInsert();
        }
    });

    modal.show();
}

/**
 * Show table modal
 * @param {Object} editor - TipTap editor instance
 */
function showTableModal(editor) {
    const modalEl = ensureModal('tiptap-table-modal', createTableModalHTML());
    const modal = new bootstrap.Modal(modalEl);

    const rowsInput = modalEl.querySelector('#table-rows');
    const colsInput = modalEl.querySelector('#table-cols');
    const headerCheckbox = modalEl.querySelector('#table-header');
    const insertBtn = modalEl.querySelector('#table-insert-btn');

    // Focus first input
    modalEl.addEventListener('shown.bs.modal', () => {
        rowsInput.focus();
        rowsInput.select();
    });

    // Insert table handler
    const handleInsert = () => {
        const rows = parseInt(rowsInput.value) || 3;
        const cols = parseInt(colsInput.value) || 3;
        const withHeaderRow = headerCheckbox.checked;

        editor.chain().focus().insertTable({ rows, cols, withHeaderRow }).run();
        modal.hide();
    };

    insertBtn.onclick = handleInsert;

    // Handle Enter key in inputs
    [rowsInput, colsInput].forEach(input => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                handleInsert();
            }
        });
    });

    modal.show();
}

/**
 * Show table context menu on right-click
 * @param {MouseEvent} e - Mouse event
 * @param {Object} editor - TipTap editor instance
 */
function showTableContextMenu(e, editor) {
    e.preventDefault();

    // Remove existing menu if any
    const existingMenu = document.querySelector('.tiptap-context-menu');
    if (existingMenu) {
        existingMenu.remove();
    }

    // Create menu
    const menu = document.createElement('div');
    menu.className = 'tiptap-context-menu';
    menu.style.left = `${e.pageX}px`;
    menu.style.top = `${e.pageY}px`;

    menu.innerHTML = `
        <button type="button" class="context-menu-item" data-action="addRowBefore">Insert row above</button>
        <button type="button" class="context-menu-item" data-action="addRowAfter">Insert row below</button>
        <button type="button" class="context-menu-item" data-action="deleteRow">Delete row</button>
        <hr class="my-1">
        <button type="button" class="context-menu-item" data-action="addColumnBefore">Insert column left</button>
        <button type="button" class="context-menu-item" data-action="addColumnAfter">Insert column right</button>
        <button type="button" class="context-menu-item" data-action="deleteColumn">Delete column</button>
        <hr class="my-1">
        <button type="button" class="context-menu-item" data-action="deleteTable">Delete table</button>
    `;

    document.body.appendChild(menu);

    // Handle menu item clicks
    menu.addEventListener('click', (event) => {
        const button = event.target.closest('[data-action]');
        if (!button) return;

        const action = button.dataset.action;
        menu.remove();

        switch (action) {
            case 'addRowBefore':
                editor.chain().focus().addRowBefore().run();
                break;
            case 'addRowAfter':
                editor.chain().focus().addRowAfter().run();
                break;
            case 'deleteRow':
                editor.chain().focus().deleteRow().run();
                break;
            case 'addColumnBefore':
                editor.chain().focus().addColumnBefore().run();
                break;
            case 'addColumnAfter':
                editor.chain().focus().addColumnAfter().run();
                break;
            case 'deleteColumn':
                editor.chain().focus().deleteColumn().run();
                break;
            case 'deleteTable':
                editor.chain().focus().deleteTable().run();
                break;
        }
    });

    // Close on outside click
    const closeMenu = (event) => {
        if (!menu.contains(event.target)) {
            menu.remove();
            document.removeEventListener('click', closeMenu);
        }
    };
    setTimeout(() => document.addEventListener('click', closeMenu), 0);

    // Close on Escape
    const closeOnEscape = (event) => {
        if (event.key === 'Escape') {
            menu.remove();
            document.removeEventListener('keydown', closeOnEscape);
        }
    };
    document.addEventListener('keydown', closeOnEscape);
}

/**
 * Create HTML Source modal HTML
 * @returns {string} Modal HTML
 */
function createHTMLSourceModalHTML() {
    return `
        <div class="modal fade tiptap-modal" id="tiptap-html-modal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Edit HTML source</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <textarea id="html-source-textarea" class="form-control" rows="15" style="font-family: monospace;"></textarea>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" id="html-save-btn">Save</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Show HTML source modal
 * @param {Object} editor - TipTap editor instance
 */
function showHTMLSourceModal(editor) {
    const modalEl = ensureModal('tiptap-html-modal', createHTMLSourceModalHTML());
    const modal = new bootstrap.Modal(modalEl);
    const textarea = modalEl.querySelector('#html-source-textarea');
    const saveBtn = modalEl.querySelector('#html-save-btn');

    textarea.value = editor.getHTML();

    modalEl.addEventListener('shown.bs.modal', () => {
        textarea.focus();
    });

    saveBtn.onclick = () => {
        const html = textarea.value.trim();
        editor.commands.setContent(html, false);
        modal.hide();
    };

    // Handle Escape key
    textarea.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            modal.hide();
        }
    });

    modal.show();
}

/**
 * Show color picker popover
 * @param {Object} editor - TipTap editor instance
 * @param {HTMLElement} button - Button element
 * @param {string} type - 'text' or 'background'
 */
function showColorPicker(editor, button, type) {
    console.log('showColorPicker called', { button, type });

    if (!button) {
        console.error('Button is null or undefined');
        return;
    }

    // Remove existing picker
    const existing = document.querySelector('.color-picker-popover');
    if (existing) {
        existing.remove();
    }

    const colors = [
        '#000000', '#444444', '#666666', '#999999', '#CCCCCC', '#EEEEEE', '#F3F3F3', '#FFFFFF',
        '#FF0000', '#FF9900', '#FFFF00', '#00FF00', '#00FFFF', '#0000FF', '#9900FF', '#FF00FF',
        '#F4CCCC', '#FCE5CD', '#FFF2CC', '#D9EAD3', '#D0E0E3', '#CFE2F3', '#D9D2E9', '#EAD1DC',
        '#EA9999', '#F9CB9C', '#FFE599', '#B6D7A8', '#A2C4C9', '#9FC5E8', '#B4A7D6', '#D5A6BD'
    ];

    const picker = document.createElement('div');
    picker.className = 'color-picker-popover';

    let html = '<div class="color-grid">';
    colors.forEach(color => {
        html += `<button type="button" class="color-swatch" data-color="${color}" style="background:${color};" title="${color}"></button>`;
    });
    html += '</div>';
    html += '<button type="button" class="btn btn-sm btn-link text-danger mt-2 w-100" data-action="remove">× Remove color</button>';

    picker.innerHTML = html;

    // Position picker
    const rect = button.getBoundingClientRect();
    picker.style.left = `${rect.left}px`;
    picker.style.top = `${rect.bottom + 5}px`;

    console.log('Appending picker to body', { left: rect.left, top: rect.bottom + 5 });
    document.body.appendChild(picker);

    // Handle swatch clicks
    picker.querySelectorAll('.color-swatch').forEach(swatch => {
        swatch.addEventListener('click', (e) => {
            e.stopPropagation();
            const color = swatch.dataset.color;
            if (type === 'text') {
                editor.chain().focus().setColor(color).run();
            } else {
                editor.chain().focus().toggleHighlight({ color }).run();
            }
            picker.remove();
        });
    });

    // Handle remove button
    picker.querySelector('[data-action="remove"]').addEventListener('click', (e) => {
        e.stopPropagation();
        if (type === 'text') {
            editor.chain().focus().unsetColor().run();
        } else {
            editor.chain().focus().unsetHighlight().run();
        }
        picker.remove();
    });

    // Close on outside click - use longer delay to ensure click event has fully propagated
    setTimeout(() => {
        const closeOnClick = (e) => {
            if (!picker.contains(e.target) && !button.contains(e.target)) {
                console.log('Closing picker - clicked outside');
                picker.remove();
                document.removeEventListener('click', closeOnClick);
            }
        };
        document.addEventListener('click', closeOnClick);
    }, 100);

    // Close on Escape
    const closeOnEscape = (e) => {
        if (e.key === 'Escape') {
            picker.remove();
            document.removeEventListener('keydown', closeOnEscape);
        }
    };
    document.addEventListener('keydown', closeOnEscape);
}

/**
 * Create FontSize extension for TipTap
 * @param {Object} Extension - TipTap Extension class
 * @returns {Object} FontSize extension
 */
function createFontSizeExtension(Extension) {
    return Extension.create({
        name: 'fontSize',
        addOptions() {
            return {
                types: ['textStyle'],
            };
        },
        addGlobalAttributes() {
            return [
                {
                    types: this.options.types,
                    attributes: {
                        fontSize: {
                            default: null,
                            parseHTML: element => element.style.fontSize || null,
                            renderHTML: attributes => {
                                if (!attributes.fontSize) {
                                    return {};
                                }
                                return {
                                    style: `font-size: ${attributes.fontSize}`,
                                };
                            },
                        },
                    },
                },
            ];
        },
    });
}

/**
 * Initialize a WYSIWYG field with TipTap editor
 * @param {HTMLElement} fieldElement - The field container element
 * @param {Object} config - Configuration object
 * @param {string} config.developername - Field developer name
 * @param {string} config.initialValue - Initial editor content
 * @param {string} config.pathBase - URL base path
 * @param {boolean} config.useDirectUploadToCloud - Upload mode
 * @param {number} config.maxFileSize - Max file size
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
    const { Editor, Node, Extension } = await import('https://esm.sh/@tiptap/core@2.1.13');
    const StarterKit = await import('https://esm.sh/@tiptap/starter-kit@2.1.13');
    const Link = await import('https://esm.sh/@tiptap/extension-link@2.1.13');
    const Image = await import('https://esm.sh/@tiptap/extension-image@2.1.13');
    const Table = await import('https://esm.sh/@tiptap/extension-table@2.1.13');
    const TableRow = await import('https://esm.sh/@tiptap/extension-table-row@2.1.13');
    const TableCell = await import('https://esm.sh/@tiptap/extension-table-cell@2.1.13');
    const TableHeader = await import('https://esm.sh/@tiptap/extension-table-header@2.1.13');
    const Underline = await import('https://esm.sh/@tiptap/extension-underline@2.1.13');
    const Subscript = await import('https://esm.sh/@tiptap/extension-subscript@2.1.13');
    const Superscript = await import('https://esm.sh/@tiptap/extension-superscript@2.1.13');
    const TextStyle = await import('https://esm.sh/@tiptap/extension-text-style@2.1.13');
    const FontFamily = await import('https://esm.sh/@tiptap/extension-font-family@2.1.13');
    const TextAlign = await import('https://esm.sh/@tiptap/extension-text-align@2.1.13');
    const Color = await import('https://esm.sh/@tiptap/extension-color@2.1.13');
    const Highlight = await import('https://esm.sh/@tiptap/extension-highlight@2.1.13');

    // Create custom Video node
    const Video = createVideoNode(Node);

    // Create custom FontSize extension
    const FontSize = createFontSizeExtension(Extension);

    // Create toolbar
    const toolbar = document.createElement('div');
    toolbar.className = 'tiptap-toolbar d-flex flex-wrap align-items-center gap-1 border border-bottom-0 p-2';
    toolbar.innerHTML = `
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="bold" title="Bold">
            <strong>B</strong>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="italic" title="Italic">
            <em>I</em>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="underline" title="Underline">
            <u>U</u>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="strike" title="Strikethrough">
            <s>S</s>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="subscript" title="Subscript">
            X<sub>2</sub>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="superscript" title="Superscript">
            X<sup>2</sup>
        </button>
        <span class="separator"></span>
        <select class="form-select form-select-sm d-inline-block w-auto me-1" data-action="heading" title="Paragraph style">
            <option value="paragraph">Paragraph</option>
            <option value="1">Heading 1</option>
            <option value="2">Heading 2</option>
            <option value="3">Heading 3</option>
            <option value="4">Heading 4</option>
            <option value="5">Heading 5</option>
            <option value="6">Heading 6</option>
        </select>
        <select class="form-select form-select-sm d-inline-block w-auto me-1" data-action="font-family" title="Font family">
            <option value="">Default</option>
            <option value="Arial, Helvetica, sans-serif">Arial</option>
            <option value="Arial Black, Gadget, sans-serif">Arial Black</option>
            <option value="Comic Sans MS, cursive, sans-serif">Comic Sans MS</option>
            <option value="Courier New, Courier, monospace">Courier New</option>
            <option value="Georgia, serif">Georgia</option>
            <option value="Impact, Charcoal, sans-serif">Impact</option>
            <option value="Times New Roman, Times, serif">Times New Roman</option>
            <option value="Trebuchet MS, Helvetica, sans-serif">Trebuchet MS</option>
            <option value="Verdana, Geneva, sans-serif">Verdana</option>
        </select>
        <select class="form-select form-select-sm d-inline-block w-auto me-1" data-action="font-size" title="Font size">
            <option value="">Default</option>
            <option value="8px">8px</option>
            <option value="9px">9px</option>
            <option value="10px">10px</option>
            <option value="11px">11px</option>
            <option value="12px">12px</option>
            <option value="14px">14px</option>
            <option value="16px">16px</option>
            <option value="18px">18px</option>
            <option value="20px">20px</option>
            <option value="24px">24px</option>
            <option value="28px">28px</option>
            <option value="32px">32px</option>
            <option value="36px">36px</option>
            <option value="40px">40px</option>
            <option value="44px">44px</option>
            <option value="48px">48px</option>
        </select>
        <button type="button" class="btn btn-sm btn-outline-secondary" id="text-color-btn" title="Text Color">
            <span style="text-decoration: underline; text-decoration-thickness: 3px; text-decoration-color: currentColor;">A</span>
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" id="bg-color-btn" title="Background Color">
            <span style="background: #ffeb3b; padding: 0 4px;">A</span>
        </button>
        <div class="btn-group">
            <button type="button" class="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown" id="align-dropdown-btn" title="Text Align">
                <span id="align-icon">≡</span>
            </button>
            <ul class="dropdown-menu" id="align-dropdown-menu">
                <li><a class="dropdown-item" href="#" data-align="left">Left <span class="align-check float-end">✓</span></a></li>
                <li><a class="dropdown-item" href="#" data-align="center">Center <span class="align-check float-end">✓</span></a></li>
                <li><a class="dropdown-item" href="#" data-align="right">Right <span class="align-check float-end">✓</span></a></li>
                <li><a class="dropdown-item" href="#" data-align="justify">Justify <span class="align-check float-end">✓</span></a></li>
            </ul>
        </div>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="bullet-list" title="Bullet List">
            •
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="ordered-list" title="Numbered List">
            1.
        </button>
        <span class="separator"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="blockquote" title="Quote">
            "
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="code-block" title="Code Block">
            &lt;/&gt;
        </button>
        <span class="separator"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="table" title="Insert Table">
            ⚏
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="link" title="Insert Link">
            🔗
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="image" title="Insert Image">
            🖼️
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="video" title="Insert Video">
            🎬
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="html-source" title="Edit HTML Source">
            &lt;/&gt; HTML
        </button>
        <span class="separator"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="undo" title="Undo">
            ↶
        </button>
        <button type="button" class="btn btn-sm btn-outline-secondary" data-action="redo" title="Redo">
            ↷
        </button>
    `;

    // Create editor wrapper
    const editorWrapper = document.createElement('div');
    editorWrapper.className = 'tiptap-editor-wrapper';

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
                    class: 'link-primary',
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
            Underline.default,
            Subscript.default,
            Superscript.default,
            TextStyle.default,
            FontFamily.default,
            FontSize,
            TextAlign.default.configure({
                types: ['heading', 'paragraph'],
            }),
            Color.default,
            Highlight.default.configure({
                multicolor: true,
            }),
            Video,
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

    // Toolbar button actions
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
            case 'underline':
                editor.chain().focus().toggleUnderline().run();
                break;
            case 'strike':
                editor.chain().focus().toggleStrike().run();
                break;
            case 'subscript':
                editor.chain().focus().toggleSubscript().run();
                break;
            case 'superscript':
                editor.chain().focus().toggleSuperscript().run();
                break;
            case 'bullet-list':
                editor.chain().focus().toggleBulletList().run();
                break;
            case 'ordered-list':
                editor.chain().focus().toggleOrderedList().run();
                break;
            case 'blockquote':
                // Fix: use setBlockquote for empty selection, toggleBlockquote otherwise
                if (editor.state.selection.empty) {
                    editor.chain().focus().setBlockquote().run();
                } else {
                    editor.chain().focus().toggleBlockquote().run();
                }
                break;
            case 'code-block':
                editor.chain().focus().toggleCodeBlock().run();
                break;
            case 'table':
                showTableModal(editor);
                break;
            case 'link':
                showLinkModal(editor, config);
                break;
            case 'image':
                showImageModal(editor, config);
                break;
            case 'video':
                showVideoModal(editor, config);
                break;
            case 'html-source':
                showHTMLSourceModal(editor);
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

    // Toolbar select actions
    toolbar.addEventListener('change', (e) => {
        const select = e.target.closest('select[data-action]');
        if (!select) return;

        const action = select.dataset.action;
        const value = select.value;

        switch (action) {
            case 'heading':
                if (value === 'paragraph') {
                    editor.chain().focus().setParagraph().run();
                } else {
                    editor.chain().focus().setHeading({ level: parseInt(value) }).run();
                }
                break;
            case 'font-family':
                if (value) {
                    editor.chain().focus().setFontFamily(value).run();
                } else {
                    editor.chain().focus().unsetFontFamily().run();
                }
                break;
            case 'font-size':
                if (!value) {
                    editor.chain().focus().unsetMark('textStyle').run();
                } else {
                    editor.chain().focus().setMark('textStyle', { fontSize: value }).run();
                }
                break;
        }

        updateToolbarState(toolbar, editor);
    });

    // Update toolbar state when selection changes
    editor.on('selectionUpdate', () => {
        updateToolbarState(toolbar, editor);
    });

    // Text color button - use event delegation on toolbar
    toolbar.addEventListener('click', (e) => {
        const textColorBtn = e.target.closest('#text-color-btn');
        if (textColorBtn) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Text color button clicked');
            showColorPicker(editor, textColorBtn, 'text');
            return;
        }

        const bgColorBtn = e.target.closest('#bg-color-btn');
        if (bgColorBtn) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Background color button clicked');
            showColorPicker(editor, bgColorBtn, 'background');
            return;
        }
    });

    // Align dropdown handler
    const alignDropdownMenu = toolbar.querySelector('#align-dropdown-menu');
    if (alignDropdownMenu) {
        alignDropdownMenu.addEventListener('click', (e) => {
            const link = e.target.closest('a[data-align]');
            if (!link) return;

            e.preventDefault();
            const align = link.dataset.align;
            editor.chain().focus().setTextAlign(align).run();
            updateToolbarState(toolbar, editor);
        });
    }

    // Right-click context menu for links, images, and videos
    editorWrapper.addEventListener('contextmenu', (e) => {
        const target = e.target;

        // Check for link
        if ((target.tagName === 'A' || target.closest('a')) && editor.isActive('link')) {
            showLinkContextMenu(e, editor, config);
            return;
        }

        // Check for image
        if (target.tagName === 'IMG') {
            showImageContextMenu(e, editor, config, target);
            return;
        }

        // Check for video (iframe or video element)
        if (target.tagName === 'VIDEO' || target.tagName === 'IFRAME') {
            showVideoContextMenu(e, editor, config, target);
            return;
        }

        // Check for table (TD or TH elements)
        if (target.tagName === 'TD' || target.tagName === 'TH' || target.closest('td') || target.closest('th')) {
            showTableContextMenu(e, editor);
            return;
        }
    });

    // Video URL paste handler
    editor.on('paste', (event) => {
        const clipboardData = event.event.clipboardData;
        if (!clipboardData) return;

        const text = clipboardData.getData('text/plain');
        if (!text) return;

        const { provider } = detectVideoProvider(text);
        if (provider !== 'file') {
            const { from, to } = editor.state.selection;
            const isEmpty = editor.state.doc.textBetween(from, to, ' ').trim() === '';

            if (isEmpty) {
                event.preventDefault();
                showVideoModal(editor, config, { src: text });
            }
        }
    });

    return editor;
}

/**
 * Update toolbar button active states based on current editor state
 * @param {HTMLElement} toolbar - Toolbar element
 * @param {Object} editor - TipTap editor instance
 */
function updateToolbarState(toolbar, editor) {
    // Update buttons
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
            case 'underline':
                isActive = editor.isActive('underline');
                break;
            case 'strike':
                isActive = editor.isActive('strike');
                break;
            case 'subscript':
                isActive = editor.isActive('subscript');
                break;
            case 'superscript':
                isActive = editor.isActive('superscript');
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
            case 'table':
                isActive = editor.isActive('table');
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

    // Update heading dropdown
    const headingSelect = toolbar.querySelector('select[data-action="heading"]');
    if (headingSelect) {
        if (editor.isActive('heading', { level: 1 })) {
            headingSelect.value = '1';
        } else if (editor.isActive('heading', { level: 2 })) {
            headingSelect.value = '2';
        } else if (editor.isActive('heading', { level: 3 })) {
            headingSelect.value = '3';
        } else if (editor.isActive('heading', { level: 4 })) {
            headingSelect.value = '4';
        } else if (editor.isActive('heading', { level: 5 })) {
            headingSelect.value = '5';
        } else if (editor.isActive('heading', { level: 6 })) {
            headingSelect.value = '6';
        } else {
            headingSelect.value = 'paragraph';
        }
    }

    // Update font family dropdown
    const fontFamilySelect = toolbar.querySelector('select[data-action="font-family"]');
    if (fontFamilySelect) {
        const currentFontFamily = editor.getAttributes('textStyle').fontFamily;
        fontFamilySelect.value = currentFontFamily || '';
    }

    // Update font size dropdown
    const fontSizeSelect = toolbar.querySelector('select[data-action="font-size"]');
    if (fontSizeSelect) {
        const currentFontSize = editor.getAttributes('textStyle').fontSize;
        fontSizeSelect.value = currentFontSize || '';
    }

    // Update align dropdown checkmarks
    const alignDropdownMenu = toolbar.querySelector('#align-dropdown-menu');
    if (alignDropdownMenu) {
        const alignLinks = alignDropdownMenu.querySelectorAll('a[data-align]');
        alignLinks.forEach(link => {
            const align = link.dataset.align;
            const check = link.querySelector('.align-check');
            if (check) {
                if (editor.isActive({ textAlign: align })) {
                    check.classList.add('active');
                    check.style.visibility = 'visible';
                } else {
                    check.classList.remove('active');
                    check.style.visibility = 'hidden';
                }
            }
        });

        // Update dropdown button icon
        const alignIcon = toolbar.querySelector('#align-icon');
        if (alignIcon) {
            if (editor.isActive({ textAlign: 'left' })) {
                alignIcon.textContent = '≡';
            } else if (editor.isActive({ textAlign: 'center' })) {
                alignIcon.textContent = '≣';
            } else if (editor.isActive({ textAlign: 'right' })) {
                alignIcon.textContent = '≡';
            } else if (editor.isActive({ textAlign: 'justify' })) {
                alignIcon.textContent = '≣';
            } else {
                alignIcon.textContent = '≡';
            }
        }
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
            pathBase: config.pathBase || '',
            useDirectUploadToCloud: config.useDirectUploadToCloud || false,
            maxFileSize: config.maxFileSize || null,
        };

        initWysiwygField(field, fieldConfig).then(editor => {
            if (editor) {
                editors.push(editor);
            }
        });
    });

    return editors;
}
