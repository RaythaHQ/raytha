import { Meta, Body, UppyFile } from "@uppy/core";

export interface IUppyWrapperOptions {
   id: string,
   useDirectUploadToCloud: boolean,
   pathbase: string,
   autoProceed: boolean,
   onUploadSuccess?: (file: UppyFile<Meta, Body> | undefined, fileUrl: string) => void,
}