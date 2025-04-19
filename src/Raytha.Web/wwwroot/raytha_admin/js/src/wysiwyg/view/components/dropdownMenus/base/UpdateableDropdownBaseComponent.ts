import { EditorStateKeys, EditorStateValue } from 'wysiwyg/state/types';
import { UpdateableViewComponent } from 'wysiwyg/view/components';

export abstract class UpdateableDropdownMenuViewComponent<T extends EditorStateKeys> extends UpdateableViewComponent<T> {
   private activeItem: HTMLElement | null;

   protected dropdownMenuItems: Map<string, HTMLElement>;
   protected dropdownButton: HTMLElement;
   protected dropdownElementText: HTMLElement | null;

   protected abstract getCheckPlace(newValue: EditorStateValue<T>): HTMLElement | null;

   constructor(container: HTMLElement, controllerIdentifier: string, private itemAttribute: string, private dropdownTextId?: string) {
      super(container, controllerIdentifier);

      this.dropdownElementText = this.dropdownTextId
         ? this.container.querySelector(`#${this.dropdownTextId}`)
         : null;

      this.initializeCheckPlaces();
   }

   private initializeCheckPlaces(): void {
      this.dropdownMenuItems = new Map();
      Array.from(this.container.querySelectorAll('.dropdown-item')).forEach(item => {
         const key: string | undefined = item.getAttributeNames().find(name => name.includes(this.itemAttribute));
         const checkPlace: HTMLElement = (item.lastElementChild ?? item) as HTMLElement;
         const value: string | null = key ? item.getAttribute(key) : null;
         if (value) {
            this.dropdownMenuItems.set(value, checkPlace);
         }
      });
   }

   protected initialize() { }

   protected onStateChanged(newValue: EditorStateValue<T>): void {
      this.updateItems(newValue);
      this.updateDropdownText?.(newValue);
   }

   protected updateItems(newValue: EditorStateValue<T>): void {
      this.clearActiveItemIconCheck();

      if (!newValue) return;

      const checkPlace = this.getCheckPlace(newValue);
      if (checkPlace)
         this.setActiveItemIconCheck(checkPlace);
   }

   protected clearActiveItemIconCheck(): void {
      if (this.activeItem) {
         this.toggleClass(this.activeItem, 'icon-check', false);
         this.toggleClass(this.activeItem, 'icon-empty', true);
         this.activeItem = null;
      }
   }

   protected setActiveItemIconCheck(element: HTMLElement): void {
      this.toggleClass(element, 'icon-check', true);
      this.toggleClass(element, 'icon-empty', false);
      this.activeItem = element;
   }

   protected updateDropdownText?(newValue: EditorStateValue<T>): void {
      if (!this.dropdownElementText || !newValue) return;

      this.dropdownElementText.textContent = String(newValue);
   }
}