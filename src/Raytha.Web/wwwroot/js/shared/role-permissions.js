/**
 * Role Permissions Management
 * Handles permission dependencies and auto-disable logic for role forms.
 * 
 * This module manages the complex logic where:
 * - System "content_types" permission disables all content type permissions
 * - Content type "edit" permission requires "read" permission
 * - Content type "config" permission requires both "read" and "edit" permissions
 */

import { $$, on } from '/js/core/dom.js';

/**
 * Role Permissions Manager
 */
class RolePermissionsManager {
  /**
   * @param {HTMLFormElement} form - Role form element
   */
  constructor(form) {
    this.form = form;
    this.systemPermissionCheckboxes = $$('[data-permission-type="system"]', form);
    this.contentTypeCheckboxes = $$('[data-permission-type="contenttype"]', form);
    
    this.initialize();
  }

  /**
   * Initialize permission logic
   */
  initialize() {
    // Check initial state on page load
    this.checkInitialState();
    
    // Add event listeners
    this.systemPermissionCheckboxes.forEach(checkbox => {
      on(checkbox, 'change', (e) => this.handleSystemPermissionChange(e));
    });
    
    this.contentTypeCheckboxes.forEach(checkbox => {
      on(checkbox, 'change', (e) => this.handleContentTypePermissionChange(e));
    });
  }

  /**
   * Check and enforce initial state based on current selections
   */
  checkInitialState() {
    const hasManageContentTypes = this.systemPermissionCheckboxes.some(
      cb => cb.checked && cb.dataset.permission === 'content_types'
    );

    if (hasManageContentTypes) {
      this.disableAllContentTypePermissions();
    } else {
      // Check each content type permission for dependencies
      this.contentTypeCheckboxes.forEach(checkbox => {
        if (checkbox.checked) {
          this.enforcePermissionDependencies(
            checkbox.dataset.contenttypeid,
            checkbox.dataset.permission,
            true
          );
        }
      });
    }
  }

  /**
   * Handle system permission change
   * @param {Event} event - Change event
   */
  handleSystemPermissionChange(event) {
    const permission = event.target.dataset.permission;
    
    if (permission === 'content_types') {
      if (event.target.checked) {
        this.disableAllContentTypePermissions();
      } else {
        this.enableAllContentTypePermissions();
      }
    }
  }

  /**
   * Handle content type permission change
   * @param {Event} event - Change event
   */
  handleContentTypePermissionChange(event) {
    const contentTypeId = event.target.dataset.contenttypeid;
    const permission = event.target.dataset.permission;
    const isChecked = event.target.checked;
    
    // Update the hidden input value for proper form submission
    const permissionIndex = event.target.dataset.permissionindex;
    if (permissionIndex) {
      const hiddenInput = this.form.querySelector(
        `input[type="hidden"][name="Form.ContentTypePermissions[${permissionIndex}].Selected"]`
      );
      if (hiddenInput) {
        hiddenInput.value = isChecked ? 'true' : 'false';
      }
    }
    
    this.enforcePermissionDependencies(contentTypeId, permission, isChecked);
  }

  /**
   * Enforce permission dependencies
   * @param {string} contentTypeId - Content type ID
   * @param {string} sourcePermission - Permission being changed
   * @param {boolean} isChecked - Whether permission is checked
   */
  enforcePermissionDependencies(contentTypeId, sourcePermission, isChecked) {
    if (isChecked) {
      // When enabling a permission, enable required dependencies
      if (sourcePermission === 'edit') {
        // Edit requires Read
        this.setPermission(contentTypeId, 'read', true, true);
      } else if (sourcePermission === 'config') {
        // Config requires both Read and Edit
        this.setPermission(contentTypeId, 'read', true, true);
        this.setPermission(contentTypeId, 'edit', true, true);
      }
    } else {
      // When disabling a permission, allow dependents to be unchecked
      if (sourcePermission === 'read') {
        // When unchecking Read, uncheck Edit and Config
        this.setPermission(contentTypeId, 'edit', false, false);
        this.setPermission(contentTypeId, 'config', false, false);
      } else if (sourcePermission === 'edit') {
        // When unchecking Edit, uncheck Config
        this.setPermission(contentTypeId, 'config', false, false);
      }
    }
  }

  /**
   * Set permission state
   * @param {string} contentTypeId - Content type ID
   * @param {string} permission - Permission name
   * @param {boolean|null} checked - Checked state (null = no change)
   * @param {boolean} disabled - Disabled state
   */
  setPermission(contentTypeId, permission, checked, disabled) {
    this.contentTypeCheckboxes.forEach(checkbox => {
      if (checkbox.dataset.contenttypeid === contentTypeId && 
          checkbox.dataset.permission === permission) {
        
        if (checked !== null) {
          checkbox.checked = checked;
          
          // Update hidden input
          const permissionIndex = checkbox.dataset.permissionindex;
          if (permissionIndex) {
            const hiddenInput = this.form.querySelector(
              `input[type="hidden"][name="Form.ContentTypePermissions[${permissionIndex}].Selected"]`
            );
            if (hiddenInput) {
              hiddenInput.value = checked ? 'true' : 'false';
            }
          }
        }
        
        checkbox.disabled = disabled;
      }
    });
  }

  /**
   * Disable all content type permissions
   * Called when system "content_types" permission is enabled
   */
  disableAllContentTypePermissions() {
    this.contentTypeCheckboxes.forEach(checkbox => {
      checkbox.disabled = true;
      checkbox.checked = true;
      
      // Update hidden inputs
      const permissionIndex = checkbox.dataset.permissionindex;
      if (permissionIndex) {
        const hiddenInput = this.form.querySelector(
          `input[type="hidden"][name="Form.ContentTypePermissions[${permissionIndex}].Selected"]`
        );
        if (hiddenInput) {
          hiddenInput.value = 'true';
        }
      }
    });
  }

  /**
   * Enable all content type permissions
   * Called when system "content_types" permission is disabled
   */
  enableAllContentTypePermissions() {
    this.contentTypeCheckboxes.forEach(checkbox => {
      checkbox.disabled = false;
      checkbox.checked = false;
      
      // Update hidden inputs
      const permissionIndex = checkbox.dataset.permissionindex;
      if (permissionIndex) {
        const hiddenInput = this.form.querySelector(
          `input[type="hidden"][name="Form.ContentTypePermissions[${permissionIndex}].Selected"]`
        );
        if (hiddenInput) {
          hiddenInput.value = 'false';
        }
      }
    });
  }
}

/**
 * Initialize role permissions management on form
 * @param {HTMLFormElement|string} form - Form element or selector
 * @returns {RolePermissionsManager|null}
 */
export const initRolePermissions = (form) => {
  const formEl = typeof form === 'string' ? document.querySelector(form) : form;
  if (!formEl) return null;
  
  return new RolePermissionsManager(formEl);
};

/**
 * Auto-initialize on DOM ready
 */
import { ready } from '/js/core/events.js';

ready(() => {
  const roleForm = document.querySelector('form[data-role-permissions-form]');
  if (roleForm) {
    initRolePermissions(roleForm);
  }
});

export default RolePermissionsManager;

