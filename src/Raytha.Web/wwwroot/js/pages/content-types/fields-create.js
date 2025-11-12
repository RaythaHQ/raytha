/**
 * Content Type Fields - Create Page
 * Handles field creation form logic including developer name sync and choices management
 */

import { bindDeveloperNameSync } from '/js/shared/developer-name-sync.js';
import { FieldChoicesController } from '/js/shared/field-choices.js';
import { initRelatedContentTypeToggle } from '/js/shared/related-content-type.js';
import { ready } from '/js/core/events.js';
import { $ } from '/js/core/dom.js';

function init() {
  // Developer name sync
  const labelInput = $('#Form_Label');
  const devNameInput = $('#Form_DeveloperName');

  if (labelInput && devNameInput) {
    bindDeveloperNameSync(labelInput, devNameInput, {
      onlyIfEmpty: true
    });
  }

  // Field choices controller
  const fieldTypeSelect = $('#Form_FieldType');
  const choicesSection = $('#choices-section');
  const choicesList = $('#choices-list');
  const addAnotherButton = document.querySelector('[data-action="addAnother"]');

  if (fieldTypeSelect && choicesSection && choicesList) {
    new FieldChoicesController({
      fieldTypeSelect,
      choicesSection,
      choicesList,
      addAnotherButton,
      enableAutoSlug: false,
      onSlugify: null
    });
  }

  // Related content type toggle (separate from choices)
  const relatedSection = $('#related-content-type-section');
  const relatedSelect = $('#Form_RelatedContentTypeId');
  initRelatedContentTypeToggle(fieldTypeSelect, relatedSection, relatedSelect);
}

ready(init);

