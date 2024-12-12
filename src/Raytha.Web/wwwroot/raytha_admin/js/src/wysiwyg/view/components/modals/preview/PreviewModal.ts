import { ModalBaseComponent } from '../../modals/base/ModalBaseComponent';
import { IPreviewModal } from './interfaces/IPreviewModal';
import template from './templates/previewModal.html';

export class PreviewModal extends ModalBaseComponent implements IPreviewModal {
   private previewContent: HTMLDivElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.previewContent = this.querySelector<HTMLDivElement>('.preview-content');
   }

   public setHTMLContent(content: string): void {
      this.previewContent.innerHTML = content;
   }
}