/**
 * Content Items - Create Page
 * Initializes all field types for content item creation
 */

import { ready } from '/js/core/events.js';
import { initAllWysiwygFields } from './wysiwyg.js';
import { initAllAttachmentFields } from './attachment.js';
import { initAllRelationshipFields } from './relationship.js';

/**
 * Initialize all content item fields
 */
function init() {
    // Get global configuration from window object
    const config = window.RaythaContentItemsConfig || {};

    console.log('Initializing content items create page...', config);

    // Initialize WYSIWYG fields
    const wysiwygFields = document.querySelectorAll('[data-field-type="wysiwyg"]');
    if (wysiwygFields.length > 0) {
        console.log(`Initializing ${wysiwygFields.length} WYSIWYG field(s)...`);
        initAllWysiwygFields(config);
    }

    // Initialize attachment upload fields
    const attachmentFields = document.querySelectorAll('[data-field-type="attachment"]');
    if (attachmentFields.length > 0) {
        console.log(`Initializing ${attachmentFields.length} attachment field(s)...`);
        initAllAttachmentFields(config);
    }

    // Initialize relationship fields
    const relationshipFields = document.querySelectorAll('[data-field-type="one_to_one_relationship"]');
    if (relationshipFields.length > 0) {
        console.log(`Initializing ${relationshipFields.length} relationship field(s)...`);
        initAllRelationshipFields(config);
    }

    console.log('Content items create page initialized.');
}

// Run on DOM ready
ready(init);

