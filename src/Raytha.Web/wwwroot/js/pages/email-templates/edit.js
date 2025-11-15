/**
 * Email Templates - Edit Page
 * Handles email template editing with CodeMirror/Liquid editor
 * 
 * Note: This page uses CodeMirror for Liquid template editing.
 * The editor initialization is handled separately with import maps.
 */

import { ready } from '/js/core/events.js';
import { $ } from '/js/core/dom.js';

function init() {
  // CodeMirror initialization is handled via import map and separate script
  // This is defined in the page's @section Scripts
  
  // Add any additional template-specific logic here
  const editorContainer = $('[data-editor-container]');
  
  if (editorContainer) {
    // Editor is initialized inline via the page's script section
    // because it requires specific CodeMirror imports via import map
  }
}

ready(init);

