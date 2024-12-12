import { SynchronizableComponentKey } from '../../base';
import { ColorDropdownMenu } from './base/ColorDropdownMenu';
import { IWithQuickButtonAction } from '../base/interfaces/IWithQuickButtonAction';

export class BackgroundColorDropdownMenu extends ColorDropdownMenu<'backgroundColor'> implements IWithQuickButtonAction {
   private quickActionButton: HTMLElement | null;

   protected getDefaultColor(): string {
      return '#ffffff';
   }

   protected getSetColorCommand(): string {
      return 'setBackgroundColor';
   }

   protected getRemoveColorCommand(): string {
      return 'unsetBackgroundColor';
   }

   protected getDropdownButtonSelector(): string {
      return '[data="current-background-color"]'
   }

   protected getComponentKey(): SynchronizableComponentKey {
      return 'backgroundColor';
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