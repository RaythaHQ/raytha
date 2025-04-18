import { Editor } from '@tiptap/core';
import { IEditorModel } from 'wysiwyg/model/interfaces/IEditorModel';
import { IEditorCommands } from 'wysiwyg/model/interfaces/IEditorCommands';
import { IEditorStateManager } from 'wysiwyg/model/interfaces/IEditorStateManager';
import { EditorCommands } from "wysiwyg/model/core/EditorCommands"
import { EditorStateManager } from "wysiwyg/model/core/EditorStateManager"
import { Extensions } from '../extensions/Extensions';

export class EditorModel implements IEditorModel {
   private editor: Editor;
   private commands: IEditorCommands;
   private stateManager: IEditorStateManager;
   private fileUploadFn: (file: File) => Promise<string>;
   private tableBubbleMenu: HTMLElement;
   private rightClickMenu: HTMLElement;
   private listBubbleMenu: HTMLElement;

   public addFileUploadFn(fileUploadFn: (file: File) => Promise<string>): IEditorModel {
      this.fileUploadFn = fileUploadFn;

      return this;
   }

   public addTableBubbleMenuElement(menu: HTMLElement): IEditorModel {
      this.tableBubbleMenu = menu;

      return this;
   }

   public addRightClickMenuElement(menu: HTMLElement): IEditorModel {
      this.rightClickMenu = menu;

      return this;
   }

   public addListBubbleMenuElement(menu: HTMLElement): IEditorModel {
      this.listBubbleMenu = menu;

      return this;
   }

   public initializeEditor(): IEditorModel {
      if (!this.rightClickMenu)
         throw new Error('Right click menu is required');

      if (!this.tableBubbleMenu)
         throw new Error('Table bubble menu is required');

      if (!this.listBubbleMenu)
         throw new Error('List bubble menu is required');

      if (!this.fileUploadFn)
         throw new Error('File upload callback is required');

      this.editor = new Editor({
         extensions: [
            Extensions.Bold,
            Extensions.Italic,
            Extensions.Code,
            Extensions.Strike,
            Extensions.Subscript,
            Extensions.Superscript,
            Extensions.ExtendedHighlight.configure({
               multicolor: true,
            }),
            Extensions.ExtendedLink.configure({
               HTMLAttributes: {
                  class: "link-primary",
               },
               openOnClick: false,
            }),
            Extensions.Underline,
            Extensions.Blockquote,
            Extensions.BulletList,
            Extensions.CodeBlock,
            Extensions.Document,
            Extensions.Text,
            Extensions.HardBreak,
            Extensions.Heading,
            Extensions.HorizontalRule,
            Extensions.ListItem,
            Extensions.OrderedList,
            Extensions.Paragraph,
            Extensions.Table.configure({
               resizable: true,
            }),
            Extensions.TableCell,
            Extensions.TableHeader,
            Extensions.TableRow,
            Extensions.Youtube,
            Extensions.ExtendedImage.configure({
               inline: true,
               fileUploadFn: this.fileUploadFn,
            }),
            Extensions.ExtendedCharacterCount,
            Extensions.TextStyle,
            Extensions.Color,
            Extensions.TextAlign.configure({
               types: ["heading", "paragraph", "video"],
            }),
            Extensions.Typography,
            Extensions.History,
            Extensions.FontFamily,
            Extensions.RightClickMenu.configure({
               element: this.rightClickMenu,
            }),
            Extensions.BubbleMenu.configure({
               pluginKey: 'tableBubbleMenu',
               element: this.tableBubbleMenu,
               shouldShow: ({ editor }) => {
                  return editor.isActive('table') || editor.isActive('tableCell') || editor.isActive('tableHeader') || editor.isActive('tableRow');
               },
               tippyOptions: {
                  placement: 'bottom',
                  interactive: true,
               },
            }),
            Extensions.BubbleMenu.configure({
               pluginKey: 'listBubbleMenu',
               element: this.listBubbleMenu,
               shouldShow: ({ editor }) => {
                  return editor.isActive('list') || editor.isActive('listItem');
               },
               tippyOptions: {
                  placement: 'bottom',
               }
            }),
            Extensions.FontSize,
            Extensions.CutCopyPaste,
            Extensions.SearchAndReplace,
            Extensions.LineHeight,
            Extensions.Indent,
            Extensions.InvisibleCharacters,
            Extensions.Gapcursor,
            Extensions.Video.configure({
               fileUploadFn: this.fileUploadFn
            }),
            Extensions.Vimeo,
            Extensions.Div,
            Extensions.Fullscreen.configure({
               editorContainerClass: '.editor-container',
            }),
            Extensions.Iframe,
         ],
         editorProps: {
            attributes: {
               class: 'p-1',
            },
         },
         parseOptions: {
            preserveWhitespace: 'full',
         },
      });

      this.commands = new EditorCommands(this.editor);
      this.stateManager = new EditorStateManager(this.editor);

      return this;
   }

   public getEditorElement(): HTMLElement {
      return this.editor.options.element as HTMLElement;
   }

   public getCommands(): IEditorCommands {
      return this.commands;
   }

   public getStateManager(): IEditorStateManager {
      return this.stateManager;
   }

   public destroy(): void {
      if (this.editor) {
         this.stateManager.destroy();
         this.editor.destroy();
      }
   }
}