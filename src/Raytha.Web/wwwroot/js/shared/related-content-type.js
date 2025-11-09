/**
 * Related Content Type Toggle Handler
 * Shows/hides the related content type section when field type is one_to_one_relationship
 */

/**
 * Initialize related content type section toggle
 * @param {HTMLSelectElement} fieldTypeSelect - The field type select element
 * @param {HTMLElement} relatedSection - The related content type section container
 * @param {HTMLSelectElement} relatedSelect - The related content type select element
 */
export function initRelatedContentTypeToggle(fieldTypeSelect, relatedSection, relatedSelect) {
  if (!fieldTypeSelect || !relatedSection || !relatedSelect) return;
  
  /**
   * Toggle section visibility based on field type
   */
  function toggleSection() {
    const isRelationship = fieldTypeSelect.value === "one_to_one_relationship";
    relatedSection.style.display = isRelationship ? "" : "none";
    relatedSelect.required = isRelationship;
  }
  
  // Set initial state
  toggleSection();
  
  // Listen for field type changes
  fieldTypeSelect.addEventListener('change', toggleSection);
}

