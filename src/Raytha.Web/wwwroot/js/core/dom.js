/**
 * DOM Manipulation Utilities
 * Provides lightweight helpers for common DOM operations.
 */

/**
 * Query selector helper - returns first matching element
 * @param {string} sel - CSS selector
 * @param {Element|Document} root - Root element to search within
 * @returns {Element|null}
 */
export const $ = (sel, root = document) => root.querySelector(sel);

/**
 * Query selector all helper - returns array of matching elements
 * @param {string} sel - CSS selector
 * @param {Element|Document} root - Root element to search within
 * @returns {Element[]}
 */
export const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

/**
 * Add event listener
 * @param {Element} el - Element to attach listener to
 * @param {string} ev - Event name
 * @param {Function} fn - Event handler
 * @param {Object|boolean} opts - Event listener options
 */
export const on = (el, ev, fn, opts) => el.addEventListener(ev, fn, opts);

/**
 * Remove event listener
 * @param {Element} el - Element to remove listener from
 * @param {string} ev - Event name
 * @param {Function} fn - Event handler
 * @param {Object|boolean} opts - Event listener options
 */
export const off = (el, ev, fn, opts) => el.removeEventListener(ev, fn, opts);

/**
 * Event delegation helper
 * @param {Element} root - Root element to attach delegated listener
 * @param {string} ev - Event name
 * @param {string} sel - Selector to match target elements
 * @param {Function} fn - Event handler (receives event and matched target)
 * @returns {Function} - Cleanup function to remove listener
 */
export const delegate = (root, ev, sel, fn) => {
  const handler = (e) => {
    const target = e.target.closest(sel);
    if (target && root.contains(target)) {
      fn(e, target);
    }
  };
  on(root, ev, handler);
  return () => off(root, ev, handler);
};

/**
 * Show element (remove hidden attribute and d-none class)
 * @param {Element} el - Element to show
 */
export const show = (el) => {
  el.removeAttribute('hidden');
  el.classList.remove('d-none');
};

/**
 * Hide element (add hidden attribute)
 * @param {Element} el - Element to hide
 */
export const hide = (el) => {
  el.setAttribute('hidden', '');
};

/**
 * Toggle element visibility
 * @param {Element} el - Element to toggle
 * @param {boolean} [force] - Force show (true) or hide (false)
 */
export const toggle = (el, force) => {
  if (force === undefined) {
    force = el.hasAttribute('hidden');
  }
  force ? show(el) : hide(el);
};

/**
 * Add CSS class(es)
 * @param {Element} el - Element
 * @param {...string} classes - Class names to add
 */
export const addClass = (el, ...classes) => el.classList.add(...classes);

/**
 * Remove CSS class(es)
 * @param {Element} el - Element
 * @param {...string} classes - Class names to remove
 */
export const removeClass = (el, ...classes) => el.classList.remove(...classes);

/**
 * Toggle CSS class
 * @param {Element} el - Element
 * @param {string} className - Class name to toggle
 * @param {boolean} [force] - Force add (true) or remove (false)
 * @returns {boolean} - True if class is now present
 */
export const toggleClass = (el, className, force) => el.classList.toggle(className, force);

/**
 * Check if element has CSS class
 * @param {Element} el - Element
 * @param {string} className - Class name to check
 * @returns {boolean}
 */
export const hasClass = (el, className) => el.classList.contains(className);

/**
 * Set element attribute
 * @param {Element} el - Element
 * @param {string} name - Attribute name
 * @param {string} value - Attribute value
 */
export const attr = (el, name, value) => el.setAttribute(name, value);

/**
 * Get element attribute
 * @param {Element} el - Element
 * @param {string} name - Attribute name
 * @returns {string|null}
 */
export const getAttr = (el, name) => el.getAttribute(name);

/**
 * Remove element attribute
 * @param {Element} el - Element
 * @param {string} name - Attribute name
 */
export const removeAttr = (el, name) => el.removeAttribute(name);

/**
 * Serialize form to JSON object
 * @param {HTMLFormElement} form - Form element
 * @returns {Object} - Form data as key-value pairs
 */
export const formToJson = (form) => {
  const formData = new FormData(form);
  const obj = {};
  for (const [key, value] of formData.entries()) {
    if (obj[key] !== undefined) {
      // Handle multiple values for same key (e.g., checkboxes)
      if (Array.isArray(obj[key])) {
        obj[key].push(value);
      } else {
        obj[key] = [obj[key], value];
      }
    } else {
      obj[key] = value;
    }
  }
  return obj;
};

/**
 * Serialize form to URL search params
 * @param {HTMLFormElement} form - Form element
 * @returns {string} - URL-encoded query string
 */
export const serializeForm = (form) => {
  const formData = new FormData(form);
  return new URLSearchParams(formData).toString();
};

/**
 * Create element from HTML string
 * @param {string} html - HTML string
 * @returns {Element} - Created element
 */
export const createElement = (html) => {
  const template = document.createElement('template');
  template.innerHTML = html.trim();
  return template.content.firstElementChild;
};

/**
 * Remove element from DOM
 * @param {Element} el - Element to remove
 */
export const remove = (el) => el.remove();

/**
 * Empty element (remove all children)
 * @param {Element} el - Element to empty
 */
export const empty = (el) => {
  while (el.firstChild) {
    el.removeChild(el.firstChild);
  }
};

/**
 * Set text content
 * @param {Element} el - Element
 * @param {string} text - Text content
 */
export const setText = (el, text) => {
  el.textContent = text;
};

/**
 * Set HTML content
 * @param {Element} el - Element
 * @param {string} html - HTML content
 */
export const setHtml = (el, html) => {
  el.innerHTML = html;
};

/**
 * Get closest ancestor matching selector
 * @param {Element} el - Starting element
 * @param {string} sel - CSS selector
 * @returns {Element|null}
 */
export const closest = (el, sel) => el.closest(sel);

/**
 * Get element's parent
 * @param {Element} el - Element
 * @returns {Element|null}
 */
export const parent = (el) => el.parentElement;

/**
 * Get element's children as array
 * @param {Element} el - Element
 * @returns {Element[]}
 */
export const children = (el) => Array.from(el.children);

