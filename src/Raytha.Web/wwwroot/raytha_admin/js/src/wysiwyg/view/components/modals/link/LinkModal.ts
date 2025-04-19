import { ModalBaseComponent } from '../base';
import { FORM_FIELDS } from './constants/linkModalFormFields';
import { ILinkModal } from './interfaces/ILinkModal';
import { ILinkModalFormData } from './interfaces/ILinkModalFormData';
import template from './templates/linkModal.html';

export class LinkModal extends ModalBaseComponent implements ILinkModal {
   private urlInput: HTMLInputElement;
   private textToDisplayInput: HTMLInputElement;
   private titleInput: HTMLInputElement;
   private openInNewTabCheckbox: HTMLInputElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const linkForm = this.querySelector<HTMLFormElement>('#linkForm');
      this.urlInput = linkForm.elements.namedItem('linkUrl') as HTMLInputElement;
      this.textToDisplayInput = linkForm.elements.namedItem('linkText') as HTMLInputElement;
      this.titleInput = linkForm.elements.namedItem('linkTitle') as HTMLInputElement;
      this.openInNewTabCheckbox = linkForm.elements.namedItem('linkOpenInNewWindow') as HTMLInputElement;
   }

   public hideTextToDisplayInput(): void {
      this.textToDisplayInput.parentElement!.hidden = true;
   }

   public showTextToDisplayInput(): void {
      this.textToDisplayInput.parentElement!.hidden = false;
   }

   public setTextToDisplay(text: string): void {
      this.textToDisplayInput.value = text;
   }

   public setUrl(url: string): void {
      this.urlInput.value = url;
   }

   public setTitle(title: string): void {
      this.titleInput.value = title;
   }

   public setOpenInNewTab(open: boolean): void {
      this.openInNewTabCheckbox.checked = open;
   }

   public clearInputs(): void {
      this.urlInput.value = '';
      this.textToDisplayInput.value = '';
      this.titleInput.value = '';
      this.openInNewTabCheckbox.checked = false;
   }

   public getFormValues(element: HTMLFormElement): ILinkModalFormData {
      const formData = new FormData(element);

      return {
         href: formData.get(FORM_FIELDS.URL) as string,
         text: formData.get(FORM_FIELDS.TEXT) as string,
         title: formData.get(FORM_FIELDS.TITLE) as string,
         openInNewTab: formData.get(FORM_FIELDS.OPEN_IN_NEW_TAB) === 'on',
      };
   }
}