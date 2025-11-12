/**
 * Table Utilities
 * Provides helpers for sortable tables, bulk selection, and filtering.
 */

import { $$, on, addClass, removeClass, toggleClass } from './dom.js';

/**
 * Initialize sortable table headers
 * Adds click handlers to sortable column headers
 * @param {string|Element} tableSelector - Table element or selector
 */
export const initSortableHeaders = (tableSelector) => {
  const table = typeof tableSelector === 'string' 
    ? document.querySelector(tableSelector) 
    : tableSelector;
  
  if (!table) return;
  
  const headers = $$('th[data-sortable]', table);
  
  headers.forEach(header => {
    on(header, 'click', () => {
      const column = header.getAttribute('data-sortable');
      const currentOrder = header.getAttribute('data-order') || 'asc';
      const newOrder = currentOrder === 'asc' ? 'desc' : 'asc';
      
      // Update UI
      headers.forEach(h => {
        removeClass(h, 'sorted-asc', 'sorted-desc');
        h.removeAttribute('data-order');
      });
      
      addClass(header, `sorted-${newOrder}`);
      header.setAttribute('data-order', newOrder);
      
      // Trigger sort (emit custom event)
      table.dispatchEvent(new CustomEvent('table:sort', {
        detail: { column, order: newOrder }
      }));
    });
  });
};

/**
 * Initialize bulk select functionality
 * Handles "select all" checkbox and individual row checkboxes
 * @param {string|Element} tableSelector - Table element or selector
 * @param {Object} options - Options
 * @param {string} options.selectAllSelector - Selector for "select all" checkbox
 * @param {string} options.rowCheckboxSelector - Selector for row checkboxes
 * @param {Function} options.onSelectionChange - Callback when selection changes
 */
export const initBulkSelect = (tableSelector, options = {}) => {
  const defaults = {
    selectAllSelector: '[data-select-all]',
    rowCheckboxSelector: '[data-row-select]',
    onSelectionChange: null,
  };
  
  const opts = { ...defaults, ...options };
  
  const table = typeof tableSelector === 'string' 
    ? document.querySelector(tableSelector) 
    : tableSelector;
  
  if (!table) return;
  
  const selectAll = table.querySelector(opts.selectAllSelector);
  const rowCheckboxes = $$(opts.rowCheckboxSelector, table);
  
  if (!selectAll || rowCheckboxes.length === 0) return;
  
  // Handle select all
  on(selectAll, 'change', () => {
    const isChecked = selectAll.checked;
    rowCheckboxes.forEach(checkbox => {
      checkbox.checked = isChecked;
      toggleRowHighlight(checkbox);
    });
    
    if (opts.onSelectionChange) {
      opts.onSelectionChange(getSelectedRows(table, opts.rowCheckboxSelector));
    }
  });
  
  // Handle individual row checkboxes
  rowCheckboxes.forEach(checkbox => {
    on(checkbox, 'change', () => {
      toggleRowHighlight(checkbox);
      
      // Update select all state
      const allChecked = rowCheckboxes.every(cb => cb.checked);
      const someChecked = rowCheckboxes.some(cb => cb.checked);
      selectAll.checked = allChecked;
      selectAll.indeterminate = someChecked && !allChecked;
      
      if (opts.onSelectionChange) {
        opts.onSelectionChange(getSelectedRows(table, opts.rowCheckboxSelector));
      }
    });
  });
};

/**
 * Toggle row highlight based on checkbox state
 * @param {HTMLInputElement} checkbox - Row checkbox
 */
const toggleRowHighlight = (checkbox) => {
  const row = checkbox.closest('tr');
  if (row) {
    toggleClass(row, 'table-active', checkbox.checked);
  }
};

/**
 * Get selected row IDs
 * @param {Element} table - Table element
 * @param {string} checkboxSelector - Row checkbox selector
 * @returns {string[]} - Array of selected row IDs
 */
export const getSelectedRows = (table, checkboxSelector = '[data-row-select]') => {
  const checkboxes = $$(checkboxSelector, table);
  return checkboxes
    .filter(cb => cb.checked)
    .map(cb => cb.value || cb.getAttribute('data-id'))
    .filter(Boolean);
};

/**
 * Clear all selections
 * @param {Element} table - Table element
 * @param {string} checkboxSelector - Row checkbox selector
 */
export const clearSelection = (table, checkboxSelector = '[data-row-select]') => {
  const checkboxes = $$(checkboxSelector, table);
  checkboxes.forEach(cb => {
    cb.checked = false;
    toggleRowHighlight(cb);
  });
  
  const selectAll = table.querySelector('[data-select-all]');
  if (selectAll) {
    selectAll.checked = false;
    selectAll.indeterminate = false;
  }
};

/**
 * Simple client-side table filter
 * Filters table rows based on search text
 * @param {Element} table - Table element
 * @param {string} searchText - Text to search for
 * @param {number[]} columnIndices - Column indices to search (null = all columns)
 */
export const filterTable = (table, searchText, columnIndices = null) => {
  const tbody = table.querySelector('tbody');
  if (!tbody) return;
  
  const rows = $$('tr', tbody);
  const lowerSearch = searchText.toLowerCase();
  
  rows.forEach(row => {
    const cells = $$('td', row);
    const columnsToSearch = columnIndices 
      ? cells.filter((_, i) => columnIndices.includes(i))
      : cells;
    
    const text = columnsToSearch.map(cell => cell.textContent).join(' ').toLowerCase();
    const matches = text.includes(lowerSearch);
    
    if (matches) {
      row.style.display = '';
    } else {
      row.style.display = 'none';
    }
  });
};

/**
 * Bind search input to table filter
 * @param {string|Element} searchInput - Search input element or selector
 * @param {string|Element} table - Table element or selector
 * @param {Object} options - Options
 * @param {number} options.debounceMs - Debounce delay in ms
 * @param {number[]} options.columnIndices - Columns to search
 */
export const bindTableSearch = (searchInput, table, options = {}) => {
  const defaults = {
    debounceMs: 300,
    columnIndices: null,
  };
  
  const opts = { ...defaults, ...options };
  
  const input = typeof searchInput === 'string' 
    ? document.querySelector(searchInput) 
    : searchInput;
  
  const tableEl = typeof table === 'string' 
    ? document.querySelector(table) 
    : table;
  
  if (!input || !tableEl) return;
  
  let timeout;
  on(input, 'input', () => {
    clearTimeout(timeout);
    timeout = setTimeout(() => {
      filterTable(tableEl, input.value, opts.columnIndices);
    }, opts.debounceMs);
  });
};

/**
 * Get selected row count
 * @param {Element} table - Table element
 * @param {string} checkboxSelector - Row checkbox selector
 * @returns {number}
 */
export const getSelectedCount = (table, checkboxSelector = '[data-row-select]') => {
  return getSelectedRows(table, checkboxSelector).length;
};

/**
 * Toggle bulk action toolbar visibility based on selection
 * @param {Element} table - Table element
 * @param {string|Element} toolbar - Toolbar element or selector
 * @param {string} checkboxSelector - Row checkbox selector
 */
export const toggleBulkActionToolbar = (table, toolbar, checkboxSelector = '[data-row-select]') => {
  const toolbarEl = typeof toolbar === 'string' 
    ? document.querySelector(toolbar) 
    : toolbar;
  
  if (!toolbarEl) return;
  
  const count = getSelectedCount(table, checkboxSelector);
  
  if (count > 0) {
    toolbarEl.style.display = '';
    const countEl = toolbarEl.querySelector('[data-selected-count]');
    if (countEl) {
      countEl.textContent = count;
    }
  } else {
    toolbarEl.style.display = 'none';
  }
};

