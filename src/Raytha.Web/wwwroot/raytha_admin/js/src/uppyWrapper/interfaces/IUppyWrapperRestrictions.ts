export interface IUppyRestrictions {
   allowedFileTypes: string[],
   maxFileSize: number,
   maxTotalFileSize?: number,
   maxNumberOfFiles?: number,
}