import { ViewComponent } from '../../base';
import template from './templates/table.html';

export class TableDropdownMenu extends ViewComponent {
   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const tableSizeTextElement = this.querySelector('.text-center');
      const cells = this.querySelectorAll('.table-cell');
      cells.forEach(cell => {
         this.bindEvent(cell, 'mouseover', (event) => {
            const currentCell = event.target as HTMLElement;
            const currentRow = parseInt(currentCell.dataset.row!);
            const currentCol = parseInt(currentCell.dataset.col!);

            this.updateTableSizeText(currentRow, currentCol, tableSizeTextElement);

            cells.forEach(targetCell => {
               const targetRow = parseInt(targetCell.dataset.row!);
               const targetCol = parseInt(targetCell.dataset.col!);
               this.toggleClass(targetCell, 'active', targetRow <= currentRow && targetCol <= currentCol);
            });
         });
      });
   }

   private updateTableSizeText(row: number, col: number, tableSizeTextElement: HTMLElement): void {
      tableSizeTextElement.textContent = `${row} x ${col} table`;
   }
}