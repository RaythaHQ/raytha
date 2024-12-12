import { IModalBaseComponent, IModalWithFormSubmit } from '../../base';
import { ILinkModalFormData } from './ILinkModalFormData';

export interface ILinkModal extends IModalBaseComponent, IModalWithFormSubmit<ILinkModalFormData> {
   hideTextToDisplayInput(): void,
   showTextToDisplayInput(): void,
   setTextToDisplay(text: string): void,
   setUrl(url: string): void,
   setTitle(title: string): void,
   setOpenInNewTab(open: boolean): void,
   clearInputs(): void,
}