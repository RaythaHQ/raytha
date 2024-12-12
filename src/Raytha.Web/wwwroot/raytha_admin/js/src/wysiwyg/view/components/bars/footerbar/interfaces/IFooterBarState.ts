import { IEditorState } from "wysiwyg/model/interfaces/IEditorState";

export interface IFooterBarState {
   cursorPosition: IEditorState['cursorPosition'],
   words: IEditorState['words'],
}