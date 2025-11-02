/**
 * Content Types - Create Page
 * Handles auto-generation of developer name from label
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { ready } from '/js/core/events.js';

function init() {
  // Bind label plural to developer name sync
  bindDeveloperNameSync('#label-plural-input', '#developer-name-input', {
    onlyIfEmpty: true
  });
}

ready(init);

