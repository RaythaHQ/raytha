import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["permissionCheckbox", "permissionValue", "systemPermission"]

    connect() {
        var hasManageContentTypes = false;

        for (let i = 0; i < this.systemPermissionTargets.length; i++) {
            var permission = this.systemPermissionTargets[i].dataset.permission;
            if (this.systemPermissionTargets[i].checked && permission === "content_types") {
                hasManageContentTypes = true;
            }
        }

        if (hasManageContentTypes) {
            this.disableAllPermissions();
        } else {
            for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
                var contentTypeId = this.permissionCheckboxTargets[i].dataset.contenttypeid;
                var permission = this.permissionCheckboxTargets[i].dataset.permission;
                if (this.permissionCheckboxTargets[i].checked) {
                    this.disablePermissions(contentTypeId, permission);
                }
                else {
                    this.enablePermissions(contentTypeId, permission);
                }
            }
        }
    }

    toggleSystemPermission(event) {
        var permission = event.currentTarget.dataset.permission;

        if (permission === "content_types") {
            if (event.currentTarget.checked) {
                this.disableAllPermissions();
            }
            else {
                this.enableAllPermissions();
            }
        }
    }

    toggleContentTypePermission(event) {
        var contentTypeId = event.currentTarget.dataset.contenttypeid;
        var permission = event.currentTarget.dataset.permission;
        var permissionIndex = event.currentTarget.dataset.permissionindex;
        var permissionValToUpdate = this.permissionValueTargets.find(x => x.getAttribute("data-permissionindex") === permissionIndex);
         
        if (event.currentTarget.checked) {
            permissionValToUpdate.value = "true";
            this.disablePermissions(contentTypeId, permission);
        }
        else {
            permissionValToUpdate.value = "false";
            this.enablePermissions(contentTypeId, permission);
        }
    }

    disablePermissions(contentTypeId, sourcePermission) {
        if (sourcePermission === "edit") {
            for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
                if (this.permissionCheckboxTargets[i].dataset.contenttypeid == contentTypeId) {
                    if (this.permissionCheckboxTargets[i].dataset.permission == "read") {
                        this.permissionCheckboxTargets[i].disabled = true;
                        this.permissionCheckboxTargets[i].checked = true;
                        this.permissionValueTargets[i].value = "true";
                    }
                }
            }
        }
        if (sourcePermission === "config") {
            for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
                if (this.permissionCheckboxTargets[i].dataset.contenttypeid == contentTypeId) {
                    if (this.permissionCheckboxTargets[i].dataset.permission == "read" || this.permissionCheckboxTargets[i].dataset.permission == "edit") {
                        this.permissionCheckboxTargets[i].disabled = true;
                        this.permissionCheckboxTargets[i].checked = true;
                        this.permissionValueTargets[i].value = "true";
                    }
                }
            }
        }
    }

    enablePermissions(contentTypeId, sourcePermission) {
        if (sourcePermission === "edit") {
            for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
                if (this.permissionCheckboxTargets[i].dataset.contenttypeid == contentTypeId) {
                    if (this.permissionCheckboxTargets[i].dataset.permission == "read") {
                        this.permissionCheckboxTargets[i].disabled = false;
                    }
                }
            }
        }
        if (sourcePermission === "config") {
            for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
                if (this.permissionCheckboxTargets[i].dataset.contenttypeid == contentTypeId) {
                    if (this.permissionCheckboxTargets[i].dataset.permission == "edit") {
                        this.permissionCheckboxTargets[i].disabled = false;
                    }
                }
            }
        }
    }

    disableAllPermissions() {
        for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
            this.permissionCheckboxTargets[i].disabled = true;
            this.permissionCheckboxTargets[i].checked = true;
            this.permissionValueTargets[i].value = "true";
        }
    }

    enableAllPermissions() {
        for (let i = 0; i < this.permissionCheckboxTargets.length; i++) {
            this.permissionCheckboxTargets[i].disabled = false;
            this.permissionCheckboxTargets[i].checked = false;
            this.permissionValueTargets[i].value = "false";
        }
    }
}