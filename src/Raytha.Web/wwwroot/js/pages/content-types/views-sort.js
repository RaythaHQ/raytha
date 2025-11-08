/**
 * Content Type Views - Sort Page
 * Handles drag-and-drop reordering of view sort columns using SortableJS
 */

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
 * Initialize sortable sort column list
 */
function initSortable() {
  const listEl = document.querySelector('[data-sortable-list]');
  if (!listEl) return;

  const items = listEl.querySelectorAll('li[data-sortable-update-url]');
  if (items.length === 0) return;

  /**
   * Handle reorder event
   * @param {Event} evt - Sortable event
   */
  async function onReordered(evt) {
    const item = evt.item;
    const updateUrl = item.getAttribute('data-sortable-update-url');
    const newPosition = evt.newIndex + 1; // zero-based to 1-based

    if (!updateUrl) {
      console.error('No update URL found on item');
      return;
    }

    // Get CSRF token
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    try {
      const response = await fetch(updateUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
          position: newPosition,
          __RequestVerificationToken: token
        })
      });

      if (!response.ok) {
        showNotification('Failed to save sort order ❌', false);
      } else {
        showNotification('Sort order saved ✅', true);
      }
    } catch (error) {
      console.error('Reorder error:', error);
      showNotification('Failed to save sort order ❌', false);
    }
  }

  // Initialize SortableJS (load from CDN if not already loaded)
  if (typeof Sortable === 'undefined') {
    // Load SortableJS dynamically
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js';
    script.onload = () => {
      new Sortable(listEl, {
        animation: 150,
        ghostClass: 'bg-light',
        onEnd: onReordered
      });
    };
    document.head.appendChild(script);
  } else {
    new Sortable(listEl, {
      animation: 150,
      ghostClass: 'bg-light',
      onEnd: onReordered
    });
  }
}

function init() {
  initSortable();
}

ready(init);

