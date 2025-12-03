/**
 * Developer Name Sync
 * Auto-generates developer names from label inputs.
 * 
 * Syncs a label field to a developer name field, converting:
 * "My Content Type" → "my_content_type"
 */

import { $, on } from '/js/core/dom.js';
import { toDeveloperName } from '/js/core/utils.js';

/**
 * Bind label to developer name sync
 * @param {string|HTMLInputElement} labelInput - Label input element or selector
 * @param {string|HTMLInputElement} devNameInput - Developer name input element or selector
 * @param {Object} options - Options
 * @param {boolean} options.onlyIfEmpty - Only sync if developer name was initially empty (default: true)
 * @param {boolean} options.allowDot - When true, preserves dots in output (useful for Raytha function routes like feed.xml)
 * @returns {Function} - Cleanup function to remove listeners
 */
export const bindDeveloperNameSync = (labelInput, devNameInput, options = {}) => {
    const defaults = {
        onlyIfEmpty: true,
        allowDot: false,
    };

    const opts = { ...defaults, ...options };
    const devNameOptions = { allowDot: opts.allowDot };

    const labelEl = typeof labelInput === 'string' ? $(labelInput) : labelInput;
    const devNameEl = typeof devNameInput === 'string' ? $(devNameInput) : devNameInput;

    if (!labelEl || !devNameEl) {
        console.warn('Developer name sync: Label or developer name input not found', { labelInput, devNameInput });
        return () => { };
    }

    // Track if user has manually overridden the developer name
    let userOverrode = false;

    // Check initial state - if dev name has a value that's different from what it should be, user overrode it
    if (opts.onlyIfEmpty && devNameEl.value.trim()) {
        const initialSlug = toDeveloperName(labelEl.value, devNameOptions);
        if (devNameEl.value !== initialSlug) {
            userOverrode = true;
        }
    }

    // If user types directly in dev name field, stop auto-sync
    const handleDevNameInput = () => {
        userOverrode = true;
    };

    on(devNameEl, 'input', handleDevNameInput);
    on(devNameEl, 'change', handleDevNameInput);

    // Handle label input event
    const handleLabelInput = () => {
        if (!userOverrode) {
            devNameEl.value = toDeveloperName(labelEl.value, devNameOptions);
        }
    };

    // Attach listeners to label field
    on(labelEl, 'input', handleLabelInput);
    on(labelEl, 'keyup', handleLabelInput);
    on(labelEl, 'change', handleLabelInput);

    // Return cleanup function
    return () => {
        labelEl.removeEventListener('input', handleLabelInput);
        labelEl.removeEventListener('keyup', handleLabelInput);
        labelEl.removeEventListener('change', handleLabelInput);
        devNameEl.removeEventListener('input', handleDevNameInput);
        devNameEl.removeEventListener('change', handleDevNameInput);
    };
};

/**
 * Initialize developer name sync based on data attributes
 * Looks for elements with data-sync-to attribute
 * @param {Element} root - Root element to search within (default: document)
 */
export const initDeveloperNameSync = (root = document) => {
    const labelInputs = root.querySelectorAll('[data-sync-to]');

    labelInputs.forEach(labelInput => {
        const targetSelector = labelInput.getAttribute('data-sync-to');
        const onlyIfEmpty = labelInput.getAttribute('data-sync-only-if-empty') !== 'false';
        const allowDot = labelInput.getAttribute('data-sync-allow-dot') === 'true';

        bindDeveloperNameSync(labelInput, targetSelector, { onlyIfEmpty, allowDot });
    });
};

/**
 * Legacy function for backward compatibility
 * @deprecated Use bindDeveloperNameSync instead
 */
export const bindDeveloperNameSyncById = (sourceId, destinationId) => {
    return bindDeveloperNameSync(`#${sourceId}`, `#${destinationId}`);
};

export default bindDeveloperNameSync;

