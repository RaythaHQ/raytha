import { Node } from '@tiptap/core';

export interface IframeOptions {
   allowFullscreen: boolean,
   HTMLAttributes: {
      [key: string]: any,
   },
}

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      iframe: {
         setIframe: (options: { src: string }) => ReturnType,
      }
   }
}

export const Iframe = Node.create<IframeOptions>({
   name: 'iframe',
   group: 'block',
   selectable: true,
   draggable: true,
   atom: true,

   addOptions() {
      return {
         allowFullscreen: true,
         HTMLAttributes: {
            class: 'iframe-wrapper',
         },
      };
   },

   addAttributes() {
      return {
         src: {
            default: null,
         },
         frameborder: {
            default: 0,
         },
         allowfullscreen: {
            default: this.options.allowFullscreen,
            parseHTML: (element) => {
               return element.getAttribute('allowfullscreen') !== null;
            },
         },
         width: {
            default: '100%',
            parseHTML: (element) => {
               return element.getAttribute('width') || element.style.width;
            },
         },
         height: {
            default: '100%',
            parseHTML: (element) => {
               return element.getAttribute('height') || element.style.height;
            },
         },
         style: {
            default: null,
            parseHTML: (element) => element.getAttribute('style'),
         },
      };
   },

   parseHTML() {
      return [
         {
            tag: 'p iframe',
         },
         {
            tag: 'iframe',
            getAttrs: element => {
               const src = element.getAttribute('src');

               if (!src) return false;

               return { src };
            },
         },
      ];
   },

   renderHTML({ HTMLAttributes }) {
      return [
         'iframe',
         {
            ...HTMLAttributes,
            style: HTMLAttributes.style || `width: ${HTMLAttributes.width}; height: ${HTMLAttributes.height};`,
         },
      ];
   },

   addCommands() {
      return {
         setIframe: (options: { src: string }) => ({ tr, dispatch }) => {
            if (dispatch) {
               const { selection } = tr;
               const node = this.type.create(options);

               tr.replaceRangeWith(selection.from, selection.to, node);
            }

            return true;
         },
      };
   },
})