/**
 * Event Utilities
 * Provides helpers for event handling, throttling, and debouncing.
 */

/**
 * Attach event listener that fires only once
 * @param {Element} el - Element to attach listener to
 * @param {string} event - Event name
 * @param {Function} handler - Event handler
 */
export const once = (el, event, handler) => {
  el.addEventListener(event, handler, { once: true });
};

/**
 * Debounce function - delays execution until after wait time has elapsed since last call
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} - Debounced function
 */
export const debounce = (func, wait = 300) => {
  let timeout;
  return function debounced(...args) {
    const context = this;
    clearTimeout(timeout);
    timeout = setTimeout(() => func.apply(context, args), wait);
  };
};

/**
 * Throttle function - limits execution to once per wait period
 * @param {Function} func - Function to throttle
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} - Throttled function
 */
export const throttle = (func, wait = 300) => {
  let timeout;
  let lastRan;
  return function throttled(...args) {
    const context = this;
    if (!lastRan) {
      func.apply(context, args);
      lastRan = Date.now();
    } else {
      clearTimeout(timeout);
      timeout = setTimeout(() => {
        if (Date.now() - lastRan >= wait) {
          func.apply(context, args);
          lastRan = Date.now();
        }
      }, wait - (Date.now() - lastRan));
    }
  };
};

/**
 * Simple event emitter/pub-sub
 */
class EventBus {
  constructor() {
    this.events = {};
  }

  /**
   * Subscribe to an event
   * @param {string} event - Event name
   * @param {Function} callback - Callback function
   * @returns {Function} - Unsubscribe function
   */
  on(event, callback) {
    if (!this.events[event]) {
      this.events[event] = [];
    }
    this.events[event].push(callback);
    
    // Return unsubscribe function
    return () => this.off(event, callback);
  }

  /**
   * Unsubscribe from an event
   * @param {string} event - Event name
   * @param {Function} callback - Callback function to remove
   */
  off(event, callback) {
    if (!this.events[event]) return;
    this.events[event] = this.events[event].filter(cb => cb !== callback);
  }

  /**
   * Emit an event
   * @param {string} event - Event name
   * @param {any} data - Event data
   */
  emit(event, data) {
    if (!this.events[event]) return;
    this.events[event].forEach(callback => callback(data));
  }

  /**
   * Subscribe to an event once
   * @param {string} event - Event name
   * @param {Function} callback - Callback function
   */
  once(event, callback) {
    const onceWrapper = (data) => {
      callback(data);
      this.off(event, onceWrapper);
    };
    this.on(event, onceWrapper);
  }
}

// Export singleton instance
export const eventBus = new EventBus();

/**
 * Wait for DOM ready
 * @param {Function} callback - Callback to execute when DOM is ready
 */
export const ready = (callback) => {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', callback);
  } else {
    callback();
  }
};

/**
 * Wait for event with timeout
 * @param {Element} el - Element to wait for event on
 * @param {string} event - Event name
 * @param {number} timeout - Timeout in milliseconds
 * @returns {Promise} - Resolves with event or rejects on timeout
 */
export const waitForEvent = (el, event, timeout = 5000) => {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      reject(new Error(`Event ${event} timed out`));
    }, timeout);

    el.addEventListener(event, function handler(e) {
      clearTimeout(timer);
      el.removeEventListener(event, handler);
      resolve(e);
    });
  });
};

