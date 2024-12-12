import { IEditorCommands } from "wysiwyg/model/interfaces/IEditorCommands";
import { IEditorStateManager } from "wysiwyg/model/interfaces/IEditorStateManager";

export interface IEditorModel {
   getCommands(): IEditorCommands,
   getStateManager(): IEditorStateManager,
   addFileUploadFn(fileUploadCallback: (file: File) => Promise<string>): IEditorModel,
   addTableBubbleMenuElement(menu: HTMLElement): IEditorModel,
   addRightClickMenuElement(menu: HTMLElement): IEditorModel,
   addListBubbleMenuElement(menu: HTMLElement): IEditorModel,
   initializeEditor(): IEditorModel,
   getEditorElement(): HTMLElement,
   destroy(): void,
}