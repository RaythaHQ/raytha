/**
 * Navigation Menu Items - Reorder Page
 * Handles drag-and-drop reordering of menu items using SortableJS
 */

import { $ } from '/js/core/dom.js';
import { ready } from '/js/core/events.js';

/**
 * Show toast notification
 * @param {string} message - Message to display
 * @param {boolean} ok - Success or error
 */
function showNotification(message, ok = true) {
  const n = document.createElement('div');
  n.textContent = message;
  Object.assign(n.style, {
    position: 'fixed',
    bottom: '20px',
    right: '20px',
    padding: '10px 16px',
    borderRadius: '6px',
    color: '#fff',
    fontWeight: '500',
    background: ok ? '#28a745' : '#dc3545',
    boxShadow: '0 2px 6px rgba(0,0,0,.2)',
    zIndex: '9999'
  });
  document.body.appendChild(n);
  setTimeout(() => n.remove(), 2500);
}

/**
 * Initialize sortable menu list
 */
function initSortable() {
  const listEl = $('#menuList');
  if (!listEl) return;

  const token = $('input[name="__RequestVerificationToken"]')?.value;
  if (!token) {
    console.error('CSRF token not found');
    return;
  }

  const ajaxUrl = listEl.getAttribute('data-ajax-url');
  if (!ajaxUrl) {
    console.error('Ajax URL not found');
    return;
  }

  /**
   * Handle reorder event
   * @param {Event} evt - Sortable event
   */
  async function onReordered(evt) {
    const id = evt.item.dataset.id;
    const newPosition = evt.newIndex + 1; // zero-based to 1-based

    try {
      const response = await fetch(ajaxUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
          id,
          position: newPosition,
          __RequestVerificationToken: token
        })
      });

      if (!response.ok) {
        showNotification('Failed to save order ❌', false);
      } else {
        showNotification('Menu order saved ✅', true);
      }
    } catch (error) {
      console.error('Reorder error:', error);
      showNotification('Failed to save order ❌', false);
    }
  }

  // Initialize SortableJS (loaded from CDN in page)
  if (typeof Sortable !== 'undefined') {
    new Sortable(listEl, {
      animation: 150,
      ghostClass: 'bg-light',
      onEnd: onReordered
    });
  } else {
    console.error('SortableJS not loaded');
  }
}

/**
 * Initialize parent select dropdown
 */
function initParentSelect() {
  const sel = $('#keySelect');
  if (!sel) return;

  const rootUrl = sel.getAttribute('data-root-url');
  const parentTpl = sel.getAttribute('data-parent-template');

  if (!rootUrl || !parentTpl) {
    console.error('Missing URL templates on select');
    return;
  }

  sel.addEventListener('change', (e) => {
    const value = e.target.value;
    const url = value
      ? parentTpl.replace('__PARENT__', encodeURIComponent(value))
      : rootUrl;

    window.location.assign(url);
  });
}

function init() {
  initSortable();
  initParentSelect();
}

ready(init);

