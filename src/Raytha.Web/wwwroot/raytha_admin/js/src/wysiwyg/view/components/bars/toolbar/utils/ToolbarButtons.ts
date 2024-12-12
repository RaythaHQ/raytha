import { IToolbarState } from '../interfaces/IToolbarState';

export class ToolbarButtons {
   private readonly buttonSelectors = {
      bold: '#boldButton',
      italic: '#italicButton',
      underline: '#underlineButton',
      strikethrough: '#strikeButton',
      superscript: '#superscriptButton',
      subscript: '#subscriptButton',
      code: '#codeButton',
      bulletList: '#bulletListButton',
      orderedList: '#orderedListButton',
      blockquote: '#blockquoteButton',
      codeBlock: '#codeBlockButton',
      invisibleCharacters: '[data-command="invisibleCharacters"]',
      image: '#imageButton',
      link: '#linkButton',
      video: '#videoButton',
   } as const;

   private buttons: Map<keyof typeof this.buttonSelectors, HTMLElement> = new Map();

   constructor(container: HTMLElement) {
      this.initializeButtons(container);
   }

   public destroy(): void {
      this.buttons.clear();
   }

   private initializeButtons(container: HTMLElement): void {
      for (const [key, selector] of Object.entries(this.buttonSelectors)) {
         const button: HTMLElement | null = container.querySelector(selector);
         if (button) {
            const typedKey = key as keyof typeof this.buttonSelectors;
            this.buttons.set(typedKey, button);
         }
         else {
            throw new Error(`Button ${key} not found`);
         }
      }
   }

   public updateStates(state: IToolbarState): void {
      this.buttons.forEach((button, key) => {
         const isActive = this.getButtonState(key, state);
         button.classList.toggle('active', isActive);
      });
   }

   private getButtonState(key: keyof typeof this.buttonSelectors, state: IToolbarState): boolean {
      switch (key) {
         case 'bold': return state.marks.bold;
         case 'italic': return state.marks.italic;
         case 'underline': return state.marks.underline;
         case 'strikethrough': return state.marks.strike;
         case 'superscript': return state.marks.superscript;
         case 'subscript': return state.marks.subscript;
         case 'code': return state.marks.code;
         case 'bulletList': return state.list.bullet;
         case 'orderedList': return state.list.ordered;
         case 'blockquote': return state.blockquote;
         case 'codeBlock': return state.codeBlock;
         case 'invisibleCharacters': return state.invisibleCharacters;
         case 'image': return state.nodeType.isImage;
         case 'link': return state.nodeType.isLink;
         case 'video': return state.nodeType.isVideo;
         default: return false;
      }
   }
}