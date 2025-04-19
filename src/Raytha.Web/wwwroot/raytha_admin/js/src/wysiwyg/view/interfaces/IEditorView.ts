import { DialogType, DialogTypeMap } from '../types/DialogTypes';
import { EditorElement } from '../enums/EditorViewComponents';
import { ModalTypeMap, ModalType } from '../types/ModalTypes';

export interface IEditorView {
   appendEditor(element: Element): void,
   getModal<T extends ModalType>(type: T): ModalTypeMap[T],
   getDialog(type: DialogType): DialogTypeMap[DialogType],
   getComponent(component: EditorElement): HTMLElement,
   getEditorContainerElement(): HTMLElement,
   destroy(): void,
}