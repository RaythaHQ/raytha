/**
 * Modal/Dialog Utilities
 * Provides helpers for Bootstrap modals and confirmation dialogs.
 */

/**
 * Show Bootstrap modal
 * @param {string|Element} modalEl - Modal element or selector
 * @returns {Object} - Bootstrap Modal instance
 */
export const showModal = (modalEl) => {
  if (typeof modalEl === 'string') {
    modalEl = document.querySelector(modalEl);
  }
  
  if (!modalEl) {
    throw new Error('Modal element not found');
  }
  
  // Bootstrap 5 Modal API
  const modal = new bootstrap.Modal(modalEl);
  modal.show();
  return modal;
};

/**
 * Hide Bootstrap modal
 * @param {string|Element} modalEl - Modal element or selector
 */
export const hideModal = (modalEl) => {
  if (typeof modalEl === 'string') {
    modalEl = document.querySelector(modalEl);
  }
  
  if (!modalEl) return;
  
  const modal = bootstrap.Modal.getInstance(modalEl);
  if (modal) {
    modal.hide();
  }
};

/**
 * Confirm dialog using Bootstrap modal
 * @param {string} message - Confirmation message
 * @param {Object} options - Options
 * @param {string} options.title - Modal title
 * @param {string} options.confirmText - Confirm button text
 * @param {string} options.cancelText - Cancel button text
 * @param {string} options.confirmClass - Confirm button CSS class
 * @param {boolean} options.showWarning - Show warning alert
 * @returns {Promise<boolean>} - Resolves with true if confirmed, false if cancelled
 */
export const confirmDialog = (message, options = {}) => {
  const defaults = {
    title: 'Confirm Action',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'btn-danger',
    showWarning: false,
  };
  
  const opts = { ...defaults, ...options };
  
  return new Promise((resolve) => {
    // Create modal HTML
    const modalId = `confirm-modal-${Date.now()}`;
    const modalHtml = `
      <div class="modal fade" id="${modalId}" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">${opts.title}</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              ${opts.showWarning ? '<div class="alert alert-warning" role="alert"><strong>⚠️ Warning!</strong> This action cannot be undone.</div>' : ''}
              <p>${message}</p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${opts.cancelText}</button>
              <button type="button" class="btn ${opts.confirmClass}" data-confirm="true">${opts.confirmText}</button>
            </div>
          </div>
        </div>
      </div>
    `;
    
    // Insert modal into DOM
    const template = document.createElement('div');
    template.innerHTML = modalHtml;
    const modalEl = template.firstElementChild;
    document.body.appendChild(modalEl);
    
    // Show modal
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
    
    // Handle confirm
    const confirmBtn = modalEl.querySelector('[data-confirm]');
    confirmBtn.addEventListener('click', () => {
      modal.hide();
      resolve(true);
    });
    
    // Handle cancel/close
    modalEl.addEventListener('hidden.bs.modal', () => {
      modalEl.remove();
      resolve(false);
    }, { once: true });
  });
};

/**
 * Alert dialog
 * @param {string} message - Alert message
 * @param {Object} options - Options
 * @param {string} options.title - Modal title
 * @param {string} options.type - Alert type: 'info', 'success', 'warning', 'danger'
 * @param {string} options.buttonText - Button text
 * @returns {Promise<void>}
 */
export const alertDialog = (message, options = {}) => {
  const defaults = {
    title: 'Notice',
    type: 'info',
    buttonText: 'OK',
  };
  
  const opts = { ...defaults, ...options };
  
  return new Promise((resolve) => {
    const modalId = `alert-modal-${Date.now()}`;
    const modalHtml = `
      <div class="modal fade" id="${modalId}" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">${opts.title}</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <div class="alert alert-${opts.type}" role="alert">${message}</div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-primary" data-bs-dismiss="modal">${opts.buttonText}</button>
            </div>
          </div>
        </div>
      </div>
    `;
    
    const template = document.createElement('div');
    template.innerHTML = modalHtml;
    const modalEl = template.firstElementChild;
    document.body.appendChild(modalEl);
    
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
    
    modalEl.addEventListener('hidden.bs.modal', () => {
      modalEl.remove();
      resolve();
    }, { once: true });
  });
};

/**
 * Set modal form action dynamically when shown
 * Used for confirm dialogs that need dynamic action URLs
 * @param {string} modalId - Modal element ID
 */
export const bindDynamicModalAction = (modalId) => {
  const modalEl = document.getElementById(modalId);
  if (!modalEl) return;
  
  const form = modalEl.querySelector('form');
  if (!form) return;
  
  modalEl.addEventListener('show.bs.modal', (event) => {
    const button = event.relatedTarget;
    if (button) {
      const actionUrl = button.getAttribute('data-action-url');
      if (actionUrl) {
        form.action = actionUrl;
      }
    }
  });
};

/**
 * Initialize all modals with data-action-url support
 */
export const initDynamicModals = () => {
  const modals = document.querySelectorAll('.modal[id] form');
  modals.forEach(form => {
    const modal = form.closest('.modal');
    if (modal && modal.id) {
      bindDynamicModalAction(modal.id);
    }
  });
};

