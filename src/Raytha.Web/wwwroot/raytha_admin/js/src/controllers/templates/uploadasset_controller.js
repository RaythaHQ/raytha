import { Controller } from 'stimulus'
import Uppy from '@uppy/core'
import AwsS3 from '@uppy/aws-s3'
import XHRUpload from '@uppy/xhr-upload'
import Dashboard from '@uppy/dashboard';
import Swal from 'sweetalert2'
import * as bootstrap from 'bootstrap'

import '@uppy/core/dist/style.min.css';
import '@uppy/dashboard/dist/style.min.css';

export default class extends Controller {
    static values = {
        fieldid: String,
        usedirectuploadtocloud: Boolean,
        mimetypes: String,
        maxfilesize: Number,
        pathbase: String,
        themeid: String,
    }

    static targets = ['uppyContainer', 'uppyCopyUrls', 'toast']

    connect() {
        this.uppy = new Uppy({
            id: this.fieldidValue,
            restrictions: {
                maxFileSize: this.maxfilesizeValue,
                maxNumberOfFiles: 10,
                allowedFileTypes: this.mimetypesValue.split(",")
            },
            autoProceed: true,
            allowMultipleUploads: true
        })

        this.uppy.use(Dashboard, {
            inline: true,
            target: `#${this.fieldidValue}-uppy`,
            height: 320
        })

        if (!this.usedirectuploadtocloudValue) {
            this.uppy.use(XHRUpload, {
                endpoint: `${this.pathbaseValue}/raytha/media-items/upload?themeId=${this.themeidValue}`
            })
            this.uppy.on('upload-success', (file, response) => {
                var item = {
                    name: response.body.fields.fileName,
                    meta: {
                        objectKey: response.body.fields.objectKey
                    }
                };
                this.addCopyUrlItem(item);
            })
        } else {
            this.uppy.use(AwsS3, {
                getUploadParameters: file => {
                    const URL = `${this.pathbaseValue}/raytha/media-items/presign?themeId=${this.themeidValue}`;
                    return fetch(URL, {
                        method: 'POST',
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            filename: file.name,
                            contentType: file.type,
                            extension: file.extension
                        })
                    })
                    .then(response => response.json())
                    .then(data => {
                        return {
                            method: 'PUT',
                            url: data.url,
                            fields: data.fields,
                            headers: {
                                'x-ms-blob-type': 'BlockBlob'
                            }
                        }
                    });
                }
            })
            this.uppy.on('upload-success', (file, response) => {
                console.log(response);
                const CREATE_MEDIA_ENDPOINT = `${this.pathbaseValue}/raytha/media-items/create-after-upload?themeId=${this.themeidValue}`;

                //make post call
                fetch(CREATE_MEDIA_ENDPOINT, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        filename: file.name,
                        contentType: file.type,
                        extension: file.extension,
                        id: file.meta.id,
                        objectKey: file.meta.objectKey,
                        length: file.size
                    })
                })
                .then(response => response.json())
                .then(data => {
                    this.addCopyUrlItem(file);
                });
            })
        }

        this.uppy.on('restriction-failed', (file, error) => {
            Swal.fire({
                title: "File Restriction",
                text: error,
                showConfirmButton: false,
                showCloseButton: true,
                showCancelButton: true,
                cancelButtonText: "OK",
                icon: "error"
            });
        })

        this.uppy.on('upload-error', (file, error, response) => {
            console.log('error with file:', file.id)
            console.log('error message:', error)
            Swal.fire({
                title: "Upload failed",
                text: error,
                showConfirmButton: false,
                showCloseButton: true,
                showCancelButton: true,
                cancelButtonText: "OK",
                icon: "error"
            });
        })
    }

    disconnect() {
       this.uppy.close();
    }

    addCopyUrlItem(file) {
        var li = document.createElement("li");
        var truncated = file.name.replace(/(.{20})..+/, "$1&hellip;");
        li.setAttribute("class", "list-group-item");
        li.innerHTML = `<a target="_blank" class="text-secondary" href="${this.pathbaseValue}/raytha/media-items/objectkey/${file.meta.objectKey}">${truncated}</a> | <a href="javascript:void(0);" class="text-primary" data-action="templates--uploadasset#copyToClipboard" data-id="${file.meta.objectKey}" data-copyas="public">Copy as public</a> | <a href="javascript:void(0);"  data-action="templates--uploadasset#copyToClipboard" class="text-primary" data-copyas="redirect" data-id="${file.meta.objectKey}">Copy as redirect</a>`;
        this.uppyCopyUrlsTarget.appendChild(li);
    }

    copyToClipboard(event) {
        var toast = new bootstrap.Toast(this.toastTarget);
        if (event.target.dataset.copyas == "public") {
            var urlToCopy = `{{ "${event.target.dataset.id}" | attachment_public_url }}`;
            navigator.clipboard.writeText(urlToCopy).then(function () {
                toast.show();
            });
        } else {
            var urlToCopy = `{{ "${event.target.dataset.id}" | attachment_redirect_url }}`;
            navigator.clipboard.writeText(urlToCopy).then(function () {
                toast.show();
            });
        }
    }
}