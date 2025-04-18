import { Editor } from "@tiptap/core";
import { throttle } from 'lodash';
import { IEditorState } from "wysiwyg/model/interfaces/IEditorState";
import { IEditorStateManager } from "wysiwyg/model/interfaces/IEditorStateManager";
import { StateSubscriptionManager } from "wysiwyg/state/core/StateSubscriptionManager";
import { IStateSubscriptionManager } from "wysiwyg/state/interfaces/IStateSubscriptionManager";
import { Alignment } from "../types";
import { DEFAULT_STYLES } from "../constants/defaultStyles";

export class EditorStateManager implements IEditorStateManager {
   private readonly VALID_ALIGNMENTS: Alignment[] = ['left', 'center', 'right', 'justify'];
   private readonly THROTTLE_DELAY = 300;

   private currentState: IEditorState;
   private stateSubscriptionManager: IStateSubscriptionManager;
   private updateSelectionThrottle: ReturnType<typeof throttle>;
   private onContentChangedCallback?: (content: string) => void;

   constructor(private editor: Editor) {
      this.stateSubscriptionManager = StateSubscriptionManager.getInstance();
      this.setupFunctions();
      this.setupEditorListeners();
      this.updateEditorState();
   }

   public onContentChanged(callback: (content: string) => void): void {
      this.onContentChangedCallback = callback;
   }

   public forceUpdate(): void {
      this.updateEditorState();
   }

   public destroy(): void {
      if (this.onContentChangedCallback)
         this.onContentChangedCallback(this.editor.getHTML());

      this.updateSelectionThrottle.cancel();
      this.editor.off('update');
      this.editor.off('selectionUpdate');
      this.editor.off('blur');
   }

   private setupFunctions(): void {
      this.updateSelectionThrottle = throttle(() => this.updateEditorState(), this.THROTTLE_DELAY,
         {
            leading: true,
            trailing: true,
         },
      );
   }

   private setupEditorListeners(): void {
      this.editor.on('update', () => this.updateIfContentChanged());
      this.editor.on('selectionUpdate', () => this.updateSelectionThrottle());
      this.editor.on('blur', () => {
         if (this.onContentChangedCallback)
            this.onContentChangedCallback(this.editor.getHTML());
      });
   }

   private updateIfContentChanged(): void {
      const newContent = this.editor.getHTML();

      this.updateEditorState();

      if (this.onContentChangedCallback)
         this.onContentChangedCallback(newContent);
   }

   private updateEditorState(): void {
      this.currentState = {
         marks: this.getActiveMarks(),
         textStyle: this.getActiveTextStyle(),
         formats: this.getActiveFormat(),
         textAlign: this.getActiveTextAlign(),
         list: this.getActiveList(),
         blockquote: this.editor.isActive('blockquote'),
         codeBlock: this.editor.isActive('codeBlock'),
         invisibleCharacters: this.verifyInvisibleCharactersIsEnabled(),
         cursorPosition: this.getCursorPosition(),
         words: this.getWordsCount(),
         searchResult: this.getSearchResultTotal(),
         nodeType: {
            isImage: this.editor.isActive('image'),
            isLink: this.editor.isActive('link'),
            isVideo: this.editor.isActive('video'),
         },
         link: {
            href: this.editor.getAttributes('link').href,
         },
         image: {
            align: this.getImageAlignment(),
         },
         video: this.getVideoAttributes(),

      };

      this.stateSubscriptionManager.notifySubscribers(this.currentState);
   }

   private getActiveMarks(): IEditorState['marks'] {
      return {
         bold: this.editor.isActive('bold'),
         italic: this.editor.isActive('italic'),
         underline: this.editor.isActive('underline'),
         strike: this.editor.isActive('strike'),
         superscript: this.editor.isActive('superscript'),
         subscript: this.editor.isActive('subscript'),
         code: this.editor.isActive('code'),
      };
   }

   private getActiveTextStyle(): IEditorState['textStyle'] {
      const attrs = this.editor.getAttributes('textStyle');

      return {
         fontFamily: attrs.fontFamily ?? DEFAULT_STYLES.FONT_FAMILY,
         fontSize: attrs.fontSize ?? DEFAULT_STYLES.FONT_SIZE,
      };
   }

   private getActiveFormat(): IEditorState['formats'] {
      return {
         paragraph: this.editor.isActive('paragraph'),
         heading1: this.editor.isActive('heading', { level: 1 }),
         heading2: this.editor.isActive('heading', { level: 2 }),
         heading3: this.editor.isActive('heading', { level: 3 }),
         heading4: this.editor.isActive('heading', { level: 4 }),
         heading5: this.editor.isActive('heading', { level: 5 }),
         heading6: this.editor.isActive('heading', { level: 6 }),
         lineHeight: (this.editor.getAttributes('paragraph') || this.editor.getAttributes('heading')).lineHeight,
      };
   }

   private getActiveList(): IEditorState['list'] {
      return {
         ordered: this.editor.isActive('orderedList'),
         bullet: this.editor.isActive('bulletList'),
      };
   }

   private getActiveTextAlign(): IEditorState['textAlign'] {
      const currentAlignment = this.VALID_ALIGNMENTS.find((alignment) =>
         this.editor.isActive({ textAlign: alignment })
      );

      return currentAlignment || DEFAULT_STYLES.TEXT_ALIGN;
   }

   private verifyInvisibleCharactersIsEnabled(): IEditorState['invisibleCharacters'] {
      return this.editor.extensionManager.extensions.find(extension => extension.name === 'invisibleCharacters')?.options.enabled ?? false;
   }

   private getCursorPosition(): IEditorState['cursorPosition'] {
      const { $from } = this.editor.state.selection;
      const path: string[] = [];
      const marksInText = new Set();

      for (let i = $from.depth; i >= 0; i--) {
         const node = $from.node(i);
         if (node.type.spec.toDOM) {
            const domSpec = node.type.spec.toDOM(node);
            const tagName = Array.isArray(domSpec)
               ? domSpec[0]
               : domSpec;

            path.unshift(tagName);
         }

         const marks = $from.marks();
         for (const mark of marks) {
            if (!marksInText.has(mark.type.name)) {
               marksInText.add(mark.type.name);
               if (mark.type.spec.toDOM) {
                  const domSpec = mark.type.spec.toDOM(mark, true);
                  const tagName = Array.isArray(domSpec)
                     ? domSpec[0]
                     : domSpec;

                  path.push(tagName);
               } else {
                  path.push(mark.type.name);
               }
            }
         }
      }

      return path.join(" > ");
   }

   private getWordsCount(): IEditorState['words'] {
      return this.editor.storage.characterCount.words();
   }

   private getSearchResultTotal(): IEditorState['searchResult'] {
      const { results, resultIndex } = this.editor?.storage?.searchAndReplace ?? {};
      const total = results.length;

      if (total === 0) {
         return {
            total: 0,
            index: 0,
         };
      }

      if (resultIndex >= total) {
         return {
            total: total,
            index: 0,
         };
      }

      return {
         total: total,
         index: resultIndex === 0 ? 1 : resultIndex + 1,
      };
   }

   private getImageAlignment(): IEditorState['image']['align'] {
      const attrs = this.editor.getAttributes('image');

      if (attrs.style) {
         if (attrs.style.includes('float: left')) {
            return 'left';
         }
         else if (attrs.style.includes('float: right')) {
            return 'right';
         }
         else if (attrs.style.includes('margin-left: auto;') && attrs.style.includes('margin-right: auto;')) {
            return 'center';
         }
      }

      return 'left';
   }

   private getVideoAttributes(): IEditorState['video'] {
      const attrs = this.editor.getAttributes('video');

      return {
         src: attrs.src,
         width: attrs.width,
         height: attrs.height,
      };
   }
}