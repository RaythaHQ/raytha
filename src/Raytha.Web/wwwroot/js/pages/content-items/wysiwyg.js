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
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="link-noopener">
                                    <label class="form-check-label" for="link-noopener">
                                        No opener (prevents access to window.opener)
                                    </label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="link-noreferrer">
                                    <label class="form-check-label" for="link-noreferrer">
                                        No referrer (don't send referrer information)
                                    </label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="link-nofollow">
                                    <label class="form-check-label" for="link-nofollow">
                                        No follow (don't follow this link for SEO)
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

    // Import Uppy from local library (same version as attachment.js)
    const { Uppy, Dashboard, XHRUpload, AwsS3 } = await import('/raytha_admin/lib/uppy/dist/uppy.min.mjs');

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
 * Check if an image is inside a link
 * @param {Object} editor - TipTap editor instance
 * @param {HTMLElement} imgElement - Image element
 * @returns {Object|null} Link attributes if linked, null otherwise
 */
function getImageLinkAttributes(editor, imgElement) {
    try {
        const pos = editor.view.posAtDOM(imgElement, 0);
        const $pos = editor.state.doc.resolve(pos);
        
        // Check if the image is wrapped in a link mark
        const marks = $pos.marks();
        const linkMark = marks.find(mark => mark.type.name === 'link');
        
        if (linkMark) {
            return linkMark.attrs;
        }
        
        return null;
    } catch (e) {
        console.error('Error getting image link attributes:', e);
        return null;
    }
}

/**
 * Wrap an image with a link
 * @param {Object} editor - TipTap editor instance
 * @param {HTMLElement} imgElement - Image element
 * @param {Object} linkAttrs - Link attributes
 */
function linkImage(editor, imgElement, linkAttrs) {
    try {
        const pos = editor.view.posAtDOM(imgElement, 0);
        const $pos = editor.state.doc.resolve(pos);
        const imageNode = $pos.nodeAfter;
        
        if (imageNode && imageNode.type.name === 'image') {
            const { tr } = editor.state;
            const from = pos;
            const to = pos + imageNode.nodeSize;
            
            // Add link mark to the image
            tr.addMark(from, to, editor.schema.marks.link.create(linkAttrs));
            editor.view.dispatch(tr);
        }
    } catch (e) {
        console.error('Error linking image:', e);
    }
}

/**
 * Remove link from an image
 * @param {Object} editor - TipTap editor instance
 * @param {HTMLElement} imgElement - Image element
 */
function unlinkImage(editor, imgElement) {
    try {
        const pos = editor.view.posAtDOM(imgElement, 0);
        const $pos = editor.state.doc.resolve(pos);
        const imageNode = $pos.nodeAfter;
        
        if (imageNode && imageNode.type.name === 'image') {
            const { tr } = editor.state;
            const from = pos;
            const to = pos + imageNode.nodeSize;
            
            // Remove link mark from the image
            const linkMark = editor.schema.marks.link;
            tr.removeMark(from, to, linkMark);
            editor.view.dispatch(tr);
        }
    } catch (e) {
        console.error('Error unlinking image:', e);
    }
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

    // Check if image is already linked
    const linkAttrs = getImageLinkAttributes(editor, imgElement);
    const isLinked = !!linkAttrs;

    // Create menu
    const menu = document.createElement('div');
    menu.className = 'tiptap-context-menu';
    menu.style.left = `${e.pageX}px`;
    menu.style.top = `${e.pageY}px`;

    let menuItems = '<button type="button" class="context-menu-edit">Edit image</button>';
    
    if (isLinked) {
        menuItems += '<button type="button" class="context-menu-edit-link">Edit link</button>';
        menuItems += '<button type="button" class="context-menu-unlink">Unlink image</button>';
        menuItems += '<button type="button" class="context-menu-open-link">Open link</button>';
    } else {
        menuItems += '<button type="button" class="context-menu-link">Link image</button>';
    }

    menu.innerHTML = menuItems;
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

    // Link image (if not linked)
    const linkBtn = menu.querySelector('.context-menu-link');
    if (linkBtn) {
        linkBtn.addEventListener('click', () => {
            menu.remove();
            showImageLinkModal(editor, config, imgElement, null);
        });
    }

    // Edit link (if linked)
    const editLinkBtn = menu.querySelector('.context-menu-edit-link');
    if (editLinkBtn) {
        editLinkBtn.addEventListener('click', () => {
            menu.remove();
            showImageLinkModal(editor, config, imgElement, linkAttrs);
        });
    }

    // Unlink image (if linked)
    const unlinkBtn = menu.querySelector('.context-menu-unlink');
    if (unlinkBtn) {
        unlinkBtn.addEventListener('click', () => {
            menu.remove();
            unlinkImage(editor, imgElement);
        });
    }

    // Open link (if linked)
    const openLinkBtn = menu.querySelector('.context-menu-open-link');
    if (openLinkBtn) {
        openLinkBtn.addEventListener('click', () => {
            menu.remove();
            if (linkAttrs && linkAttrs.href) {
                window.open(linkAttrs.href, '_blank', 'noopener,noreferrer');
            }
        });
    }

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
 * Show image link modal
 * @param {Object} editor - TipTap editor instance
 * @param {Object} config - Configuration
 * @param {HTMLElement} imgElement - Image element
 * @param {Object} existingAttrs - Existing link attributes (for editing)
 */
function showImageLinkModal(editor, config, imgElement, existingAttrs = null) {
    // Reuse the link modal HTML but with a different title
    const modalEl = ensureModal('tiptap-link-modal', createLinkModalHTML());
    
    // Update title
    const modalTitle = modalEl.querySelector('.modal-title');
    modalTitle.textContent = existingAttrs ? 'Edit image link' : 'Link image';
    
    const modal = new bootstrap.Modal(modalEl);

    const urlInput = modalEl.querySelector('#link-url');
    const textInput = modalEl.querySelector('#link-text');
    const titleInput = modalEl.querySelector('#link-title');
    const newWindowCheckbox = modalEl.querySelector('#link-new-window');
    const noopenerCheckbox = modalEl.querySelector('#link-noopener');
    const noreferrerCheckbox = modalEl.querySelector('#link-noreferrer');
    const nofollowCheckbox = modalEl.querySelector('#link-nofollow');
    const insertBtn = modalEl.querySelector('#link-insert-btn');

    // Hide text input and upload tab for image links (not needed)
    textInput.closest('.mb-3').style.display = 'none';
    const uploadTab = modalEl.querySelector('#link-upload-tab');
    if (uploadTab) {
        uploadTab.closest('.nav-item').style.display = 'none';
    }

    // Prefill for editing
    if (existingAttrs) {
        urlInput.value = existingAttrs.href || '';
        titleInput.value = existingAttrs.title || '';
        newWindowCheckbox.checked = existingAttrs.target === '_blank';
        
        // Parse rel attribute for noopener, noreferrer, and nofollow
        const rel = existingAttrs.rel || '';
        noopenerCheckbox.checked = rel.includes('noopener');
        noreferrerCheckbox.checked = rel.includes('noreferrer');
        nofollowCheckbox.checked = rel.includes('nofollow');
    } else {
        urlInput.value = '';
        titleInput.value = '';
        newWindowCheckbox.checked = false;
        noopenerCheckbox.checked = false;
        noreferrerCheckbox.checked = false;
        nofollowCheckbox.checked = false;
    }

    // Update button text
    insertBtn.textContent = existingAttrs ? 'Update link' : 'Add link';

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

    // Insert link handler
    const handleInsert = () => {
        const url = urlInput.value.trim();
        if (!url) return;

        const title = titleInput.value.trim();
        const openInNewWindow = newWindowCheckbox.checked;
        const noopener = noopenerCheckbox.checked;
        const noreferrer = noreferrerCheckbox.checked;
        const nofollow = nofollowCheckbox.checked;

        // Build rel attribute
        const relParts = [];
        if (noopener) relParts.push('noopener');
        if (noreferrer) relParts.push('noreferrer');
        if (nofollow) relParts.push('nofollow');
        const rel = relParts.join(' ');

        const linkAttrs = {
            href: url,
            title: title || null,
            target: openInNewWindow ? '_blank' : null,
            rel: rel || null
        };

        linkImage(editor, imgElement, linkAttrs);
        modal.hide();
    };

    insertBtn.onclick = handleInsert;

    // Handle Enter key in inputs
    [urlInput, titleInput].forEach(input => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !insertBtn.disabled) {
                e.preventDefault();
                handleInsert();
            }
        });
    });

    // Cleanup on hide
    modalEl.addEventListener('hidden.bs.modal', () => {
        // Restore text input and upload tab visibility for regular link modal usage
        textInput.closest('.mb-3').style.display = '';
        if (uploadTab) {
            uploadTab.closest('.nav-item').style.display = '';
        }
        modalTitle.textContent = 'Insert link';
        insertBtn.textContent = 'Insert link';
    }, { once: true });

    modal.show();
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
    const noopenerCheckbox = modalEl.querySelector('#link-noopener');
    const noreferrerCheckbox = modalEl.querySelector('#link-noreferrer');
    const nofollowCheckbox = modalEl.querySelector('#link-nofollow');
    const insertBtn = modalEl.querySelector('#link-insert-btn');

    // Prefill for editing
    if (existingAttrs) {
        urlInput.value = existingAttrs.href || '';
        titleInput.value = existingAttrs.title || '';
        newWindowCheckbox.checked = existingAttrs.target === '_blank';
        
        // Parse rel attribute for noopener, noreferrer, and nofollow
        const rel = existingAttrs.rel || '';
        noopenerCheckbox.checked = rel.includes('noopener');
        noreferrerCheckbox.checked = rel.includes('noreferrer');
        nofollowCheckbox.checked = rel.includes('nofollow');
        
        textInput.value = '';  // Can't easily get selected text for editing
    } else {
        // Prefill with selection text
        const { from, to } = editor.state.selection;
        const text = editor.state.doc.textBetween(from, to, '');
        urlInput.value = '';
        textInput.value = text;
        titleInput.value = '';
        newWindowCheckbox.checked = false;
        noopenerCheckbox.checked = false;
        noreferrerCheckbox.checked = false;
        nofollowCheckbox.checked = false;
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
        const noopener = noopenerCheckbox.checked;
        const noreferrer = noreferrerCheckbox.checked;
        const nofollow = nofollowCheckbox.checked;

        // Build rel attribute
        const relParts = [];
        if (noopener) relParts.push('noopener');
        if (noreferrer) relParts.push('noreferrer');
        if (nofollow) relParts.push('nofollow');
        const rel = relParts.join(' ');

        const linkAttrs = {
            href: url,
            title: title || null,
            target: openInNewWindow ? '_blank' : null,
            rel: rel || null
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
                // Insert new text with link - use proper mark structure to avoid HTML parsing
                editor.chain().focus()
                    .insertContent({ type: 'text', text: text, marks: [{ type: 'link', attrs: linkAttrs }] })
                    .run();
            } else {
                // Just use URL as text - use proper mark structure to avoid HTML parsing
                editor.chain().focus()
                    .insertContent({ type: 'text', text: url, marks: [{ type: 'link', attrs: linkAttrs }] })
                    .run();
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
        const html = textarea.value;
        // Set emitUpdate to true so the onUpdate handler fires and updates the form textarea
        editor.commands.setContent(html, true);
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
 * Create Div node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Div extension
 */
function createDivNode(Node) {
    return Node.create({
        name: 'div',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'div' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['div', HTMLAttributes, 0];
        },
    });
}


/**
 * Create Section node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Section extension
 */
function createSectionNode(Node) {
    return Node.create({
        name: 'section',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'section' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['section', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Article node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Article extension
 */
function createArticleNode(Node) {
    return Node.create({
        name: 'article',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'article' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['article', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Aside node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Aside extension
 */
function createAsideNode(Node) {
    return Node.create({
        name: 'aside',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'aside' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['aside', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Nav node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Nav extension
 */
function createNavNode(Node) {
    return Node.create({
        name: 'nav',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'nav' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['nav', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Figure node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Figure extension
 */
function createFigureNode(Node) {
    return Node.create({
        name: 'figure',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'figure' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['figure', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Figcaption node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Figcaption extension
 */
function createFigcaptionNode(Node) {
    return Node.create({
        name: 'figcaption',
        content: 'inline*',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'figcaption' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['figcaption', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Details node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Details extension
 */
function createDetailsNode(Node) {
    return Node.create({
        name: 'details',
        content: 'block+',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'details' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['details', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Summary node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Summary extension
 */
function createSummaryNode(Node) {
    return Node.create({
        name: 'summary',
        content: 'inline*',
        group: 'block',
        defining: true,
        parseHTML() {
            return [{ tag: 'summary' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['summary', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Pre (without code) node extension for TipTap
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Pre extension
 */
function createPreNode(Node) {
    return Node.create({
        name: 'pre',
        content: 'inline*',
        group: 'block',
        code: true,
        defining: true,
        parseHTML() {
            return [{ tag: 'pre', preserveWhitespace: 'full' }];
        },
        renderHTML({ HTMLAttributes }) {
            return ['pre', HTMLAttributes, 0];
        },
    });
}

/**
 * Create Button node extension for TipTap
 * Allows <button> elements to be used anywhere in content (Bootstrap accordions, etc.)
 * Preserves all attributes including data-bs-*, aria-*, id, class, type
 * @param {Object} Node - TipTap Node class
 * @returns {Object} Button extension
 */
function createButtonNode(Node) {
    return Node.create({
        name: 'button',
        inline: true,
        group: 'inline',
        content: 'text*',  // Allow text content inside button
        atom: false,       // Not atomic so content can be edited
        
        addAttributes() {
            return {
                // Bootstrap attributes
                'data-bs-toggle': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-bs-toggle') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-bs-toggle']) return {};
                        return { 'data-bs-toggle': attributes['data-bs-toggle'] };
                    },
                },
                'data-bs-target': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-bs-target') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-bs-target']) return {};
                        return { 'data-bs-target': attributes['data-bs-target'] };
                    },
                },
                'data-bs-parent': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-bs-parent') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-bs-parent']) return {};
                        return { 'data-bs-parent': attributes['data-bs-parent'] };
                    },
                },
                // ARIA attributes
                'aria-expanded': {
                    default: null,
                    parseHTML: element => element.getAttribute('aria-expanded') || null,
                    renderHTML: attributes => {
                        if (!attributes['aria-expanded']) return {};
                        return { 'aria-expanded': attributes['aria-expanded'] };
                    },
                },
                'aria-controls': {
                    default: null,
                    parseHTML: element => element.getAttribute('aria-controls') || null,
                    renderHTML: attributes => {
                        if (!attributes['aria-controls']) return {};
                        return { 'aria-controls': attributes['aria-controls'] };
                    },
                },
                // Standard attributes (class, id, type, etc. handled by CustomAttributes extension)
                class: {
                    default: null,
                    parseHTML: element => element.getAttribute('class') || null,
                    renderHTML: attributes => {
                        if (!attributes.class) return {};
                        return { class: attributes.class };
                    },
                },
                id: {
                    default: null,
                    parseHTML: element => element.getAttribute('id') || null,
                    renderHTML: attributes => {
                        if (!attributes.id) return {};
                        return { id: attributes.id };
                    },
                },
                type: {
                    default: null,
                    parseHTML: element => element.getAttribute('type') || null,
                    renderHTML: attributes => {
                        if (!attributes.type) return {};
                        return { type: attributes.type };
                    },
                },
                style: {
                    default: null,
                    parseHTML: element => element.getAttribute('style') || null,
                    renderHTML: attributes => {
                        if (!attributes.style) return {};
                        return { style: attributes.style };
                    },
                },
                title: {
                    default: null,
                    parseHTML: element => element.getAttribute('title') || null,
                    renderHTML: attributes => {
                        if (!attributes.title) return {};
                        return { title: attributes.title };
                    },
                },
                // Catch-all for any other data-* attributes
                'data-id': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-id') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-id']) return {};
                        return { 'data-id': attributes['data-id'] };
                    },
                },
                'data-name': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-name') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-name']) return {};
                        return { 'data-name': attributes['data-name'] };
                    },
                },
                'data-value': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-value') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-value']) return {};
                        return { 'data-value': attributes['data-value'] };
                    },
                },
            };
        },
        
        parseHTML() {
            return [
                {
                    tag: 'button',
                    // Preserve all attributes when parsing
                    getAttrs: element => {
                        return {};  // Return empty object to accept all buttons
                    },
                },
            ];
        },
        
        renderHTML({ HTMLAttributes }) {
            // Render button with all preserved attributes
            return ['button', HTMLAttributes, 0];
        },
    });
}

/**
 * Create custom TextStyle extension that supports span with custom attributes
 * @param {Object} TextStyle - TipTap TextStyle extension
 * @returns {Object} Extended TextStyle
 */
function createExtendedTextStyle(TextStyle) {
    return TextStyle.extend({
        addAttributes() {
            return {
                ...this.parent?.(),
                class: {
                    default: null,
                    parseHTML: element => element.getAttribute('class') || null,
                    renderHTML: attributes => {
                        if (!attributes.class) return {};
                        return { class: attributes.class };
                    },
                },
                id: {
                    default: null,
                    parseHTML: element => element.getAttribute('id') || null,
                    renderHTML: attributes => {
                        if (!attributes.id) return {};
                        return { id: attributes.id };
                    },
                },
                style: {
                    default: null,
                    parseHTML: element => element.getAttribute('style') || null,
                    renderHTML: attributes => {
                        if (!attributes.style) return {};
                        return { style: attributes.style };
                    },
                },
                title: {
                    default: null,
                    parseHTML: element => element.getAttribute('title') || null,
                    renderHTML: attributes => {
                        if (!attributes.title) return {};
                        return { title: attributes.title };
                    },
                },
                'data-id': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-id') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-id']) return {};
                        return { 'data-id': attributes['data-id'] };
                    },
                },
                'data-name': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-name') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-name']) return {};
                        return { 'data-name': attributes['data-name'] };
                    },
                },
                'data-value': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-value') || null,
                    renderHTML: attributes => {
                        if (!attributes['data-value']) return {};
                        return { 'data-value': attributes['data-value'] };
                    },
                },
            };
        },
        parseHTML() {
            return [
                {
                    tag: 'span',
                    getAttrs: element => {
                        // Only parse spans that have attributes we care about
                        const hasRelevantAttrs = element.hasAttribute('style') ||
                                                element.hasAttribute('class') ||
                                                element.hasAttribute('id') ||
                                                element.hasAttribute('title') ||
                                                Array.from(element.attributes).some(attr => attr.name.startsWith('data-'));
                        
                        return hasRelevantAttrs ? {} : false;
                    },
                },
            ];
        },
    });
}

/**
 * Create custom Link extension that properly handles rel and target attributes without defaults
 * Disables TipTap's automatic security attributes (noopener, noreferrer, nofollow)
 * @param {Object} Link - TipTap Link extension
 * @returns {Object} Extended Link
 */
function createExtendedLink(Link) {
    return Link.extend({
        addOptions() {
            return {
                ...this.parent?.(),
                // Disable automatic protocol validation
                validate: undefined,
                // Disable auto-linking
                autolink: false,
                // Override default HTMLAttributes
                HTMLAttributes: {},
            };
        },
        
        addAttributes() {
            return {
                href: {
                    default: null,
                    parseHTML(element) {
                        return element.getAttribute('href') || null;
                    },
                },
                target: {
                    default: null,
                    parseHTML(element) {
                        return element.getAttribute('target') || null;
                    },
                },
                rel: {
                    default: null,
                    parseHTML(element) {
                        return element.getAttribute('rel') || null;
                    },
                },
                class: {
                    default: null,
                    parseHTML(element) {
                        const className = element.getAttribute('class');
                        return className || null;
                    },
                },
                title: {
                    default: null,
                    parseHTML(element) {
                        const title = element.getAttribute('title');
                        return title || null;
                    },
                },
            };
        },
        
        // Override parseHTML to prevent automatic attribute manipulation
        parseHTML() {
            return [
                {
                    tag: 'a[href]',
                    getAttrs: (dom) => {
                        return {};
                    },
                },
            ];
        },
        
        // CRITICAL: Override renderHTML to have full control over output
        renderHTML({ HTMLAttributes }) {
            // Build attributes object manually, only including non-null values
            const attrs = {
                href: HTMLAttributes.href,
            };
            
            if (HTMLAttributes.target) {
                attrs.target = HTMLAttributes.target;
            }
            
            if (HTMLAttributes.rel) {
                attrs.rel = HTMLAttributes.rel;
            }
            
            if (HTMLAttributes.class) {
                attrs.class = HTMLAttributes.class;
            }
            
            if (HTMLAttributes.title) {
                attrs.title = HTMLAttributes.title;
            }
            
            return ['a', attrs, 0];
        },
    });
}

/**
 * Create custom attributes extension for TipTap
 * Supports: class, id, style, title, alt, aria-*, role, data-*
 * @param {Object} Extension - TipTap Extension class
 * @returns {Object} Custom attributes extension
 */
function createCustomAttributesExtension(Extension) {
    return Extension.create({
        name: 'customAttributes',
        
        addGlobalAttributes() {
            return [
                {
                    // Apply to all node types (excluding 'link' which has its own extended version)
                    types: [
                        'paragraph',
                        'heading',
                        'blockquote',
                        'bulletList',
                        'orderedList',
                        'listItem',
                        'codeBlock',
                        'horizontalRule',
                        'image',
                        'table',
                        'tableRow',
                        'tableCell',
                        'tableHeader',
                        'div',
                        'section',
                        'article',
                        'aside',
                        'nav',
                        'figure',
                        'figcaption',
                        'details',
                        'summary',
                        'pre',
                        'video',
                        'button',
                    ],
                    attributes: {
                        class: {
                            default: null,
                            parseHTML: element => element.getAttribute('class') || null,
                            renderHTML: attributes => {
                                if (!attributes.class) return {};
                                return { class: attributes.class };
                            },
                        },
                        id: {
                            default: null,
                            parseHTML: element => element.getAttribute('id') || null,
                            renderHTML: attributes => {
                                if (!attributes.id) return {};
                                return { id: attributes.id };
                            },
                        },
                        style: {
                            default: null,
                            parseHTML: element => element.getAttribute('style') || null,
                            renderHTML: attributes => {
                                if (!attributes.style) return {};
                                return { style: attributes.style };
                            },
                        },
                        title: {
                            default: null,
                            parseHTML: element => element.getAttribute('title') || null,
                            renderHTML: attributes => {
                                if (!attributes.title) return {};
                                return { title: attributes.title };
                            },
                        },
                        role: {
                            default: null,
                            parseHTML: element => element.getAttribute('role') || null,
                            renderHTML: attributes => {
                                if (!attributes.role) return {};
                                return { role: attributes.role };
                            },
                        },
                        // Aria attributes
                        'aria-label': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-label') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-label']) return {};
                                return { 'aria-label': attributes['aria-label'] };
                            },
                        },
                        'aria-labelledby': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-labelledby') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-labelledby']) return {};
                                return { 'aria-labelledby': attributes['aria-labelledby'] };
                            },
                        },
                        'aria-describedby': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-describedby') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-describedby']) return {};
                                return { 'aria-describedby': attributes['aria-describedby'] };
                            },
                        },
                        'aria-hidden': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-hidden') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-hidden']) return {};
                                return { 'aria-hidden': attributes['aria-hidden'] };
                            },
                        },
                        'aria-expanded': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-expanded') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-expanded']) return {};
                                return { 'aria-expanded': attributes['aria-expanded'] };
                            },
                        },
                        'aria-controls': {
                            default: null,
                            parseHTML: element => element.getAttribute('aria-controls') || null,
                            renderHTML: attributes => {
                                if (!attributes['aria-controls']) return {};
                                return { 'aria-controls': attributes['aria-controls'] };
                            },
                        },
                        // Common data attributes
                        'data-id': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-id') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-id']) return {};
                                return { 'data-id': attributes['data-id'] };
                            },
                        },
                        'data-name': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-name') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-name']) return {};
                                return { 'data-name': attributes['data-name'] };
                            },
                        },
                        'data-value': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-value') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-value']) return {};
                                return { 'data-value': attributes['data-value'] };
                            },
                        },
                        'data-type': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-type') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-type']) return {};
                                return { 'data-type': attributes['data-type'] };
                            },
                        },
                        'data-target': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-target') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-target']) return {};
                                return { 'data-target': attributes['data-target'] };
                            },
                        },
                        'data-toggle': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-toggle') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-toggle']) return {};
                                return { 'data-toggle': attributes['data-toggle'] };
                            },
                        },
                        'data-dismiss': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-dismiss') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-dismiss']) return {};
                                return { 'data-dismiss': attributes['data-dismiss'] };
                            },
                        },
                        // Bootstrap 5 specific attributes (for accordions, modals, etc.)
                        'data-bs-toggle': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-bs-toggle') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-bs-toggle']) return {};
                                return { 'data-bs-toggle': attributes['data-bs-toggle'] };
                            },
                        },
                        'data-bs-target': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-bs-target') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-bs-target']) return {};
                                return { 'data-bs-target': attributes['data-bs-target'] };
                            },
                        },
                        'data-bs-parent': {
                            default: null,
                            parseHTML: element => element.getAttribute('data-bs-parent') || null,
                            renderHTML: attributes => {
                                if (!attributes['data-bs-parent']) return {};
                                return { 'data-bs-parent': attributes['data-bs-parent'] };
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

    const {
        Editor,
        Node,
        Extension,
        StarterKit,
        Link,
        Image,
        Table,
        TableRow,
        TableCell,
        TableHeader,
        Underline,
        Subscript,
        Superscript,
        TextStyle,
        FontFamily,
        TextAlign,
        Color,
        Highlight } = await import('/raytha_admin/lib/tiptap/tiptap-bundle.js');

    // Create custom Video node
    const Video = createVideoNode(Node);

    // Create custom FontSize extension
    const FontSize = createFontSizeExtension(Extension);

    // Create extended Link that properly handles rel attribute without defaults
    const ExtendedLink = createExtendedLink(Link);

    // Create extended TextStyle that supports span with custom attributes
    const ExtendedTextStyle = createExtendedTextStyle(TextStyle);

    // Create custom HTML5 semantic element nodes
    const Div = createDivNode(Node);
    const Section = createSectionNode(Node);
    const Article = createArticleNode(Node);
    const Aside = createAsideNode(Node);
    const Nav = createNavNode(Node);
    const Figure = createFigureNode(Node);
    const Figcaption = createFigcaptionNode(Node);
    const Details = createDetailsNode(Node);
    const Summary = createSummaryNode(Node);
    const Pre = createPreNode(Node);

    // Create Button node (for Bootstrap accordions, modals, etc.)
    const Button = createButtonNode(Node);

    // Create custom attributes extension (class, id, style, aria-*, data-*, etc.)
    const CustomAttributes = createCustomAttributesExtension(Extension);

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
            StarterKit,
            ExtendedLink.configure({
                openOnClick: false,
                autolink: false,
                linkOnPaste: false,
                HTMLAttributes: {},
            }),
            Image.configure({
                inline: true,  // Allow images to be inline so they can be wrapped in links
                HTMLAttributes: {
                    class: 'img-fluid',
                },
            }),
            Table.configure({
                resizable: true,
            }),
            TableRow,
            TableCell,
            TableHeader,
            Underline,
            Subscript,
            Superscript,
            ExtendedTextStyle,  // Use extended TextStyle that supports span with custom attributes
            FontFamily,
            FontSize,
            TextAlign.configure({
                types: ['heading', 'paragraph'],
            }),
            Color,
            Highlight.configure({
                multicolor: true,
            }),
            Video,
            // Custom HTML5 semantic elements
            Div,
            Section,
            Article,
            Aside,
            Nav,
            Figure,
            Figcaption,
            Details,
            Summary,
            Pre,
            // Button element (for Bootstrap accordions, modals, etc.)
            Button,
            // Custom attributes (must be last to apply to all elements)
            CustomAttributes,
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

        // Check for image first (even if inside a link)
        if (target.tagName === 'IMG') {
            showImageContextMenu(e, editor, config, target);
            return;
        }

        // Check for link (but not if it contains an image)
        if ((target.tagName === 'A' || target.closest('a')) && editor.isActive('link')) {
            // Make sure it's not a link around an image
            const linkElement = target.tagName === 'A' ? target : target.closest('a');
            if (linkElement && !linkElement.querySelector('img')) {
                showLinkContextMenu(e, editor, config);
                return;
            }
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
