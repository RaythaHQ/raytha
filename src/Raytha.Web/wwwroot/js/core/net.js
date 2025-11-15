/**
 * Network/HTTP Utilities
 * Provides fetch wrappers with CSRF token support and error handling.
 */

/**
 * Get CSRF token from meta tag
 * @returns {string|null}
 */
const getCsrfToken = () => {
  const meta = document.querySelector('meta[name="request-verification-token"]');
  return meta ? meta.content : null;
};

/**
 * Get default headers including CSRF token
 * @returns {Object}
 */
const getHeaders = () => {
  const headers = {
    'Content-Type': 'application/json'
  };
  
  const token = getCsrfToken();
  if (token) {
    headers['RequestVerificationToken'] = token;
    headers['X-CSRF-TOKEN'] = token;
  }
  
  return headers;
};

/**
 * Fetch JSON with standard error handling
 * @param {string} url - Request URL
 * @param {Object} opts - Fetch options
 * @returns {Promise<any>} - Response data
 * @throws {Error} - On HTTP error
 */
export const fetchJson = async (url, opts = {}) => {
  const defaultOpts = {
    credentials: 'same-origin',
    headers: getHeaders(),
  };
  
  const finalOpts = {
    ...defaultOpts,
    ...opts,
    headers: {
      ...defaultOpts.headers,
      ...(opts.headers || {}),
    },
  };
  
  const response = await fetch(url, finalOpts);
  
  if (!response.ok) {
    const errorText = await response.text().catch(() => response.statusText);
    throw new Error(`HTTP ${response.status}: ${errorText}`);
  }
  
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return response.json();
  }
  
  return response.text();
};

/**
 * GET request
 * @param {string} url - Request URL
 * @param {Object} opts - Additional fetch options
 * @returns {Promise<any>}
 */
export const get = (url, opts = {}) => {
  return fetchJson(url, {
    method: 'GET',
    ...opts,
  });
};

/**
 * POST request
 * @param {string} url - Request URL
 * @param {any} body - Request body (will be JSON stringified)
 * @param {Object} opts - Additional fetch options
 * @returns {Promise<any>}
 */
export const post = (url, body, opts = {}) => {
  return fetchJson(url, {
    method: 'POST',
    body: JSON.stringify(body),
    ...opts,
  });
};

/**
 * PUT request
 * @param {string} url - Request URL
 * @param {any} body - Request body (will be JSON stringified)
 * @param {Object} opts - Additional fetch options
 * @returns {Promise<any>}
 */
export const put = (url, body, opts = {}) => {
  return fetchJson(url, {
    method: 'PUT',
    body: JSON.stringify(body),
    ...opts,
  });
};

/**
 * DELETE request
 * @param {string} url - Request URL
 * @param {Object} opts - Additional fetch options
 * @returns {Promise<any>}
 */
export const del = (url, opts = {}) => {
  return fetchJson(url, {
    method: 'DELETE',
    ...opts,
  });
};

/**
 * POST form data (multipart/form-data)
 * @param {string} url - Request URL
 * @param {FormData} formData - Form data
 * @returns {Promise<any>}
 */
export const postForm = async (url, formData) => {
  const token = getCsrfToken();
  const headers = {};
  if (token) {
    headers['RequestVerificationToken'] = token;
    headers['X-CSRF-TOKEN'] = token;
  }
  
  const response = await fetch(url, {
    method: 'POST',
    credentials: 'same-origin',
    headers,
    body: formData,
  });
  
  if (!response.ok) {
    const errorText = await response.text().catch(() => response.statusText);
    throw new Error(`HTTP ${response.status}: ${errorText}`);
  }
  
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return response.json();
  }
  
  return response.text();
};

/**
 * Submit form via AJAX
 * @param {HTMLFormElement} form - Form element
 * @param {Object} opts - Additional options
 * @returns {Promise<any>}
 */
export const submitForm = async (form, opts = {}) => {
  const formData = new FormData(form);
  const method = (form.method || 'POST').toUpperCase();
  const action = form.action || window.location.href;
  
  if (method === 'GET') {
    const params = new URLSearchParams(formData);
    const url = `${action}?${params}`;
    return get(url, opts);
  }
  
  return postForm(action, formData);
};

