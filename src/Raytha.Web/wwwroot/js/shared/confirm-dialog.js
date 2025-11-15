/**
 * Confirm Dialog Handler
 * Manages dynamic action URLs for confirmation modals.
 * 
 * This module handles the pattern where a button triggers a modal,
 * and the modal's form action is set dynamically based on the button's data-action-url attribute.
 */

import { $ } from '/js/core/dom.js';
import { bindDynamicModalAction } from '/js/core/modals.js';

/**
 * Initialize confirm dialog with dynamic action URL
 * @param {string} modalId - Modal element ID
 */
export const initConfirmDialog = (modalId) => {
  bindDynamicModalAction(modalId);
};

/**
 * Initialize all confirm dialogs on the page
 * Finds all modals with forms and sets up dynamic action binding
 */
export const initAllConfirmDialogs = () => {
  const modals = document.querySelectorAll('.modal[id]');
  
  modals.forEach(modal => {
    const form = modal.querySelector('form');
    if (form && modal.id) {
      initConfirmDialog(modal.id);
    }
  });
};

/**
 * Set modal form action dynamically
 * Legacy helper for specific modal IDs
 * @param {string} modalId - Modal element ID
 * @param {string} actionUrl - Form action URL
 */
export const setModalAction = (modalId, actionUrl) => {
  const modal = $(`#${modalId}`);
  if (!modal) return;
  
  const form = modal.querySelector('form');
  if (form) {
    form.action = actionUrl;
  }
};

/**
 * Show confirm modal with action URL
 * @param {string} modalId - Modal element ID
 * @param {string} actionUrl - Form action URL
 */
export const showConfirmModal = (modalId, actionUrl) => {
  setModalAction(modalId, actionUrl);
  
  const modal = $(`#${modalId}`);
  if (modal) {
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
  }
};

/**
 * Auto-initialize on DOM ready
 */
import { ready } from '/js/core/events.js';

ready(() => {
  initAllConfirmDialogs();
});

export default initConfirmDialog;

