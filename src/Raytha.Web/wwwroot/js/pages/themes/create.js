import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Theme Create page.
 */
function initThemeCreate() {
    bindDeveloperNameSync('#Form_Title', '#Form_DeveloperName');
}

document.addEventListener('DOMContentLoaded', initThemeCreate);

