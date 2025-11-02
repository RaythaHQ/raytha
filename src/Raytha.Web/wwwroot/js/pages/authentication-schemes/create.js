/**
 * Authentication Schemes - Create Page
 * Handles authentication scheme creation (JWT, SAML, etc.)
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
  
  // Add any scheme-type specific logic here
  // e.g., showing/hiding fields based on auth type selection
}

ready(init);

