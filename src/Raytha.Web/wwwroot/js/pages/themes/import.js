import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Theme Import page.
 */
function initThemeImport() {
    bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName');
}

document.addEventListener('DOMContentLoaded', initThemeImport);

