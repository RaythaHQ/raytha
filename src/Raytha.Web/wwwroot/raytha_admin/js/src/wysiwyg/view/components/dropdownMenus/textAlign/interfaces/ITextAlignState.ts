import { IEditorState } from 'wysiwyg/model/interfaces/IEditorState';

export interface ITextAlignState extends Partial<IEditorState> {
   textAlign: IEditorState['textAlign'],
   isImageNode: IEditorState['nodeType']['isImage'],
   imageAlign: IEditorState['image']['align'],
}