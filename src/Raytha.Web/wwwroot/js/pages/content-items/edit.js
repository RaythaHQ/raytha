/**
 * Content Items - Edit Page
 * Handles content item editing with WYSIWYG editor and file attachments
 * 
 * This page uses TipTap for WYSIWYG editing and Uppy for file uploads.
 * These complex libraries are loaded via external scripts/CDN.
 * 
 * TODO: The Stimulus controllers (contentitems--wysiwyg, contentitems--attachment)
 * need to be replaced with vanilla JS initialization here.
 * For now, this is a placeholder marking where that logic should live.
 */

import { ready } from '/js/core/events.js';
import { $, $$ } from '/js/core/dom.js';

function init() {
  // Initialize WYSIWYG editors
  const wysiwygFields = $$('[data-controller="contentitems--wysiwyg"]');
  if (wysiwygFields.length > 0) {
    // TODO: Replace Stimulus controller with TipTap initialization
    // This requires loading TipTap via import map or CDN
    console.log('WYSIWYG fields found:', wysiwygFields.length);
    console.log('TODO: Initialize TipTap editors');
  }
  
  // Initialize attachment upload fields
  const attachmentFields = $$('[data-controller="contentitems--attachment"]');
  if (attachmentFields.length > 0) {
    // TODO: Replace Stimulus controller with Uppy initialization
    // This requires loading Uppy via import map or CDN
    console.log('Attachment fields found:', attachmentFields.length);
    console.log('TODO: Initialize Uppy uploaders');
  }
}

ready(init);

