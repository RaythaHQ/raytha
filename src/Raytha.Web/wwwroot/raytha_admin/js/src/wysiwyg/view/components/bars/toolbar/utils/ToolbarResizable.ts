import { TOOLBAR_SELECTORS } from '../constants/toolbarSelectors';
import { ButtonGroup } from '../types/toolbarButtonGroup';

export class ToolbarResizable {
   private resizeObserver: ResizeObserver;
   private previousToolbarWidth: number = 0;
   private toolbarButtonGroups: ButtonGroup[];
   private showMoreButtonGroupWidth: number;
   private hiddenButtonGroups: ButtonGroup[];

   constructor(private toolbarContainer: HTMLElement, private toolbar: HTMLElement, private hiddenToolbar: HTMLElement, private showMoreButtonGroup: HTMLElement) {
      this.hiddenButtonGroups = [];
      this.toolbarButtonGroups = [];

      this.initialHideButtonGroup();

      this.resizeObserver = new ResizeObserver((entries) => {
         entries.forEach((entry) => {
            this.moveButtonGroups(entry.contentRect.width);
         });
      });

      this.resizeObserver.observe(this.toolbarContainer);
   }

   private moveButtonGroups(toolbarContainerWidth: number): void {
      const windowWidthIncreased = toolbarContainerWidth > this.previousToolbarWidth;
      if (windowWidthIncreased) {
         while (this.hiddenButtonGroups.length > 0) {
            let buttonGroupWidthOnToolbar = this.toolbarButtonGroups.reduce((sum, item) => sum + item.width, 0);
            const hiddenButtonGroup = this.hiddenButtonGroups[this.hiddenButtonGroups.length - 1];

            if (buttonGroupWidthOnToolbar + hiddenButtonGroup.width <= toolbarContainerWidth - this.showMoreButtonGroupWidth) {
               const buttonGroup = this.hiddenButtonGroups.pop()!;
               this.showButtonGroup(buttonGroup);
               if (this.hiddenButtonGroups.length === 0)
                  this.showMoreButtonGroup.hidden = true;
            } else {
               break;
            }
         }
      } else {
         while (this.toolbarButtonGroups.length > 0) {
            let buttonGroupWidthOnToolbar = this.toolbarButtonGroups.reduce((sum, item) => sum + item.width, 0);

            if (buttonGroupWidthOnToolbar + this.showMoreButtonGroupWidth > toolbarContainerWidth) {
               const buttonGroup = this.toolbarButtonGroups.pop()!;
               this.hideButtonGroup(buttonGroup);
               this.showMoreButtonGroup.hidden = false;
            } else {
               break;
            }
         }
      }

      this.previousToolbarWidth = toolbarContainerWidth;
   }

   private initialHideButtonGroup(): void {
      const toolbarButtons = Array.from(this.toolbar.querySelectorAll(TOOLBAR_SELECTORS.TOOLBAR_BUTTON_GROUP)) as HTMLElement[];
      const toolbarWidth = this.toolbar.getBoundingClientRect().width;
      this.showMoreButtonGroupWidth = this.showMoreButtonGroup.getBoundingClientRect().width + parseFloat(getComputedStyle(this.showMoreButtonGroup).marginRight);

      let indexStart = toolbarButtons.length;
      let buttonGroupWidthOnToolbar = 0;
      let indexIsFound = false;

      for (let i = 0; i < toolbarButtons.length; i++) {
         const buttonGroupWidth = toolbarButtons[i].getBoundingClientRect().width + parseInt(getComputedStyle(toolbarButtons[i]).marginRight)
         buttonGroupWidthOnToolbar += buttonGroupWidth;

         this.toolbarButtonGroups.push({
            element: toolbarButtons[i],
            width: buttonGroupWidth,
         });

         if (buttonGroupWidthOnToolbar > toolbarWidth - this.showMoreButtonGroupWidth && !indexIsFound) {
            indexStart = i;
            indexIsFound = true;
         }
      }

      for (let i = this.toolbarButtonGroups.length - 1; i >= indexStart; i--) {
         const buttonGroup = this.toolbarButtonGroups[i];
         this.hideButtonGroup(buttonGroup);
      }

      this.toolbarButtonGroups = this.toolbarButtonGroups.splice(0, indexStart);
   }

   private hideButtonGroup(buttonGroup: ButtonGroup): void {
      buttonGroup.element.classList.add('mt-2');
      this.hiddenToolbar.insertBefore(buttonGroup.element, this.hiddenToolbar.firstChild);
      this.hiddenButtonGroups.push(buttonGroup);
   }

   private showButtonGroup(buttonGroup: ButtonGroup): void {
      buttonGroup.element.classList.remove('mt-2');
      this.toolbar.insertBefore(buttonGroup.element, this.showMoreButtonGroup);
      this.toolbarButtonGroups.push(buttonGroup);
   }

   public destroy(): void {
      this.resizeObserver.disconnect();
   }
}