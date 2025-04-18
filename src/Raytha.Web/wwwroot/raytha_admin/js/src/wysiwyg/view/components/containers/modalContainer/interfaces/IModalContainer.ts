import { ModalType, ModalTypeMap } from 'wysiwyg/view/types/ModalTypes';

export interface IModalContainer {
   getModal<T extends ModalType>(type: T): ModalTypeMap[T],
}