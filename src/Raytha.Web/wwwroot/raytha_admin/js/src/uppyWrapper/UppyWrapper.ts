import Uppy, { Meta, UppyFile } from '@uppy/core';
import Dashboard from '@uppy/dashboard';
import XHRUpload from '@uppy/xhr-upload';
import AwsS3 from '@uppy/aws-s3';
import ImageEditor from '@uppy/image-editor';
import { IUppyWrapperDashboardConfig } from "./interfaces/IUppyWrapperDashboardOptions";
import { IUppyWrapperOptions } from "./interfaces/IUppyWrapperOptions";
import { IUppyRestrictions } from "./interfaces/IUppyWrapperRestrictions";
import { SwalAlert } from 'wysiwyg/view/SwalAlert';

import "@uppy/core/dist/style.min.css";
import "@uppy/dashboard/dist/style.min.css";
import "@uppy/image-editor/dist/style.min.css";

export class UppyWrapper {
   private uppy: Uppy;
   private uploadPromises: Map<string, {
      resolve: (url: string) => void,
      reject: (error: any) => void;
   }> = new Map();

   constructor(options: IUppyWrapperOptions) {
      this.uppy = new Uppy({
         id: options.id,
         autoProceed: options.autoProceed,
      });

      const domainUrl = options.pathbase === ""
         ? window.location.origin
         : options.pathbase;

      if (!options.useDirectUploadToCloud) {
         this.setupXHRUpload(options, domainUrl);
      }
      else {
         this.setupAwsS3Upload(options, domainUrl);
      }

      this.uppy.on("restriction-failed", (file, error) => {
         if (file?.id)
            this.uploadPromises.delete(file.id);

         SwalAlert.showErrorAlert(`File restriction: ${error.message}`);
      });

      this.uppy.on("upload-error", (file, error) => {
         const fileId = file?.id;
         if (fileId) {
            const promise = this.uploadPromises.get(fileId);
            if (promise) {
               promise.reject(error);
               this.uploadPromises.delete(fileId);
            }
         }

         SwalAlert.showErrorAlert(`Upload failed: ${error.message}`);
      });
   }

   public initializeDashboard(opts: IUppyWrapperDashboardConfig) {
      this.uppy.use(Dashboard, {
         id: opts.id,
         inline: opts.inline,
         target: opts.target,
         height: opts?.height,
         metaFields: opts?.metaFields,
      });
   }

   public initializeImageEditor() {
      this.uppy.use(ImageEditor);
   }

   public setRestrictions(restrictions: IUppyRestrictions) {
      this.uppy.setOptions({
         restrictions: {
            allowedFileTypes: restrictions.allowedFileTypes,
            maxFileSize: restrictions.maxFileSize,
            maxNumberOfFiles: restrictions.maxNumberOfFiles,
            maxTotalFileSize: restrictions.maxTotalFileSize,
         },
      });
   }

   public uploadFile(file: File): Promise<string> {
      return new Promise((resolve, reject) => {
         try {
            const id = this.uppy.addFile(file);
            this.uploadPromises.set(id, { resolve, reject });

            if (!this.uppy.opts.autoProceed)
               this.uppy.upload();
         }
         catch (error) {
            reject(error);
         }
      });
   }

   public getFile(id: string): UppyFile<Meta, Record<string, never>> {
      return this.uppy.getFile(id);
   }

   public cancelAll() {
      this.uppy.cancelAll();
   }

   public destroy() {
      this.uppy.destroy();
   }

   private setupXHRUpload(options: IUppyWrapperOptions, domainUrl: string) {
      this.uppy.use(XHRUpload, {
         endpoint: `${options.pathbase}/raytha/media-items/upload`,
      });

      this.uppy.on('upload-success', (file, response) => {
         const URL = `${domainUrl}/raytha/media-items/objectkey/${response.body?.fields['objectKey']}`;
         options.onUploadSuccess?.(file, URL);
         this.uppy.cancelAll();
         if (file) {
            const promise = this.uploadPromises.get(file.id);
            if (promise) {
               promise.resolve(URL);
               this.uploadPromises.delete(file.id);
            }
         }
      });
   }

   private setupAwsS3Upload(options: IUppyWrapperOptions, domainUrl: string) {
      this.uppy.use(AwsS3, {
         endpoint: '',
         getUploadParameters: file => {
            const URL = `${options.pathbase}/raytha/media-items/presign`;
            return fetch(URL, {
               method: 'POST',
               headers: {
                  'Accept': 'application/json',
                  'Content-Type': 'application/json'
               },
               body: JSON.stringify({
                  filename: file.name,
                  contentType: file.type,
                  extension: file.extension,
               }),
            })
               .then(response => response.json())
               .then(data => {
                  this.uppy.setFileMeta(file.id, { id: data.fields.id, objectKey: data.fields.objectKey });
                  return {
                     method: 'PUT',
                     url: data.url,
                     fields: data.fields,
                     headers: {
                        'x-ms-blob-type': 'BlockBlob'
                     },
                  };
               });
         },
      });

      this.uppy.on('upload-success', (file, _) => {
         const CREATE_MEDIA_ENDPOINT = `${options.pathbase}/raytha/media-items/create-after-upload`;

         //make post call
         fetch(CREATE_MEDIA_ENDPOINT,
            {
               method: 'POST',
               headers: {
                  'Accept': 'application/json',
                  'Content-Type': 'application/json'
               },
               body: JSON.stringify({
                  filename: file?.name,
                  contentType: file?.type,
                  extension: file?.extension,
                  id: file?.meta?.id,
                  objectKey: file?.meta?.objectKey,
                  length: file?.size
               }),
            })
            .then(response => response.json())
            .then(_ => {
               const URL = `${domainUrl}/raytha/media-items/objectkey/${file?.meta?.objectKey}`;
               this.uppy.cancelAll();
               options.onUploadSuccess?.(file, URL);

               const promise = this.uploadPromises.get(file!.id);
               if (promise) {
                  promise.resolve(URL);
                  this.uploadPromises.delete(file!.id);
               }
            });
      });
   }
}