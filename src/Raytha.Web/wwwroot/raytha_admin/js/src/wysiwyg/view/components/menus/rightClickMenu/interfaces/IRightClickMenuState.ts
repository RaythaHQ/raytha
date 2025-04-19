import { IEditorState } from "wysiwyg/model/interfaces/IEditorState";

export interface IRightClickMenuState {
   image: IEditorState['image'],
   link: IEditorState['link'],
   video: IEditorState['video'],
   nodeType: IEditorState['nodeType'],
}