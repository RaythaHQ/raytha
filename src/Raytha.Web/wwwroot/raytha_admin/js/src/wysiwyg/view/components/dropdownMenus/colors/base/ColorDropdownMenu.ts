import { ISynchronizableComponent, SynchronizableViewComponent } from '../../../base';
import template from '../templates/colorPalette.html';

type ColorComponentKey = keyof Pick<ISynchronizableComponent, 'textColor' | 'backgroundColor'>;

export abstract class ColorDropdownMenu<T extends ColorComponentKey> extends SynchronizableViewComponent<T> {
   protected dropdownItems: Map<string, HTMLElement>;
   protected dropdownButton: HTMLElement | null;
   protected selectedItem: HTMLElement | null;

   protected abstract getDefaultColor(): string;
   protected abstract getSetColorCommand(): string;
   protected abstract getRemoveColorCommand(): string;
   protected abstract getDropdownButtonSelector(): string;

   protected render(): HTMLElement {
      const updatedTemplate = template
         .replaceAll('{{setColorCommand}}', this.getSetColorCommand())
         .replaceAll('{{removeColorCommand}}', this.getRemoveColorCommand())
         .trim();

      return this.createElementFromTemplate(updatedTemplate);
   }

   protected initialize(): void {
      this.initializeDropdownButton();
      this.initializeDropdownItems();
      this.setInitialColor();
   }

   protected onValueChanged(newColor: ISynchronizableComponent[T]): void {
      this.updateUI(newColor);
   }

   private initializeDropdownButton(): void {
      const selector = this.getDropdownButtonSelector();
      this.dropdownButton = selector
         ? this.container.querySelector(selector)
         : null;
   }

   private initializeDropdownItems(): void {
      this.selectedItem = null;
      this.dropdownItems = new Map();

      Array.from(this.querySelectorAll('.dropdown-item')).map(item => {
         const color = item.getAttribute(`data-${this.controllerIdentifier}-color-param`);

         if (color) {
            this.dropdownItems.set(color, item);
            this.bindEvent(item, 'click', () => this.handleColorSelection(color));
         }
      });
   }

   private setInitialColor(): void {
      const defaultColor = this.getDefaultColor();
      this.setValue(defaultColor);
      this.onValueChanged(defaultColor);
      this.updateUI(defaultColor);
   }

   private handleColorSelection(newColor: string): void {
      this.setValue(newColor);
   }

   private updateUI(newColor: string): void {
      if (this.selectedItem)
         this.selectedItem.classList.remove('color-icon-check');

      const newSelectedItem = this.dropdownItems.get(newColor);
      if (newSelectedItem) {
         newSelectedItem.classList.add('color-icon-check');

         if (this.dropdownButton)
            this.dropdownButton.setAttribute('fill', newColor);

         this.selectedItem = newSelectedItem;
      }
   }

   public destroy(): void {
      this.dropdownItems.clear();
      super.destroy();
   }
}