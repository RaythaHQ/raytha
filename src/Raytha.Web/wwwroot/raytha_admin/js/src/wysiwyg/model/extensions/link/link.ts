import Link from '@tiptap/extension-link';

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      linkWithTitle: {
         setLink: (attributes: { href: string; target?: string | null; rel?: string | null; class?: string | null, title?: string | null }) => ReturnType,
         toggleLink: (attributes: { href: string; target?: string | null; rel?: string | null; class?: string | null, title?: string | null }) => ReturnType,
         unsetLink: () => ReturnType,
      }
   }
}

export const ExtendedLink = Link.extend({
   addAttributes() {
      return {
         ...this.parent?.(),
         title: {
            default: null,
            parseHTML: element => element.getAttribute('title'),
            renderHTML: attributes => {
               if (!attributes.title) return;

               return {
                  title: attributes.title,
               };
            },
         },
      };
   },
});