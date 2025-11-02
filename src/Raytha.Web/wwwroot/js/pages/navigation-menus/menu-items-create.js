import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Navigation Menu Item Create page.
 */
function initMenuItemCreate() {
    bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName');
}

document.addEventListener('DOMContentLoaded', initMenuItemCreate);

