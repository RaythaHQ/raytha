import { ModalTypeMap, ModalType } from 'wysiwyg/view/types/ModalTypes';
import { IModalContainer } from './interfaces/IModalContainer';
import { ViewComponent } from '../../base';
import {
   ImageModal,
   LinkModal,
   SourceCodeModal,
   SpecialCharactersModal,
   VideoModal,
   PreviewModal,
   IModalBaseComponent,
   DivModal,
   WordCountModal,
} from '../../modals';

export class ModalContainer extends ViewComponent implements IModalContainer {
   private modals: Map<ModalType, IModalBaseComponent>;

   protected render(): HTMLElement {
      return document.createElement('div');
   }

   protected initialize(): void {
      this.modals = new Map<ModalType, IModalBaseComponent>([
         [ModalType.Link, new LinkModal(this.element, this.controllerIdentifier)],
         [ModalType.Image, new ImageModal(this.element, this.controllerIdentifier)],
         [ModalType.SourceCode, new SourceCodeModal(this.element, this.controllerIdentifier)],
         [ModalType.SpecialCharacters, new SpecialCharactersModal(this.element, this.controllerIdentifier)],
         [ModalType.Video, new VideoModal(this.element, this.controllerIdentifier)],
         [ModalType.Preview, new PreviewModal(this.element, this.controllerIdentifier)],
         [ModalType.Div, new DivModal(this.element, this.controllerIdentifier)],
         [ModalType.WordCount, new WordCountModal(this.element, this.controllerIdentifier)],
      ]);
   }

   public getModal<T extends ModalType>(type: T): ModalTypeMap[T] {
      const modal = this.modals.get(type);
      if (!modal)
         throw new Error(`Modal of type ${type} not found`);

      return modal as ModalTypeMap[T];
   }

   public destroy(): void {
      this.modals.forEach((modal) => {
         modal.destroy();
      });

      super.destroy();
   }
}