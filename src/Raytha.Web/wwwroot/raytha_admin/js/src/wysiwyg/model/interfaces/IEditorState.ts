import { Alignment } from "../types";

export interface IEditorState {
   marks: {
      bold: boolean,
      italic: boolean,
      underline: boolean,
      strike: boolean,
      superscript: boolean,
      subscript: boolean,
      code: boolean,
   },
   textStyle: {
      fontFamily: string | null,
      fontSize: string | null,
   }
   formats: {
      paragraph: boolean,
      heading1: boolean,
      heading2: boolean,
      heading3: boolean,
      heading4: boolean,
      heading5: boolean,
      heading6: boolean,
      lineHeight: string | null,
   },
   textAlign: Alignment,
   list: {
      ordered: boolean,
      bullet: boolean,
   },
   blockquote: boolean,
   codeBlock: boolean,
   invisibleCharacters: boolean,
   cursorPosition: string,
   words: number,
   nodeType: {
      isLink: boolean,
      isImage: boolean,
      isVideo: boolean,
   },
   searchResult: {
      total: number,
      index: number,
   },
   link: {
      href: string | null,
   },
   image: {
      align: Alignment,
   },
   video: {
      src: string | null,
      width: number | null,
      height: number | null,
   },
}