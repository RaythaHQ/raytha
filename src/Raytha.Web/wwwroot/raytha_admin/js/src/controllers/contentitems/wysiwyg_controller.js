import { Controller } from 'stimulus'
import Uppy from '@uppy/core'
import AwsS3 from '@uppy/aws-s3'
import XHRUpload from '@uppy/xhr-upload'
import Swal from 'sweetalert2'

/* Import TinyMCE */
import tinymce from 'tinymce';

/* Default icons are required. After that, import custom icons if applicable */
import 'tinymce/icons/default';

/* Required TinyMCE components */
import 'tinymce/themes/silver';
import 'tinymce/models/dom';

import 'tinymce/plugins/advlist';
import 'tinymce/plugins/autolink';
import 'tinymce/plugins/lists';
import 'tinymce/plugins/link';
import 'tinymce/plugins/image';
import 'tinymce/plugins/charmap';
import 'tinymce/plugins/preview';
import 'tinymce/plugins/pagebreak';
import 'tinymce/plugins/nonbreaking';
import 'tinymce/plugins/table';
import 'tinymce/plugins/emoticons';
import 'tinymce/plugins/insertdatetime';
import 'tinymce/plugins/wordcount';
import 'tinymce/plugins/directionality';
import 'tinymce/plugins/fullscreen';
import 'tinymce/plugins/searchreplace';
import 'tinymce/plugins/visualblocks';
import 'tinymce/plugins/visualchars';


export default class extends Controller {
    static targets = ['editor']
    static values = { usedirectuploadtocloud: Boolean, mimetypes: String, maxfilesize: Number }

    connect() {
        console.log("Made it here");
        tinymce.init({
            target: this.editorTarget,
            plugins: 'advlist autolink lists link image charmap preview pagebreak nonbreaking table insertdatetime wordcount directionality fullscreen searchreplace visualblocks visualchars',
            toolbar: 'undo redo | styleselect | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image | print preview media fullpage | forecolor backcolor emoticons | spellchecker | table | charmap | insertdatetime | pagebreak | nonbreaking | code',
            toolbar_mode: 'floating',
            tinycomments_mode: 'embedded',
            promotion: false
        });
    }

    disconnect() {

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
                endpoint: `/raytha/media-items/upload`
            })
            uppy.on('upload-success', (file, response) => {
                const URL = `/raytha/media-items/objectkey/${response.body.fields.objectKey}`;
                var attributes = {
                    url: URL,
                    href: URL
                }
                attachment.setAttributes(attributes)
            })
        } else {
            uppy.use(AwsS3, {
                getUploadParameters: file => {
                    const URL = `/raytha/media-items/presign`;
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
                const URL = `/raytha/media-items/objectkey/${file.meta.objectKey}`;
                var attributes = {
                    url: URL,
                    href: URL
                }
                attachment.setAttributes(attributes)
                //make post call
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
}