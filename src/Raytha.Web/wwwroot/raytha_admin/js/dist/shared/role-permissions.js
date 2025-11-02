"use strict";
var RolePermissionsManager = (function () {
    function RolePermissionsManager(formElement) {
        this.form = formElement;
        this.systemPermissionCheckboxes = this.form.querySelectorAll('[data-permission-type="system"]');
        this.contentTypeCheckboxes = this.form.querySelectorAll('[data-permission-type="contenttype"]');
        this.initialize();
    }
    RolePermissionsManager.prototype.initialize = function () {
        var _this = this;
        this.checkInitialState();
        this.systemPermissionCheckboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', function (e) { return _this.handleSystemPermissionChange(e); });
        });
        this.contentTypeCheckboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', function (e) { return _this.handleContentTypePermissionChange(e); });
        });
    };
    RolePermissionsManager.prototype.checkInitialState = function () {
        var _this = this;
        var hasManageContentTypes = Array.from(this.systemPermissionCheckboxes).some(function (cb) { return cb.checked && cb.dataset.permission === 'content_types'; });
        if (hasManageContentTypes) {
            this.disableAllContentTypePermissions();
        }
        else {
            this.contentTypeCheckboxes.forEach(function (checkbox) {
                if (checkbox.checked) {
                    _this.enforcePermissionDependencies(checkbox.dataset.contenttypeid, checkbox.dataset.permission, true);
                }
            });
        }
    };
    RolePermissionsManager.prototype.handleSystemPermissionChange = function (event) {
        var permission = event.target.dataset.permission;
        if (permission === 'content_types') {
            if (event.target.checked) {
                this.disableAllContentTypePermissions();
            }
            else {
                this.enableAllContentTypePermissions();
            }
        }
    };
    RolePermissionsManager.prototype.handleContentTypePermissionChange = function (event) {
        var contentTypeId = event.target.dataset.contenttypeid;
        var permission = event.target.dataset.permission;
        var isChecked = event.target.checked;
        var permissionIndex = event.target.dataset.permissionindex;
        if (permissionIndex) {
            var hiddenInput = this.form.querySelector("input[type=\"hidden\"][name=\"Form.ContentTypePermissions[".concat(permissionIndex, "].Selected\"]"));
            if (hiddenInput) {
                hiddenInput.value = isChecked ? 'true' : 'false';
            }
        }
        this.enforcePermissionDependencies(contentTypeId, permission, isChecked);
    };
    RolePermissionsManager.prototype.enforcePermissionDependencies = function (contentTypeId, sourcePermission, isChecked) {
        if (isChecked) {
            if (sourcePermission === 'edit') {
                this.setPermission(contentTypeId, 'read', true, true);
            }
            else if (sourcePermission === 'config') {
                this.setPermission(contentTypeId, 'read', true, true);
                this.setPermission(contentTypeId, 'edit', true, true);
            }
        }
        else {
            if (sourcePermission === 'edit') {
                this.setPermission(contentTypeId, 'read', null, false);
            }
            else if (sourcePermission === 'config') {
                this.setPermission(contentTypeId, 'edit', null, false);
            }
        }
    };
    RolePermissionsManager.prototype.setPermission = function (contentTypeId, permission, checked, disabled) {
        var _this = this;
        this.contentTypeCheckboxes.forEach(function (checkbox) {
            if (checkbox.dataset.contenttypeid === contentTypeId &&
                checkbox.dataset.permission === permission) {
                if (checked !== null) {
                    checkbox.checked = checked;
                    var permissionIndex = checkbox.dataset.permissionindex;
                    if (permissionIndex) {
                        var hiddenInput = _this.form.querySelector("input[type=\"hidden\"][name=\"Form.ContentTypePermissions[".concat(permissionIndex, "].Selected\"]"));
                        if (hiddenInput) {
                            hiddenInput.value = checked ? 'true' : 'false';
                        }
                    }
                }
                checkbox.disabled = disabled;
            }
        });
    };
    RolePermissionsManager.prototype.disableAllContentTypePermissions = function () {
        var _this = this;
        this.contentTypeCheckboxes.forEach(function (checkbox) {
            checkbox.disabled = true;
            checkbox.checked = true;
            var permissionIndex = checkbox.dataset.permissionindex;
            if (permissionIndex) {
                var hiddenInput = _this.form.querySelector("input[type=\"hidden\"][name=\"Form.ContentTypePermissions[".concat(permissionIndex, "].Selected\"]"));
                if (hiddenInput) {
                    hiddenInput.value = 'true';
                }
            }
        });
    };
    RolePermissionsManager.prototype.enableAllContentTypePermissions = function () {
        var _this = this;
        this.contentTypeCheckboxes.forEach(function (checkbox) {
            checkbox.disabled = false;
            checkbox.checked = false;
            var permissionIndex = checkbox.dataset.permissionindex;
            if (permissionIndex) {
                var hiddenInput = _this.form.querySelector("input[type=\"hidden\"][name=\"Form.ContentTypePermissions[".concat(permissionIndex, "].Selected\"]"));
                if (hiddenInput) {
                    hiddenInput.value = 'false';
                }
            }
        });
    };
    return RolePermissionsManager;
}());
document.addEventListener('DOMContentLoaded', function () {
    var roleForm = document.querySelector('form[data-role-permissions-form]');
    if (roleForm) {
        new RolePermissionsManager(roleForm);
    }
});
