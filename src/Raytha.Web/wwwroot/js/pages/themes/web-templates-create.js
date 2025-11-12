import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Web Template Create page.
 * Note: CodeMirror initialization and other complex logic remains inline due to import maps.
 * This controller only handles the developer name sync which can be extracted.
 */
function initWebTemplateCreate() {
    bindDeveloperNameSync('#Form_Label', '#Form_DeveloperName');
}

document.addEventListener('DOMContentLoaded', initWebTemplateCreate);

