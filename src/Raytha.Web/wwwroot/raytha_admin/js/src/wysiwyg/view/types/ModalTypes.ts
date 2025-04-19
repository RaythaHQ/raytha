import {
   ILinkModal,
   IImageModal,
   ISourceCodeModal,
   IPreviewModal,
   IModalBaseComponent,
   IVideoModal,
   IDivModal,
   IWordCountModal,
} from '../components/modals';

export enum ModalType {
   Image,
   Link,
   Preview,
   SourceCode,
   SpecialCharacters,
   Video,
   Div,
   WordCount,
}

export type ModalTypeMap = {
   [ModalType.Link]: ILinkModal,
   [ModalType.Image]: IImageModal,
   [ModalType.SourceCode]: ISourceCodeModal,
   [ModalType.SpecialCharacters]: IModalBaseComponent,
   [ModalType.Video]: IVideoModal,
   [ModalType.Preview]: IPreviewModal,
   [ModalType.Div]: IDivModal,
   [ModalType.WordCount]: IWordCountModal,
}