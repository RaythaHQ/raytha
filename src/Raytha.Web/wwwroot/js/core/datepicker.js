/**
 * Datepicker Module
 * Provides a reusable datepicker solution using Flatpickr
 * Auto-initializes all elements with [data-datepicker] attribute
 */

/**
 * Default configuration for Flatpickr
 */
const DEFAULT_CONFIG = {
    dateFormat: 'm/d/Y',
    allowInput: true,
    altInput: true,
    altFormat: 'm/d/Y',
    disableMobile: false,
};

/**
 * Initialize a single datepicker on an input element
 * @param {HTMLInputElement} input - Input element to attach datepicker to
 * @param {Object} customConfig - Custom Flatpickr configuration
 * @returns {Object|null} - Flatpickr instance or null if Flatpickr not loaded
 */
export const initDatepicker = (input, customConfig = {}) => {
    if (typeof flatpickr === 'undefined') {
        console.error('Flatpickr library not loaded. Please include Flatpickr CSS and JS.');
        return null;
    }

    // Merge default config with custom config
    const config = { ...DEFAULT_CONFIG, ...customConfig };

    // Parse data attributes for configuration
    if (input.dataset.dateFormat) {
        config.dateFormat = input.dataset.dateFormat;
        config.altFormat = input.dataset.dateFormat;
    }

    if (input.dataset.minDate) {
        config.minDate = input.dataset.minDate;
    }

    if (input.dataset.maxDate) {
        config.maxDate = input.dataset.maxDate;
    }

    if (input.dataset.enableTime !== undefined) {
        config.enableTime = input.dataset.enableTime === 'true';
    }

    if (input.dataset.noCalendar !== undefined) {
        config.noCalendar = input.dataset.noCalendar === 'true';
    }

    if (input.dataset.mode) {
        config.mode = input.dataset.mode; // single, multiple, range
    }

    // Initialize Flatpickr
    return flatpickr(input, config);
};

/**
 * Initialize all datepickers on the page
 * Finds all elements with [data-datepicker] attribute
 * @returns {Array} - Array of Flatpickr instances
 */
export const initAllDatepickers = () => {
    const datepickerInputs = document.querySelectorAll('[data-datepicker]');
    const instances = [];

    datepickerInputs.forEach(input => {
        const instance = initDatepicker(input);
        if (instance) {
            instances.push(instance);
        }
    });

    console.log(`Initialized ${instances.length} datepicker(s)`);
    return instances;
};

/**
 * Destroy a datepicker instance
 * @param {Object} instance - Flatpickr instance
 */
export const destroyDatepicker = (instance) => {
    if (instance && typeof instance.destroy === 'function') {
        instance.destroy();
    }
};

/**
 * Get Flatpickr instance from an input element
 * @param {HTMLInputElement} input - Input element
 * @returns {Object|null} - Flatpickr instance or null
 */
export const getDatepickerInstance = (input) => {
    return input._flatpickr || null;
};

/**
 * Set date on a datepicker
 * @param {HTMLInputElement} input - Input element with datepicker
 * @param {Date|string} date - Date to set
 */
export const setDatepickerDate = (input, date) => {
    const instance = getDatepickerInstance(input);
    if (instance) {
        instance.setDate(date);
    }
};

/**
 * Clear date on a datepicker
 * @param {HTMLInputElement} input - Input element with datepicker
 */
export const clearDatepickerDate = (input) => {
    const instance = getDatepickerInstance(input);
    if (instance) {
        instance.clear();
    }
};

/**
 * Check if Flatpickr is loaded
 * @returns {boolean}
 */
export const isFlatpickrLoaded = () => {
    return typeof flatpickr !== 'undefined';
};

/**
 * Load Flatpickr dynamically if not already loaded
 * @returns {Promise<boolean>} - True if loaded successfully
 */
export const loadFlatpickr = () => {
    return new Promise((resolve, reject) => {
        // Check if already loaded
        if (isFlatpickrLoaded()) {
            resolve(true);
            return;
        }

        // Load CSS
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = '/raytha_admin/lib/flatpickr/dist/flatpickr.min.css';
        document.head.appendChild(link);

        // Load JS
        const script = document.createElement('script');
        script.src = '/raytha_admin/lib/flatpickr/dist/flatpickr.min.js';
        script.onload = () => {
            console.log('Flatpickr loaded successfully');
            resolve(true);
        };
        script.onerror = () => {
            console.error('Failed to load Flatpickr');
            reject(false);
        };
        document.head.appendChild(script);
    });
};

/**
 * Initialize datepickers when DOM is ready
 * This is called automatically when the module is imported
 */
const autoInit = () => {
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', async () => {
            await loadFlatpickr();
            initAllDatepickers();
        });
    } else {
        // DOM already loaded
        loadFlatpickr().then(() => {
            initAllDatepickers();
        });
    }
};

// Auto-initialize when module is imported
autoInit();

// Export for manual usage
export default {
    initDatepicker,
    initAllDatepickers,
    destroyDatepicker,
    getDatepickerInstance,
    setDatepickerDate,
    clearDatepickerDate,
    loadFlatpickr,
    isFlatpickrLoaded,
};

