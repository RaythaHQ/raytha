/**
 * Authentication Schemes - Create Page
 * Handles authentication scheme creation (JWT, SAML, etc.)
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';
import { $ } from '/js/core/dom.js';

function updateFieldVisibility(authType) {
  const jwtFields = document.getElementById('jwt-fields');
  const samlFields = document.getElementById('saml-fields');
  const ssoUrlFields = document.getElementById('sso-url-fields');

  // Hide all conditional fields first
  if (jwtFields) jwtFields.style.display = 'none';
  if (samlFields) samlFields.style.display = 'none';
  if (ssoUrlFields) ssoUrlFields.style.display = 'none';

  // Show fields based on selected auth type
  if (authType === 'jwt') {
    if (jwtFields) jwtFields.style.display = 'block';
    if (ssoUrlFields) ssoUrlFields.style.display = 'block';
  } else if (authType === 'saml') {
    if (samlFields) samlFields.style.display = 'block';
    if (ssoUrlFields) ssoUrlFields.style.display = 'block';
  }
}

function init() {
  const labelInput = $('#Form_Label');
  const devNameInput = $('#Form_DeveloperName');

  if (labelInput && devNameInput) {
    bindDeveloperNameSync(labelInput, devNameInput, {
      onlyIfEmpty: true
    });
  }

  // Handle auth scheme type dropdown changes
  const authTypeSelect = document.getElementById('Form_AuthenticationSchemeType');

  if (authTypeSelect) {
    // Set initial visibility based on current selection
    updateFieldVisibility(authTypeSelect.value);

    // Update visibility when selection changes
    authTypeSelect.addEventListener('change', (e) => {
      updateFieldVisibility(e.target.value);
    });
  }
}

ready(init);

