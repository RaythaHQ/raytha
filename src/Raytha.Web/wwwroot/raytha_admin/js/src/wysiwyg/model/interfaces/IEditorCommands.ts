import { IWordCount } from "wysiwyg/model/interfaces/IWordCount";

export interface IEditorCommands {
   //file
   setContent(content: string): boolean,
   clearContent(): boolean,
   toggleFullscreen(): boolean,
   getHTML(): string,
   getWordCount(): IWordCount,

   //edit
   undo(): boolean,
   redo(): boolean,
   copy(): boolean,
   cut(): boolean,
   paste(): boolean,
   selectAll(): boolean,

   //search and replace
   setSearchTerm(searchTerm: string): boolean,
   setReplaceTerm(replaceTerm: string): boolean,
   nextSearchResult(): boolean,
   previousSearchResult(): boolean,
   setCaseSensitive(caseSensitive: boolean): boolean,
   replace(): boolean,
   replaceAll(): boolean,

   //view
   toggleInvisibleCharacters(): boolean,

   //insert
   insertLink(url: string, text: string, title: string, openInNewWindow: boolean): boolean,
   unlink(): boolean,
   insertLinkInImage(url: string, title: string, openInNewWindow: boolean): boolean,
   insertImage(src: string, width?: string, height?: string, altText?: string): boolean,
   insertVideo(src: string, width: string, height: string): boolean,
   insertHorizontalRule(): boolean,
   insertNonbreakingSpace(): boolean,
   insertContent(content: string): boolean,

   //table
   insertTable(rows: number, cols: number): boolean,
   addRowBefore(): boolean,
   addRowAfter(): boolean,
   addColumnBefore(): boolean,
   addColumnAfter(): boolean,
   deleteRow(): boolean,
   deleteColumn(): boolean,
   deleteTable(): boolean,
   mergeCells(): boolean,
   splitCell(): boolean,
   toggleHeaderRow(): boolean,
   toggleHeaderColumn(): boolean,
   toggleHeaderCell(): boolean,
   fixTable(): boolean,

   //formats
   toggleBold(): boolean,
   toggleItalic(): boolean,
   toggleUnderline(): boolean,
   toggleStrike(): boolean,
   toggleSuperscript(): boolean,
   toggleSubscript(): boolean,
   toggleCode(): boolean,

   insertParagraph(): boolean,
   insertHeading(level: number): boolean,
   insertDiv(): boolean,
   updateCSSDiv(pos: number, cssClass: string, cssStyle: string): boolean,

   setFontFamily(fontFamily: string): boolean,
   setFontSize(fontSize: string): boolean,
   setTextAlign(textAlign: string): boolean,
   setImageAlign(align: string): boolean,
   setLineHeight(lineHeight: string): boolean,
   setTextColor(color: string): boolean,
   unsetTextColor(): boolean,
   setBackgroundColor(color: string): boolean,
   unsetBackgroundColor(): boolean,
   clearFormatting(): boolean,
   addIndent(): boolean,
   removeIndent(): boolean,
   toggleBlockquote(): boolean,
   toggleCodeBlock(): boolean,

   //list
   toggleBulletList(): boolean,
   toggleOrderedList(): boolean,
   splitListItem(): boolean,
   sinkListItem(): boolean,
   liftListItem(): boolean,

   //nodes and text
   getSelectedText(): string,
   getTextOnCursor(): string,
   isImageNode(): boolean,
   isLinkNode(): boolean,
   getImageAttributes(): Record<string, string>,
   getVideoAttributes(): Record<string, string>,
   getLinkAttributes(): Record<string, string>,
   getDivAttributes(pos: number): Record<string, string>,
}