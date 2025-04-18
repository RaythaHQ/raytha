
import { IVideoModalFormData } from './IVideoModalFormData';
import {
   IModalBaseComponent,
   IModalWithUploadContainer,
   IModalWithGallery,
   IModalWithTabs,
   IModalWithFormSubmit
} from '../../base';

export interface IVideoModal extends IModalBaseComponent, IModalWithUploadContainer, IModalWithGallery, IModalWithTabs, IModalWithFormSubmit<IVideoModalFormData> {
   clearInputs(): void,
   setInputValues(url: string, width: string, height: string): void,
}