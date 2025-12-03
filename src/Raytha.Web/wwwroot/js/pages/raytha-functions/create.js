/**
 * Raytha Functions - Create Page
 * Handles function creation with CodeMirror JavaScript editor
 * 
 * Note: CodeMirror is loaded via import map defined in the page.
 * This controller is minimal since the editor initialization is complex
 * and uses import maps that must be in the HTML.
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';

/**
 * Initializes the Raytha Function Create page.
 * Note: CodeMirror initialization is handled inline via import map.
 */
function initRaythaFunctionCreate() {
    // Allow dots in Raytha function developer names for routes like feed.xml, robots.txt
    bindDeveloperNameSync('#Form_Name', '#Form_DeveloperName', { allowDot: true });
}

document.addEventListener('DOMContentLoaded', initRaythaFunctionCreate);

