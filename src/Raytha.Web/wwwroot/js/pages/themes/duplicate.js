import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Theme Duplicate page.
 */
function initThemeDuplicate() {
    bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName');
}

document.addEventListener('DOMContentLoaded', initThemeDuplicate);

