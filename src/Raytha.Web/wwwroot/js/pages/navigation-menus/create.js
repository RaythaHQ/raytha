/**
 * Navigation Menus - Create Page
 * Handles navigation menu creation
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';
import { $ } from '/js/core/dom.js';

function init() {
  const labelInput = $('#Form_Label');
  const devNameInput = $('#Form_DeveloperName');
  
  if (labelInput && devNameInput) {
    bindDeveloperNameSync(labelInput, devNameInput, {
      onlyIfEmpty: true
    });
  }
}

ready(init);

