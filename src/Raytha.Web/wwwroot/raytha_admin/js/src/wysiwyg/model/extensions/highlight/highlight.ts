import Highlight from "@tiptap/extension-highlight";

export const ExtendedHighlight = Highlight.extend({
   parseHTML() {
      return [
         {
            tag: 'mark',
         },
         {
            style: 'background-color',
            getAttrs: value => {
               return {
                  color: value,
               }
            },
         },
         {
            tag: 'span[style*=background-color]',
            getAttrs: element => {
               return {
                  color: element.style.backgroundColor,
               };
            },
         },
      ];
   },
})