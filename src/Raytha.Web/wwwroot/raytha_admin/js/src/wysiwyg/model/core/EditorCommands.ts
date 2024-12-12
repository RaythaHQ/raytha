import { Editor } from '@tiptap/core';
import { IEditorCommands } from 'wysiwyg/model/interfaces/IEditorCommands';
import { ImageAttributes } from 'wysiwyg/model/extensions/image/image';
import { IWordCount } from 'wysiwyg/model/interfaces/IWordCount';
import { DEFAULT_STYLES } from '../constants/defaultStyles';

export class EditorCommands implements IEditorCommands {
   private readonly YOUTUBE_REGEX = /^(https?:\/\/)?(www\.|music\.)?(youtube\.com|youtu\.be)(.+)?$/;
   private readonly VIMEO_REGEX = /^(https?:\/\/)?(www\.)?(vimeo\.com\/\d+|player\.vimeo\.com\/video\/\d+)(\/?.*)?$/;
   private readonly NESTED_IFRAME = /<(\w+)>\s*(<iframe[^>]*>[\s\S]*?<\/iframe>)\s*<\/\1>/g;

   constructor(private editor: Editor) { }

   public setContent(content: string): boolean {
      const fixedContent = this.fixContent(content);

      return this.editor.commands.setContent(fixedContent, false, {
         preserveWhitespace: "full",
      });
   }

   public clearContent(): boolean {
      return this.editor.chain().focus().clearContent().run();
   }

   public toggleFullscreen(): boolean {
      return this.editor.chain().focus().toggleFullscreen().run();
   }

   public getHTML(): string {
      return this.editor.getHTML();
   }

   public getWordCount(): IWordCount {
      this.editor.chain().focus().run();

      const extension = this.editor.storage.characterCount;
      return {
         document: {
            words: extension.words(),
            characters: extension.charactersWithoutLineBreaks(),
            charactersWithoutSpaces: extension.charactersWithoutSpaces(),
         },
         selection: {
            words: extension.selectionWords(),
            characters: extension.selectionCharacters(),
            charactersWithoutSpaces: extension.selectionCharactersWithoutSpaces(),
         }
      } as IWordCount;
   }

   public undo(): boolean {
      return this.editor.chain().focus().undo().run();
   }

   public redo(): boolean {
      return this.editor.chain().focus().redo().run();
   }

   public copy(): boolean {
      return this.editor.chain().focus().copy().run();
   }

   public cut(): boolean {
      return this.editor.chain().focus().cutContent().run();
   }

   public paste(): boolean {
      return this.editor.chain().focus().paste().run();
   }

   public selectAll(): boolean {
      return this.editor.chain().focus().selectAll().run();
   }

   public setSearchTerm(searchTerm: string): boolean {
      return this.editor.commands.setSearchTerm(searchTerm);
   }

   public setReplaceTerm(replaceTerm: string): boolean {
      return this.editor.commands.setReplaceTerm(replaceTerm);
   }

   public nextSearchResult(): boolean {
      return this.editor.commands.nextSearchResult();
   }

   public previousSearchResult(): boolean {
      return this.editor.commands.previousSearchResult();
   }

   public setCaseSensitive(caseSensitive: boolean): boolean {
      return this.editor.chain().focus().setCaseSensitive(caseSensitive).run();
   }

   public replace(): boolean {
      return this.editor.chain().focus().replace().run();
   }

   public replaceAll(): boolean {
      return this.editor.chain().focus().replaceAll().run();
   }

   public toggleInvisibleCharacters(): boolean {
      return this.editor.chain().focus().toggleInvisibleCharacters().run();
   }

   public insertLink(url: string, text: string, title: string, openInNewWindow: boolean): boolean {
      const { state } = this.editor;
      const { selection } = state;
      const attrs = {
         href: url,
         target: openInNewWindow ? "_blank" : "_self",
         title: title
      };

      if (!selection.empty) {
         if (this.isLinkNode()) {
            return this.editor.chain()
               .focus()
               .extendMarkRange('link')
               .command(({ tr, commands }) => {
                  tr.insertText(text, selection.from, selection.to);

                  return commands.setLink(attrs);
               })
               .run();
         }

         return this.editor.chain()
            .focus()
            .extendMarkRange('link')
            .setLink(attrs)
            .run();
      }

      return this.editor.chain()
         .focus()
         .insertContent([
            {
               type: 'text',
               marks: [{
                  type: 'link',
                  attrs
               }],
               text: text
            },
            { type: 'text', text: ' ' }
         ])
         .run();
   }

   public unlink(): boolean {
      return this.editor.chain().focus().unsetLink().run();
   }

   public insertLinkInImage(url: string, title: string, openInNewWindow: boolean): boolean {
      return this.editor.chain().focus().setLink({ href: url, target: openInNewWindow ? "_blank" : "_self", title: title }).run();
   }

   public insertImage(url: string, width: string, height: string, altText: string): boolean {
      const imageAttributes: ImageAttributes = {
         src: url,
         width: width,
         height: height,
         alt: altText,
      };

      return this.editor.chain().focus().setImage(imageAttributes).run();
   }

   public insertVideo(src: string, width: string, height: string): boolean {
      if (this.isValidYoutubeUrl(src)) {
         return this.editor.chain().focus().setYoutubeVideo({ src: src, width: Number(width), height: Number(height) }).run();
      }
      else if (this.isValidVimeoUrl(src)) {
         return this.editor.chain().focus().setVimeoVideo({ src: src, width: Number(width), height: Number(height) }).run();
      }
      else if (this.isValidMediaItemUrl(src)) {
         return this.editor.chain().focus().setVideo({ src: src, width: Number(width), height: Number(height) }).run();
      }

      return false;
   }

   public insertHorizontalRule(): boolean {
      return this.editor.chain().focus().setHorizontalRule().run();
   }

   public insertNonbreakingSpace(): boolean {
      return this.editor.chain().focus().insertContent('\u00A0').run();
   }

   public insertContent(content: string): boolean {
      return this.editor.chain().focus().insertContent(content).run();
   }

   public insertTable(rows: number, cols: number): boolean {
      return this.editor.chain().focus().insertTable({ rows: rows, cols: cols, withHeaderRow: true }).run();
   }

   public addRowBefore(): boolean {
      return this.editor.chain().focus().addRowBefore().run();
   }

   public addRowAfter(): boolean {
      return this.editor.chain().focus().addRowAfter().run();
   }

   public addColumnBefore(): boolean {
      return this.editor.chain().focus().addColumnBefore().run();
   }

   public addColumnAfter(): boolean {
      return this.editor.chain().focus().addColumnAfter().run();
   }

   public deleteRow(): boolean {
      return this.editor.chain().focus().deleteRow().run();
   }

   public deleteColumn(): boolean {
      return this.editor.chain().focus().deleteColumn().run();
   }

   public deleteTable(): boolean {
      return this.editor.chain().focus().deleteTable().run();
   }

   public mergeCells(): boolean {
      return this.editor.chain().focus().mergeCells().run();
   }

   public splitCell(): boolean {
      return this.editor.chain().focus().splitCell().run();
   }

   public toggleHeaderRow(): boolean {
      return this.editor.chain().focus().toggleHeaderRow().run();
   }

   public toggleHeaderColumn(): boolean {
      return this.editor.chain().focus().toggleHeaderColumn().run();
   }

   public toggleHeaderCell(): boolean {
      return this.editor.chain().focus().toggleHeaderCell().run();
   }

   public fixTable(): boolean {
      return this.editor.chain().focus().fixTables().run();
   }

   public toggleBold(): boolean {
      return this.editor.chain().focus().toggleBold().run();
   }

   public toggleItalic(): boolean {
      return this.editor.chain().focus().toggleItalic().run();
   }

   public toggleUnderline(): boolean {
      return this.editor.chain().focus().toggleUnderline().run();
   }

   public toggleStrike(): boolean {
      return this.editor.chain().focus().toggleStrike().run();
   }

   public toggleSuperscript(): boolean {
      return this.editor.chain().focus().toggleSuperscript().run();
   }

   public toggleSubscript(): boolean {
      return this.editor.chain().focus().toggleSubscript().run();
   }

   public toggleCode(): boolean {
      return this.editor.chain().focus().toggleCode().run();
   }

   public insertParagraph(): boolean {
      return this.editor.chain().focus().setParagraph().run();
   }

   public insertHeading(level: 1 | 2 | 3 | 4 | 5 | 6): boolean {
      return this.editor.chain().focus().unsetFontSize().setHeading({ level: level }).run();
   }

   public insertDiv(): boolean {
      return this.editor.chain().focus().insertDiv().run();
   }

   public updateCSSDiv(pos: number, cssClass: string, cssStyle: string): boolean {
      return this.editor.chain().focus().updateCSS(pos, { class: cssClass, style: cssStyle }).run();
   }

   public setFontFamily(fontFamily: string): boolean {
      return this.editor.chain().focus().setFontFamily(fontFamily).run();
   }

   public setFontSize(fontSize: string): boolean {
      return this.editor.chain().focus().setFontSize(fontSize).run();
   }

   public setTextAlign(textAlign: string): boolean {
      return this.editor.chain().focus().setTextAlign(textAlign).run();
   }

   public setImageAlign(align: string): boolean {
      switch (align) {
         case 'left':
            return this.editor.chain().focus().updateAttributes('image', { style: 'float: left' }).run();
         case 'center':
            return this.editor.chain().focus().updateAttributes('image', { style: 'display: block; margin-left: auto; margin-right: auto;' }).run();
         case 'right':
            return this.editor.chain().focus().updateAttributes('image', { style: 'float: right' }).run();
         case 'justify':
            return this.editor.chain().focus().setTextAlign('justify').run();
         default:
            return false;
      }
   }

   public setLineHeight(lineHeight: string): boolean {
      return this.editor.chain().focus().setLineHeight(String(lineHeight)).run();
   }

   public setTextColor(color: string): boolean {
      return this.editor.chain().focus().setColor(color).run();
   }

   public unsetTextColor(): boolean {
      return this.editor.chain().focus().unsetColor().run();
   }

   public setBackgroundColor(color: string): boolean {
      return this.editor.chain().focus().setHighlight({ color: color }).run();
   }

   public unsetBackgroundColor(): boolean {
      return this.editor.chain().focus().unsetHighlight().run();
   }

   public clearFormatting(): boolean {
      const { empty } = this.editor.view.state.selection;
      if (empty) {
         return this.editor.chain().focus()
            .unsetBold()
            .unsetItalic()
            .unsetUnderline()
            .unsetStrike()
            .unsetSubscript()
            .unsetSuperscript()
            .unsetCode()
            .setColor(DEFAULT_STYLES.TEXT_COLOR)
            .unsetHighlight()
            .run();
      }
      else {
         return this.editor.chain().focus().unsetAllMarks().run();
      }
   }

   public addIndent(): boolean {
      return this.editor.chain().focus().indent().run();
   }

   public removeIndent(): boolean {
      return this.editor.chain().focus().outdent().run();
   }

   public toggleBlockquote(): boolean {
      return this.editor.chain().focus().toggleBlockquote().run();
   }

   public toggleCodeBlock(): boolean {
      return this.editor.chain().focus().toggleCodeBlock().run();
   }

   public toggleBulletList(): boolean {
      return this.editor.chain().focus().toggleBulletList().run();
   }

   public toggleOrderedList(): boolean {
      return this.editor.chain().focus().toggleOrderedList().run();
   }

   public splitListItem(): boolean {
      return this.editor.chain().focus().splitListItem("listItem").run();
   }

   public sinkListItem(): boolean {
      return this.editor.chain().focus().sinkListItem("listItem").run();
   }

   public liftListItem(): boolean {
      return this.editor.chain().focus().liftListItem("listItem").run();
   }

   public getSelectedText(): string {
      const { from, to } = this.editor.view.state.selection;

      return this.editor.state.doc.textBetween(from, to, ' ');
   }

   public getTextOnCursor(): string {
      return this.editor.state.doc.nodeAt(this.editor.view.state.selection.from - 1)?.textContent ?? '';
   }

   public isImageNode(): boolean {
      return this.editor.isActive('image');
   }

   public isLinkNode(): boolean {
      return this.editor.isActive('link');
   }

   public getImageAttributes(): Record<string, string> {
      return this.editor.getAttributes('image');
   }

   public getVideoAttributes(): Record<string, string> {
      return this.editor.getAttributes('video');
   }

   public getLinkAttributes(): Record<string, string> {
      return this.editor.getAttributes('link');
   }

   public getDivAttributes(pos: number): Record<string, string> {
      const node = this.editor.view.state.doc.nodeAt(pos);
      return node?.attrs ?? {};
   }

   private isValidYoutubeUrl(url: string): boolean {
      return url.match(this.YOUTUBE_REGEX) !== null;
   }

   private isValidVimeoUrl(url: string): boolean {
      return url.match(this.VIMEO_REGEX) !== null;
   }

   private isValidMediaItemUrl(url: string): boolean {
      return url.match(/raytha/) !== null;
   }

   private fixContent(content: string): string {
      return content.replace(this.NESTED_IFRAME, '$2');
   }
}