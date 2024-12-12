import { ModalBaseComponent } from '../base';
import { ISourceCodeModal, ISourceCodeModalFormData } from './interfaces';
import { FORM_FIELDS } from './constants/sourceCodeModalFormFields';
import { SOURCE_CODE_MODAL_SELECTORS } from './constants/sourceCodeModalSelectors';
import template from './templates/sourceCodeModal.html';

export class SourceCodeModal extends ModalBaseComponent implements ISourceCodeModal {
   private textarea: HTMLTextAreaElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.textarea = this.querySelector<HTMLTextAreaElement>(SOURCE_CODE_MODAL_SELECTORS.SOURCE_CODE_TEXTAREA);
   }

   public setHTMLContent(content: string): void {
      this.textarea.value = content;
   }

   public getFormValues(element: HTMLFormElement): ISourceCodeModalFormData {
      const formData = new FormData(element);

      return {
         sourceCode: formData.get(FORM_FIELDS.SOURCE_CODE) as string,
      };
   }
}