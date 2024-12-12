import { IModalBaseComponent, IModalWithFormSubmit } from '../../base';
import { ISourceCodeModalFormData } from './ISourceCodeModalFormData';

export interface ISourceCodeModal extends IModalBaseComponent, IModalWithFormSubmit<ISourceCodeModalFormData> {
   setHTMLContent(content: string): void,
}