/**
 * Content Items - Attachment Field Module
 * Initializes Uppy file upload for attachment fields
 * 
 * Uppy is loaded via CDN. This module provides the initialization logic
 * for each attachment field instance, supporting both direct upload and cloud upload modes.
 */

/**
 * Initialize an attachment field with Uppy file uploader
 * @param {HTMLElement} fieldElement - The field container element
 * @param {Object} config - Configuration object
 * @param {string} config.developername - Field developer name
 * @param {string} config.pathBase - URL base path
 * @param {boolean} config.useDirectUploadToCloud - Upload mode (direct or cloud)
 * @param {number} config.maxFileSize - Maximum file size in bytes
 * @param {string} config.allowedMimeTypes - Comma-separated MIME types
 * @returns {Object} Uppy instance
 */
export async function initAttachmentField(fieldElement, config) {
    const developername = config.developername || fieldElement.dataset.developername;
    const uppyContainer = fieldElement.querySelector(`#${developername}-uppy`);
    const progressContainer = fieldElement.querySelector(`#${developername}-uppy-progress`);
    const hiddenInput = fieldElement.querySelector('input[type="hidden"]');
    const fileInfoDiv = fieldElement.querySelector('div[hidden]');

    if (!uppyContainer || !hiddenInput) {
        console.error('Attachment field elements not found:', developername);
        return null;
    }

    // Setup remove button handler function that will be used throughout
    let setupRemoveButton;

    // Import Uppy from CDN
    const { Uppy, Dashboard, XHRUpload, AwsS3 } = await import('https://releases.transloadit.com/uppy/v4.13.3/uppy.min.mjs');

    // Parse allowed MIME types
    const allowedFileTypes = config.allowedMimeTypes
        ? config.allowedMimeTypes.split(',').map(t => t.trim())
        : null;

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
        target: uppyContainer,
        height: 200,
        hideUploadButton: false,
        showProgressDetails: true,
        note: config.maxFileSize
            ? `Max file size: ${formatFileSize(config.maxFileSize)}`
            : null,
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

            if (objectKey) {
                hiddenInput.value = objectKey;
                showFileInfo(fileInfoDiv, file.name, objectKey, config.pathBase, setupRemoveButton);
                uppyContainer.style.display = 'none';
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

                if (file.meta.objectKey) {
                    hiddenInput.value = file.meta.objectKey;
                    showFileInfo(fileInfoDiv, file.name, file.meta.objectKey, config.pathBase, setupRemoveButton);
                    uppyContainer.style.display = 'none';
                }
            } catch (err) {
                console.error('Error in create-after-upload:', err);
            }
        });
    }

    // Handle errors
    uppy.on('restriction-failed', (_file, error) => {
        console.error('File restriction:', error);
        showError(fieldElement, error.message);
    });

    uppy.on('upload-error', (file, error, _response) => {
        console.error('Upload error:', file && file.id, error);
        showError(fieldElement, 'Upload failed. Please try again.');
    });

    // Define remove button handler
    setupRemoveButton = () => {
        if (!fileInfoDiv) return;

        const removeBtn = fileInfoDiv.querySelector('button');
        if (removeBtn) {
            removeBtn.hidden = false;
            // Remove any existing event listeners by cloning the button
            const newRemoveBtn = removeBtn.cloneNode(true);
            removeBtn.parentNode.replaceChild(newRemoveBtn, removeBtn);

            newRemoveBtn.addEventListener('click', (e) => {
                e.preventDefault();
                hiddenInput.value = '';
                fileInfoDiv.hidden = true;
                uppyContainer.style.display = 'block';
                uppy.reset();
            });
        }
    };

    // If file exists on page load, show file info and hide uppy
    if (hiddenInput.value && fileInfoDiv) {
        fileInfoDiv.hidden = false;
        uppyContainer.style.display = 'none';
        setupRemoveButton();
    }

    return uppy;
}

/**
 * Show file information after successful upload
 * @param {HTMLElement} fileInfoDiv - File info container
 * @param {string} fileName - Name of uploaded file
 * @param {string} objectKey - Storage object key
 * @param {string} pathBase - URL base path
 * @param {Function} setupRemoveButton - Function to setup remove button
 */
function showFileInfo(fileInfoDiv, fileName, objectKey, pathBase, setupRemoveButton) {
    if (!fileInfoDiv) return;

    fileInfoDiv.hidden = false;
    const para = fileInfoDiv.querySelector('p');
    const link = fileInfoDiv.querySelector('a');

    if (para) para.textContent = fileName;
    if (link) link.href = `${pathBase}/raytha/media-items/objectkey/${objectKey}`;

    // Setup remove button with event listener
    if (setupRemoveButton) {
        setupRemoveButton();
    }
}

/**
 * Show error message for the field
 * @param {HTMLElement} fieldElement - Field container
 * @param {string} message - Error message
 */
function showError(fieldElement, message) {
    let errorDiv = fieldElement.querySelector('.text-danger');
    if (!errorDiv) {
        errorDiv = document.createElement('div');
        errorDiv.className = 'text-danger mt-2';
        const label = fieldElement.querySelector('label');
        if (label) {
            label.parentElement.appendChild(errorDiv);
        }
    }
    errorDiv.textContent = message;
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
 * Format file size for display
 * @param {number} bytes - File size in bytes
 * @returns {string} Formatted file size
 */
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Initialize all attachment fields on the page
 * @param {Object} config - Global configuration
 */
export function initAllAttachmentFields(config) {
    const fields = document.querySelectorAll('[data-field-type="attachment"]');
    const uploaders = [];

    fields.forEach(field => {
        const fieldConfig = {
            developername: field.dataset.developername,
            pathBase: config.pathBase || '',
            useDirectUploadToCloud: config.useDirectUploadToCloud || false,
            maxFileSize: config.maxFileSize || null,
            allowedMimeTypes: config.allowedMimeTypes || null,
        };

        initAttachmentField(field, fieldConfig).then(uppy => {
            if (uppy) {
                uploaders.push(uppy);
            }
        });
    });

    return uploaders;
}

