/**
 * General Utilities
 * Provides common helper functions for formatting, string manipulation, etc.
 */

/**
 * Format date to locale string
 * @param {Date|string} date - Date to format
 * @param {Object} options - Intl.DateTimeFormat options
 * @returns {string}
 */
export const formatDate = (date, options = {}) => {
  if (typeof date === 'string') {
    date = new Date(date);
  }
  
  const defaults = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  };
  
  return date.toLocaleDateString(undefined, { ...defaults, ...options });
};

/**
 * Format date and time to locale string
 * @param {Date|string} date - Date to format
 * @param {Object} options - Intl.DateTimeFormat options
 * @returns {string}
 */
export const formatDateTime = (date, options = {}) => {
  if (typeof date === 'string') {
    date = new Date(date);
  }
  
  const defaults = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  };
  
  return date.toLocaleString(undefined, { ...defaults, ...options });
};

/**
 * Format currency
 * @param {number} amount - Amount to format
 * @param {string} currency - Currency code (e.g., 'USD')
 * @returns {string}
 */
export const formatCurrency = (amount, currency = 'USD') => {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
  }).format(amount);
};

/**
 * Copy text to clipboard
 * @param {string} text - Text to copy
 * @returns {Promise<boolean>} - True if successful
 */
export const copyToClipboard = async (text) => {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch (err) {
    console.error('Failed to copy to clipboard:', err);
    return false;
  }
};

/**
 * Download file from blob
 * @param {Blob} blob - File blob
 * @param {string} filename - Filename to save as
 */
export const downloadFile = (blob, filename) => {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
};

/**
 * Safely parse JSON
 * @param {string} json - JSON string
 * @param {any} defaultValue - Default value if parsing fails
 * @returns {any}
 */
export const safeParseJSON = (json, defaultValue = null) => {
  try {
    return JSON.parse(json);
  } catch (err) {
    console.error('Failed to parse JSON:', err);
    return defaultValue;
  }
};

/**
 * Convert string to slug/developer name format
 * @param {string} str - String to convert
 * @returns {string} - Slugified string
 */
export const slugify = (str) => {
  return str
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_-]+/g, '_')
    .replace(/^-+|-+$/g, '');
};

/**
 * Convert label to developer name format
 * Used for auto-generating developer names from labels.
 * Converts "My Content Type" to "my_content_type"
 * @param {string} label - Label text
 * @returns {string} - Developer name (lowercase, underscores, alphanumeric only)
 */
export const toDeveloperName = (label) => {
  if (!label) return '';
  
  return label
    .toLowerCase()
    .normalize('NFKD')                // Handle accents
    .replace(/[\u0300-\u036f]/g, '')  // Strip diacritics
    .replace(/[^a-z0-9\s_-]/g, '')    // Keep only alphanumeric, space, underscore, hyphen
    .trim()
    .replace(/\s+/g, '_')             // Replace spaces with underscores
    .replace(/-+/g, '_')              // Replace hyphens with underscores
    .replace(/_+/g, '_')              // Collapse multiple underscores
    .replace(/^_|_$/g, '')            // Remove leading/trailing underscores
    .substring(0, 128);               // Max length 128 characters
};

/**
 * Truncate string
 * @param {string} str - String to truncate
 * @param {number} length - Maximum length
 * @param {string} suffix - Suffix to append (default '...')
 * @returns {string}
 */
export const truncate = (str, length, suffix = '...') => {
  if (str.length <= length) {
    return str;
  }
  return str.substring(0, length - suffix.length) + suffix;
};

/**
 * Escape HTML special characters
 * @param {string} html - HTML string
 * @returns {string}
 */
export const escapeHtml = (html) => {
  const div = document.createElement('div');
  div.textContent = html;
  return div.innerHTML;
};

/**
 * Unescape HTML entities
 * @param {string} html - HTML string with entities
 * @returns {string}
 */
export const unescapeHtml = (html) => {
  const div = document.createElement('div');
  div.innerHTML = html;
  return div.textContent;
};

/**
 * Generate random ID
 * @param {number} length - Length of ID
 * @returns {string}
 */
export const randomId = (length = 8) => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
};

/**
 * Check if value is empty (null, undefined, empty string, empty array, empty object)
 * @param {any} value - Value to check
 * @returns {boolean}
 */
export const isEmpty = (value) => {
  if (value == null) return true;
  if (typeof value === 'string') return value.trim().length === 0;
  if (Array.isArray(value)) return value.length === 0;
  if (typeof value === 'object') return Object.keys(value).length === 0;
  return false;
};

/**
 * Deep clone object (simple implementation)
 * @param {Object} obj - Object to clone
 * @returns {Object}
 */
export const deepClone = (obj) => {
  return JSON.parse(JSON.stringify(obj));
};

/**
 * Get query parameter from URL
 * @param {string} param - Parameter name
 * @param {string} url - URL (defaults to current location)
 * @returns {string|null}
 */
export const getQueryParam = (param, url = window.location.href) => {
  const urlObj = new URL(url);
  return urlObj.searchParams.get(param);
};

/**
 * Set query parameter in URL
 * @param {string} param - Parameter name
 * @param {string} value - Parameter value
 * @param {boolean} pushState - Whether to push to history
 */
export const setQueryParam = (param, value, pushState = true) => {
  const url = new URL(window.location.href);
  url.searchParams.set(param, value);
  
  if (pushState) {
    window.history.pushState({}, '', url);
  } else {
    window.history.replaceState({}, '', url);
  }
};

/**
 * Remove query parameter from URL
 * @param {string} param - Parameter name
 * @param {boolean} pushState - Whether to push to history
 */
export const removeQueryParam = (param, pushState = true) => {
  const url = new URL(window.location.href);
  url.searchParams.delete(param);
  
  if (pushState) {
    window.history.pushState({}, '', url);
  } else {
    window.history.replaceState({}, '', url);
  }
};

