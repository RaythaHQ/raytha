import { IModalBaseComponent } from '../../base';

export interface IPreviewModal extends IModalBaseComponent {
   setHTMLContent(content: string): void,
}