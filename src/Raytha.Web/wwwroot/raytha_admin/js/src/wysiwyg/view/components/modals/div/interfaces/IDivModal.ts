import { IDivModalFormData } from './IDivModalWithFormSubmit';
import {
   IModalBaseComponent,
   IModalWithFormSubmit
} from '../../base';

export interface IDivModal extends IModalBaseComponent, IModalWithFormSubmit<IDivModalFormData> {
   getPos(): number,
   setInputs(css: string, style: string, nodePos: number): void,
}