import { IImageModalFormData } from './IImageModalFormData';
import {
   IModalBaseComponent,
   IModalWithUploadContainer,
   IModalWithTabs,
   IModalWithGallery,
   IModalWithFormSubmit
} from '../../base';

export interface IImageModal extends IModalBaseComponent, IModalWithUploadContainer, IModalWithTabs, IModalWithGallery, IModalWithFormSubmit<IImageModalFormData> {
   showGeneralTab(): void,
   setInputValues(imageUrl: string, imageWidth?: string, imageHeight?: string, imageAltText?: string): void,
   clearInputs(): void,
}