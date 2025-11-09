/**
 * Content Items - Relationship Field Module
 * Provides autocomplete/typeahead for one-to-one relationship fields
 */

import { $, $$ } from '/js/core/dom.js';
import { get } from '/js/core/net.js';
import { debounce } from '/js/core/events.js';

/**
 * Initialize a relationship field with autocomplete functionality
 * @param {HTMLElement} fieldElement - The field container element
 * @param {Object} config - Configuration object
 * @param {string} config.developername - Field developer name
 * @param {string} config.relatedContentTypeId - Related content type ID
 * @param {string} config.pathBase - URL base path
 * @returns {Object} Field controller
 */
export function initRelationshipField(fieldElement, config) {
    const developername = config.developername || fieldElement.dataset.developername;
    const relatedContentTypeId = config.relatedContentTypeId || fieldElement.dataset.relatedContentTypeId;
    const pathBase = config.pathBase || '';

    const hiddenInput = fieldElement.querySelector('input[type="hidden"][name*="Value"]');
    const primaryFieldInput = fieldElement.querySelector('input[type="hidden"][name*="RelatedContentItemPrimaryField"]');
    const searchInput = fieldElement.querySelector('input[type="text"]:not([disabled])');
    const resultsList = fieldElement.querySelector('ul.list-group');
    const addButton = fieldElement.querySelector('button.btn-primary');
    const displayInput = fieldElement.querySelector('.input-group input[disabled]');
    const removeButton = fieldElement.querySelector('.input-group button');
    const spinner = fieldElement.querySelector('.spinner-border');

    if (!hiddenInput || !searchInput || !resultsList) {
        console.error('Relationship field elements not found:', developername);
        return null;
    }

    // Hide spinner initially
    if (spinner) spinner.style.display = 'none';

    // Show/hide elements based on whether a value is selected
    const updateUI = (hasValue) => {
        if (hasValue) {
            searchInput.style.display = 'none';
            resultsList.style.display = 'none';
            if (displayInput) displayInput.parentElement.style.display = 'flex';
            if (addButton) addButton.style.display = 'none';
        } else {
            searchInput.style.display = 'none';
            resultsList.style.display = 'none';
            if (displayInput) displayInput.parentElement.style.display = 'none';
            if (addButton) addButton.style.display = 'inline-flex';
        }
    };

    // Initial UI state
    updateUI(!!hiddenInput.value);

    // Add button click - show search input
    if (addButton) {
        addButton.addEventListener('click', (e) => {
            e.preventDefault();
            searchInput.style.display = 'block';
            searchInput.focus();
            addButton.style.display = 'none';
        });
    }

    // Remove button click - clear selection
    if (removeButton) {
        removeButton.addEventListener('click', (e) => {
            e.preventDefault();
            hiddenInput.value = '';
            if (primaryFieldInput) primaryFieldInput.value = '';
            if (displayInput) displayInput.value = '';
            updateUI(false);
        });
    }

    // Debounced search function
    const performSearch = debounce(async (query) => {
        if (!query || query.length < 2) {
            resultsList.innerHTML = '';
            resultsList.style.display = 'none';
            return;
        }

        try {
            if (spinner) spinner.style.display = 'inline-block';
            
            // Construct the autocomplete endpoint URL
            // The endpoint should return a list of items with id and label
            const url = `${pathBase}/raytha/relationship/autocomplete?relatedContentTypeId=${encodeURIComponent(relatedContentTypeId)}&q=${encodeURIComponent(query)}`;
            const response = await get(url);

            if (spinner) spinner.style.display = 'none';

            // Clear previous results
            resultsList.innerHTML = '';

            if (response && response.length > 0) {
                // Render results
                response.forEach(item => {
                    const li = document.createElement('li');
                    li.className = 'list-group-item list-group-item-action';
                    li.style.cursor = 'pointer';
                    li.textContent = item.label || item.value;
                    li.dataset.id = item.id || item.key;
                    li.dataset.label = item.label || item.value;

                    li.addEventListener('click', () => {
                        selectItem(item.id || item.key, item.label || item.value);
                    });

                    resultsList.appendChild(li);
                });

                resultsList.style.display = 'block';
            } else {
                const li = document.createElement('li');
                li.className = 'list-group-item text-muted';
                li.textContent = 'No results found';
                resultsList.appendChild(li);
                resultsList.style.display = 'block';
            }
        } catch (error) {
            console.error('Error fetching autocomplete results:', error);
            if (spinner) spinner.style.display = 'none';
            
            resultsList.innerHTML = '<li class="list-group-item text-danger">Error loading results</li>';
            resultsList.style.display = 'block';
        }
    }, 300);

    // Search input handler
    searchInput.addEventListener('input', (e) => {
        performSearch(e.target.value);
    });

    // Select an item from results
    const selectItem = (id, label) => {
        hiddenInput.value = id;
        if (primaryFieldInput) primaryFieldInput.value = label;
        if (displayInput) displayInput.value = label;
        searchInput.value = '';
        resultsList.innerHTML = '';
        updateUI(true);
    };

    // Click outside to close results
    document.addEventListener('click', (e) => {
        if (!fieldElement.contains(e.target)) {
            resultsList.style.display = 'none';
        }
    });

    return {
        clear: () => {
            hiddenInput.value = '';
            if (primaryFieldInput) primaryFieldInput.value = '';
            if (displayInput) displayInput.value = '';
            updateUI(false);
        },
        setValue: (id, label) => {
            selectItem(id, label);
        },
        getValue: () => hiddenInput.value,
    };
}

/**
 * Initialize all relationship fields on the page
 * @param {Object} config - Global configuration
 */
export function initAllRelationshipFields(config) {
    const fields = $$('[data-field-type="one_to_one_relationship"]');
    const controllers = [];

    fields.forEach(field => {
        const fieldConfig = {
            developername: field.dataset.developername,
            relatedContentTypeId: field.dataset.relatedContentTypeId,
            pathBase: config.pathBase || '',
        };

        const controller = initRelationshipField(field, fieldConfig);
        if (controller) {
            controllers.push(controller);
        }
    });

    return controllers;
}

