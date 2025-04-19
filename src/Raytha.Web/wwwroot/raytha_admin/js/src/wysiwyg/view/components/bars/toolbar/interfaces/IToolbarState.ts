import { IEditorState } from 'wysiwyg/model/interfaces/IEditorState';

export interface IToolbarState {
   marks: IEditorState['marks'],
   list: IEditorState['list'],
   blockquote: IEditorState['blockquote'],
   codeBlock: IEditorState['codeBlock'],
   invisibleCharacters: IEditorState['invisibleCharacters'],
   nodeType: IEditorState['nodeType'],
}