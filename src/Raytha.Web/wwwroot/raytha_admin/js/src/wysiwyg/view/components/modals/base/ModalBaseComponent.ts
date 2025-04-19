import { ViewComponent } from '../../base';
import { IModalBaseComponent } from './interfaces/IModalBaseComponent';
import { Modal } from 'bootstrap'

export abstract class ModalBaseComponent extends ViewComponent implements IModalBaseComponent {
   private modalInstance: Modal;

   constructor(container: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);
      this.modalInstance = Modal.getOrCreateInstance(this.element);
   }

   public bindShowModal(handler: (event: Event) => void): void {
      this.bindEvent(this.element, 'show.bs.modal', handler);
   }

   public bindHideModal(handler: (event: Event) => void): void {
      this.bindEvent(this.element, 'hide.bs.modal', handler);
   }

   public show(): void {
      this.modalInstance.show();
   }

   public hide() {
      this.modalInstance.hide();
   }

   public destroy(): void {
      this.modalInstance.dispose();
      super.destroy();
   }
}