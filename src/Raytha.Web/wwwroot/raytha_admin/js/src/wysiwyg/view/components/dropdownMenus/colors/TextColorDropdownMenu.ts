import { SynchronizableComponentKey } from '../../base';
import { ColorDropdownMenu } from './base/ColorDropdownMenu';
import { IWithQuickButtonAction } from '../base/interfaces/IWithQuickButtonAction';

export class TextColorDropdownMenu extends ColorDropdownMenu<'textColor'> implements IWithQuickButtonAction {
   private quickActionButton: HTMLElement | null;

   protected getDefaultColor(): string {
      return '#000000';
   }

   protected getSetColorCommand(): string {
      return 'setTextColor';
   }

   protected getRemoveColorCommand(): string {
      return 'unsetTextColor';
   }

   protected getDropdownButtonSelector(): string {
      return '[data="current-text-color"]';
   }

   protected getComponentKey(): SynchronizableComponentKey {
      return 'textColor';
   }

   protected onValueChanged(newColor: string): void {
      super.onValueChanged(newColor);

      if (this.quickActionButton)
         this.quickActionButton.setAttribute(`data-${this.controllerIdentifier}-color-param`, newColor);
   }

   public withQuickActionButton(selector: string): this {
      this.quickActionButton = this.container.querySelector(selector);

      if (this.quickActionButton)
         this.quickActionButton.setAttribute(`data-${this.controllerIdentifier}-color-param`, this.getDefaultColor());

      return this;
   }

   public destroy() {
      if (this.quickActionButton)
         this.quickActionButton.remove();

      super.destroy();
   }
}