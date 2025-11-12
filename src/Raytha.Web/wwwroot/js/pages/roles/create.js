/**
 * Roles - Create Page
 * Handles role creation with developer name sync and permission management
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';

function init() {
  // Auto-generate developer name from label
  bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName', {
    onlyIfEmpty: true
  });
  
  // Role permissions are handled by /js/shared/role-permissions.js
  // which auto-initializes based on [data-role-permissions-form] attribute
}

ready(init);

