/**
 * Role Permissions Management
 * Handles permission dependencies and auto-disable logic for role forms
 */
class RolePermissionsManager {
    constructor(formElement) {
        this.form = formElement;
        this.systemPermissionCheckboxes = this.form.querySelectorAll('[data-permission-type="system"]');
        this.contentTypeCheckboxes = this.form.querySelectorAll('[data-permission-type="contenttype"]');
        
        this.initialize();
    }

    initialize() {
        // Check initial state on page load
        this.checkInitialState();
        
        // Add event listeners
        this.systemPermissionCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => this.handleSystemPermissionChange(e));
        });
        
        this.contentTypeCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => this.handleContentTypePermissionChange(e));
        });
    }

    checkInitialState() {
        const hasManageContentTypes = Array.from(this.systemPermissionCheckboxes).some(
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
            // When disabling a permission, enable dependents
            if (sourcePermission === 'edit') {
                // When unchecking Edit, allow Read to be unchecked
                this.setPermission(contentTypeId, 'read', null, false);
            } else if (sourcePermission === 'config') {
                // When unchecking Config, allow Edit to be unchecked
                this.setPermission(contentTypeId, 'edit', null, false);
            }
        }
    }

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

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const roleForm = document.querySelector('form[data-role-permissions-form]');
    if (roleForm) {
        new RolePermissionsManager(roleForm);
    }
});

