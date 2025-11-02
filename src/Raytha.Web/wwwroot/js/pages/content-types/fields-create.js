/**
 * Content Type Fields - Create Page
 * Handles field creation form logic
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';
import { $ } from '/js/core/dom.js';

function init() {
  // Find label and developer name inputs
  // Adapt these selectors based on actual form field IDs
  const labelInput = $('#Form_Label');
  const devNameInput = $('#Form_DeveloperName');
  
  if (labelInput && devNameInput) {
    bindDeveloperNameSync(labelInput, devNameInput, {
      onlyIfEmpty: true
    });
  }
}

ready(init);

