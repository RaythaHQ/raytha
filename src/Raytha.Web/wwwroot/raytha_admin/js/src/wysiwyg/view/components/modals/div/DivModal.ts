import { ModalBaseComponent } from '../base/ModalBaseComponent';
import { IDivModal, IDivModalFormData } from './interfaces';
import { DIV_MODAL_SELECTORS } from './constants/divModalSelectors';
import { FORM_FIELDS } from './constants/divModalFormFields';
import template from './templates/divModal.html';

export class DivModal extends ModalBaseComponent implements IDivModal {
   private cssInput: HTMLInputElement;
   private styleInput: HTMLInputElement;
   private posInput: HTMLInputElement;
   private pos: number;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.cssInput = this.querySelector<HTMLInputElement>(DIV_MODAL_SELECTORS.CSS_INPUT);
      this.styleInput = this.querySelector<HTMLInputElement>(DIV_MODAL_SELECTORS.STYLE_INPUT);
      this.posInput = this.querySelector<HTMLInputElement>(DIV_MODAL_SELECTORS.NODE_POS_INPUT);

      this.bindHideModal(() => {
         this.cssInput.value = '';
         this.styleInput.value = '';
         this.posInput.value = '';
      });
   }

   public getPos(): number {
      return this.pos;
   }

   public setInputs(css: string, style: string, nodePos: number): void {
      this.cssInput.value = css ?? '';
      this.styleInput.value = style ?? '';
      this.posInput.value = nodePos.toString();
   }

   public getFormValues(element: HTMLFormElement): IDivModalFormData {
      const formData = new FormData(element);
      const cssClass = formData.get(FORM_FIELDS.CSS_CLASS);
      const cssStyle = formData.get(FORM_FIELDS.CSS_STYLE);
      const nodePos = formData.get(FORM_FIELDS.NODE_POS);

      return {
         cssClass: String(cssClass),
         cssStyle: String(cssStyle),
         nodePos: Number(nodePos),
      };
   }
}