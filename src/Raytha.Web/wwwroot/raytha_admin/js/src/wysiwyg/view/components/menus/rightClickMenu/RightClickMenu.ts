import { EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableViewComponent } from '../../base';
import { IRightClickMenuState } from './interfaces/IRightClickMenuState';
import template from './templates/rightClickMenu.html';

export class RightClickMenu extends UpdateableViewComponent<IRightClickMenuState> {
   private addLinkButton: HTMLButtonElement;
   private editLinkButton: HTMLButtonElement;
   private unlinkButton: HTMLButtonElement;
   private openLinkButton: HTMLButtonElement;
   private imageButton: HTMLButtonElement;
   private videoButton: HTMLButtonElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected appendToContainer(): void {
      // required empty
   }

   protected initialize(): void {
      this.addLinkButton = this.querySelector<HTMLButtonElement>('#addLinkButton');
      this.editLinkButton = this.querySelector<HTMLButtonElement>('#editLinkButton');
      this.unlinkButton = this.querySelector<HTMLButtonElement>('#unlinkButton');
      this.openLinkButton = this.querySelector<HTMLButtonElement>('#openLinkButton');
      this.imageButton = this.querySelector<HTMLButtonElement>('#imageButton');
      this.videoButton = this.querySelector<HTMLButtonElement>('#videoButton');
   }

   protected getStateSelector(): EditorStateSelector<IRightClickMenuState> {
      return (state) => ({
         image: state.image,
         link: state.link,
         video: state.video,
         nodeType: state.nodeType,
      });
   }

   protected onStateChanged(newValue: IRightClickMenuState): void {
      if (newValue.nodeType.isLink) {
         this.addLinkButton.hidden = true;
         this.editLinkButton.hidden = false;
         this.unlinkButton.hidden = false;
         this.openLinkButton.hidden = false;
         this.imageButton.hidden = true;
         this.videoButton.hidden = true;
         this.openLinkButton.onclick = () => {
            if (newValue.link.href)
               window.open(newValue.link.href, '_blank');
         }
      }
      else if (newValue.nodeType.isVideo) {
         this.addLinkButton.hidden = true;
         this.editLinkButton.hidden = true;
         this.unlinkButton.hidden = true;
         this.openLinkButton.hidden = true;
         this.imageButton.hidden = true;
         this.videoButton.hidden = false;
      }
      else if (newValue.nodeType.isImage) {
         this.addLinkButton.hidden = false;
         this.imageButton.hidden = false;
         this.editLinkButton.hidden = true;
         this.unlinkButton.hidden = true;
         this.openLinkButton.hidden = true;
         this.videoButton.hidden = true;
      }
      else {
         this.addLinkButton.hidden = false;
         this.imageButton.hidden = true;
         this.editLinkButton.hidden = true;
         this.unlinkButton.hidden = true;
         this.openLinkButton.hidden = true;
         this.videoButton.hidden = true;
      }
   }
}