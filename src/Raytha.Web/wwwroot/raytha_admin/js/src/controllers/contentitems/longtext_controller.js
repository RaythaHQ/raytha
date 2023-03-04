import { Controller } from 'stimulus'
import Uppy from '@uppy/core'
import AwsS3 from '@uppy/aws-s3'
import XHRUpload from '@uppy/xhr-upload'
import Swal from 'sweetalert2'

export default class extends Controller {
    static targets = ['editor', 'toolbar']
    static values = {
        usedirectuploadtocloud: Boolean,
        mimetypes: String,
        maxfilesize: Number,
        pathbase: String
    }

    connect() {
        this.boundAttachmentEvent = this.attachmentEvent.bind(this)
        addEventListener("trix-attachment-add", this.boundAttachmentEvent)
        this.modifyToolbar()
    }

    disconnect() {
        removeEventListener("trix-attachment-add", this.boundAttachmentEvent)
    }

    attachmentEvent(event) {
        if (this.editorTarget.dataset.developername == event.target.dataset.developername && event.attachment.file) {
            this.uploadFileAttachment(event.attachment)
        }
    }

    uploadFileAttachment(attachment) {
        const uppy = new Uppy({
            id: this.editorTarget.dataset.developername,
            restrictions: {
                maxFileSize: this.maxfilesizeValue,
                maxNumberOfFiles: 1,
                allowedFileTypes: this.mimetypesValue.split(",")
            },
            autoProceed: false,
            allowMultipleUploads: false
        })

        if (!this.usedirectuploadtocloud) {
            uppy.use(XHRUpload, {
                endpoint: `${this.pathbaseValue}/raytha/media-items/upload`
            })
            uppy.on('upload-success', (file, response) => {
                const URL = `${this.pathbaseValue}/raytha/media-items/objectkey/${response.body.fields.objectKey}`;
                var attributes = {
                    url: URL,
                    href: URL
                }
                attachment.setAttributes(attributes)
            })
        } else {
            uppy.use(AwsS3, {
                getUploadParameters: file => {
                    const URL = `${this.pathbaseValue}/raytha/media-items/presign`;
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
            uppy.on('upload-success', (file, response) => {
                console.log(response);
                const URL = `${this.pathbaseValue}/raytha/media-items/objectkey/${file.meta.objectKey}`;
                var attributes = {
                    url: URL,
                    href: URL
                }
                attachment.setAttributes(attributes)

                //make post call
                const CREATE_MEDIA_ENDPOINT = `${this.pathbaseValue}/raytha/media-items/create-after-upload`;
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
            })
        }

        uppy.on('upload-progress', (file) => {
            attachment.setUploadProgress(file.progress.percentage)
        })

        uppy.on('restriction-failed', (file, error) => {
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

        uppy.on('upload-error', (file, error, response) => {
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

        uppy.addFile({
            name: attachment.file.name,
            type: attachment.file.type,
            data: attachment.file
        })

        uppy.upload()
    }

    modifyToolbar() {
        const { lang } = Trix.config;
        const toolbarHTML =
            `<div class="trix-button-row">
              <span class="trix-button-group trix-button-group--text-tools" data-trix-button-group="text-tools">
                <button type="button" class="trix-button trix-button--icon trix-button--icon-bold" data-trix-attribute="bold" data-trix-key="b" title="${lang.bold}" tabindex="-1">${lang.bold}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-italic" data-trix-attribute="italic" data-trix-key="i" title="${lang.italic}" tabindex="-1">${lang.italic}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-strike" data-trix-attribute="strike" title="${lang.strike}" tabindex="-1">${lang.strike}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-link" data-trix-attribute="href" data-trix-action="link" data-trix-key="k" title="${lang.link}" tabindex="-1">${lang.link}</button>
              </span>
              <span class="trix-button-group trix-button-group--block-tools" data-trix-button-group="block-tools">
                <button type="button" class="trix-button" data-trix-attribute="heading1" title="Heading 1">H1</button>
                <button type="button" class="trix-button" data-trix-attribute="heading2" title="Heading 2">H2</button>
                <button type="button" class="trix-button" data-trix-attribute="heading3" title="Heading 3">H3</button>
                <button type="button" class="trix-button" data-trix-attribute="heading4" title="Heading 4">H4</button>
                <button type="button" class="trix-button" data-trix-attribute="heading5" title="Heading 5">H5</button>
                <button type="button" class="trix-button" data-trix-attribute="heading6" title="Heading 6">H6</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-quote" data-trix-attribute="quote" title="${lang.quote}" tabindex="-1">${lang.quote}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-code" data-trix-attribute="code" title="${lang.code}" tabindex="-1">${lang.code}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-bullet-list" data-trix-attribute="bullet" title="${lang.bullets}" tabindex="-1">${lang.bullets}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-number-list" data-trix-attribute="number" title="${lang.numbers}" tabindex="-1">${lang.numbers}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-decrease-nesting-level" data-trix-action="decreaseNestingLevel" title="${lang.outdent}" tabindex="-1">${lang.outdent}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-increase-nesting-level" data-trix-action="increaseNestingLevel" title="${lang.indent}" tabindex="-1">${lang.indent}</button>
              </span>
              <span class="trix-button-group trix-button-group--file-tools" data-trix-button-group="file-tools">
                <button type="button" class="trix-button trix-button--icon trix-button--icon-attach" data-trix-action="attachFiles" title="${lang.attachFiles}" tabindex="-1">${lang.attachFiles}</button>
              </span>
              <span class="trix-button-group-spacer"></span>
              <span class="trix-button-group trix-button-group--history-tools" data-trix-button-group="history-tools">
                <button type="button" class="trix-button trix-button--icon trix-button--icon-undo" data-trix-action="undo" data-trix-key="z" title="${lang.undo}" tabindex="-1">${lang.undo}</button>
                <button type="button" class="trix-button trix-button--icon trix-button--icon-redo" data-trix-action="redo" data-trix-key="shift+z" title="${lang.redo}" tabindex="-1">${lang.redo}</button>
              </span>
            </div>
            <div class="trix-dialogs" data-trix-dialogs>
              <div class="trix-dialog trix-dialog--link" data-trix-dialog="href" data-trix-dialog-attribute="href">
                <div class="trix-dialog__link-fields">
                  <input type="url" name="href" class="trix-input trix-input--dialog" placeholder="${lang.urlPlaceholder}" aria-label="${lang.url}" data-trix-input>
                  <div class="trix-button-group">
                    <input type="button" class="trix-button trix-button--dialog" value="${lang.link}" data-trix-method="setAttribute">
                    <input type="button" class="trix-button trix-button--dialog" value="${lang.unlink}" data-trix-method="removeAttribute">
                  </div>
                </div>
              </div>
            </div>`
        this.toolbarTarget.innerHTML = toolbarHTML;
    }
}