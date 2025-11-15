/**
 * Form Validation Utilities
 * Provides helpers for client-side form validation with Bootstrap styling.
 */

/**
 * Validate required field
 * @param {HTMLInputElement|HTMLTextAreaElement|HTMLSelectElement} field - Form field
 * @returns {boolean} - True if valid
 */
export const validateRequired = (field) => {
  if (!field.hasAttribute('required')) {
    return true;
  }
  
  const value = field.value.trim();
  return value.length > 0;
};

/**
 * Validate pattern (regex)
 * @param {HTMLInputElement} field - Form field
 * @returns {boolean} - True if valid
 */
export const validatePattern = (field) => {
  const pattern = field.getAttribute('pattern');
  if (!pattern) {
    return true;
  }
  
  const regex = new RegExp(pattern);
  return regex.test(field.value);
};

/**
 * Validate email format
 * @param {string} email - Email address
 * @returns {boolean} - True if valid
 */
export const validateEmail = (email) => {
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return regex.test(email);
};

/**
 * Validate min length
 * @param {HTMLInputElement|HTMLTextAreaElement} field - Form field
 * @returns {boolean} - True if valid
 */
export const validateMinLength = (field) => {
  const minLength = parseInt(field.getAttribute('minlength'), 10);
  if (isNaN(minLength)) {
    return true;
  }
  
  return field.value.length >= minLength;
};

/**
 * Validate max length
 * @param {HTMLInputElement|HTMLTextAreaElement} field - Form field
 * @returns {boolean} - True if valid
 */
export const validateMaxLength = (field) => {
  const maxLength = parseInt(field.getAttribute('maxlength'), 10);
  if (isNaN(maxLength)) {
    return true;
  }
  
  return field.value.length <= maxLength;
};

/**
 * Show field error (Bootstrap style)
 * @param {HTMLElement} field - Form field
 * @param {string} message - Error message
 */
export const showFieldError = (field, message) => {
  // Add is-invalid class
  field.classList.add('is-invalid');
  field.classList.remove('is-valid');
  
  // Find or create invalid-feedback element
  let feedback = field.parentElement.querySelector('.invalid-feedback');
  if (!feedback) {
    feedback = document.createElement('div');
    feedback.className = 'invalid-feedback';
    field.parentElement.appendChild(feedback);
  }
  
  feedback.textContent = message;
  feedback.style.display = 'block';
};

/**
 * Clear field error
 * @param {HTMLElement} field - Form field
 */
export const clearFieldError = (field) => {
  field.classList.remove('is-invalid');
  
  const feedback = field.parentElement.querySelector('.invalid-feedback');
  if (feedback) {
    feedback.style.display = 'none';
  }
};

/**
 * Show field as valid
 * @param {HTMLElement} field - Form field
 */
export const showFieldValid = (field) => {
  field.classList.remove('is-invalid');
  field.classList.add('is-valid');
  
  const feedback = field.parentElement.querySelector('.invalid-feedback');
  if (feedback) {
    feedback.style.display = 'none';
  }
};

/**
 * Clear all form errors
 * @param {HTMLFormElement} form - Form element
 */
export const clearFormErrors = (form) => {
  const fields = form.querySelectorAll('.is-invalid');
  fields.forEach(field => clearFieldError(field));
  
  const feedbacks = form.querySelectorAll('.invalid-feedback');
  feedbacks.forEach(feedback => {
    feedback.style.display = 'none';
  });
};

/**
 * Validate single field
 * @param {HTMLElement} field - Form field
 * @returns {boolean} - True if valid
 */
export const validateField = (field) => {
  // Clear previous errors
  clearFieldError(field);
  
  // Check required
  if (!validateRequired(field)) {
    showFieldError(field, 'This field is required.');
    return false;
  }
  
  // Skip other validations if empty and not required
  if (!field.value.trim() && !field.hasAttribute('required')) {
    return true;
  }
  
  // Check pattern
  if (!validatePattern(field)) {
    showFieldError(field, 'Please match the requested format.');
    return false;
  }
  
  // Check email type
  if (field.type === 'email' && !validateEmail(field.value)) {
    showFieldError(field, 'Please enter a valid email address.');
    return false;
  }
  
  // Check min length
  if (!validateMinLength(field)) {
    const minLength = field.getAttribute('minlength');
    showFieldError(field, `Minimum length is ${minLength} characters.`);
    return false;
  }
  
  // Check max length
  if (!validateMaxLength(field)) {
    const maxLength = field.getAttribute('maxlength');
    showFieldError(field, `Maximum length is ${maxLength} characters.`);
    return false;
  }
  
  // Show as valid
  showFieldValid(field);
  return true;
};

/**
 * Validate entire form
 * @param {HTMLFormElement} form - Form element
 * @returns {boolean} - True if all fields valid
 */
export const validateForm = (form) => {
  const fields = form.querySelectorAll('input, textarea, select');
  let isValid = true;
  
  fields.forEach(field => {
    if (!validateField(field)) {
      isValid = false;
    }
  });
  
  return isValid;
};

/**
 * Bind validation to form fields on blur
 * @param {HTMLFormElement} form - Form element
 */
export const bindFieldValidation = (form) => {
  const fields = form.querySelectorAll('input, textarea, select');
  
  fields.forEach(field => {
    field.addEventListener('blur', () => {
      validateField(field);
    });
    
    // Clear error on input
    field.addEventListener('input', () => {
      if (field.classList.contains('is-invalid')) {
        clearFieldError(field);
      }
    });
  });
};

/**
 * Bind form submit validation
 * @param {HTMLFormElement} form - Form element
 * @param {Function} onValid - Callback if form is valid
 * @returns {Function} - Cleanup function
 */
export const bindFormValidation = (form, onValid) => {
  const handler = (e) => {
    e.preventDefault();
    
    if (validateForm(form)) {
      onValid(e);
    } else {
      // Focus first invalid field
      const firstInvalid = form.querySelector('.is-invalid');
      if (firstInvalid) {
        firstInvalid.focus();
      }
    }
  };
  
  form.addEventListener('submit', handler);
  
  // Return cleanup function
  return () => form.removeEventListener('submit', handler);
};

