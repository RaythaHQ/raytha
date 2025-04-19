import TextStyle from '@tiptap/extension-text-style';

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      fontSize: {
         setFontSize: (fontSize: string) => ReturnType,
         unsetFontSize: () => ReturnType,
      }
   }
}

export const FontSize = TextStyle.extend({
   addAttributes() {
      return {
         ...this.parent?.(),
         fontSize: {
            default: null,
            parseHTML: (element: HTMLElement) => element.style.fontSize?.replace(/['"]+/g, ''),
            renderHTML: attributes => {
               if (!attributes.fontSize) return;

               return {
                  style: `font-size: ${attributes.fontSize}`,
               };
            },
         },
      };
   },

   addCommands() {
      return {
         setFontSize: fontSize => ({ chain }) => {
            return chain().setMark('textStyle', { fontSize }).run();
         },
         unsetFontSize: () => ({ chain }) => {
            return chain().setMark('textStyle', { fontSize: null }).run();
         },
      };
   },
})