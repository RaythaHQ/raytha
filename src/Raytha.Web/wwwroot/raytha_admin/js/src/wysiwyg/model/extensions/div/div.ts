import { mergeAttributes, Node } from "@tiptap/core";

export interface DivOptions {
   HTMLAttributes: Record<string, string>,
}

export type CssOptions = {
   class: string,
   style: string,
}

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      div: {
         insertDiv: () => ReturnType,
         updateCSS: (pos: number, options: CssOptions) => ReturnType,
      }
   }
}

export const Div = Node.create<DivOptions>({
   name: 'div',
   group: 'block',
   content: 'block+',
   defining: true,
   priority: 1000,

   addOptions() {
      return {
         HTMLAttributes: {},
      };
   },

   addAttributes() {
      return {
         class: {
            default: '',
         },
         style: {
            default: '',
         },
         'data-custom-div': {
            default: 'true',
            parseHTML: (element) => element.getAttribute('data-custom-div'),
            renderHTML: (attrs) => {
               if (!attrs['data-custom-div']) return {};

               return {
                  'data-custom-div': attrs['data-custom-div'],
               };
            }
         },
      };
   },

   parseHTML() {
      return [
         {
            tag: 'div[data-custom-div]',
            getAttrs: (element) => {
               if (!(element instanceof HTMLElement)) return false;

               return {
                  class: element.getAttribute('class') ?? '',
                  style: element.getAttribute('style') ?? '',
                  'data-custom-div': true,
               };
            },
         },
      ];
   },

   renderHTML({ HTMLAttributes }) {
      return ['div', mergeAttributes(HTMLAttributes), 0];
   },

   addCommands() {
      return {
         insertDiv: () => ({ chain }) => {
            return chain()
               .insertContent('<p></p>')
               .wrapIn(this.name, {
                  ...this.options.HTMLAttributes,
                  'data-custom-div': true,
               })
               .focus()
               .run();
         },
         updateCSS: (pos: number, options: CssOptions) => ({ tr, dispatch }) => {
            if (dispatch) {
               tr.setNodeMarkup(pos, this.type, {
                  class: options.class,
                  style: options.style,
               });

               dispatch(tr);

               return true;
            }

            return false;
         },
      };
   },

   addNodeView() {
      return ({ node, getPos }) => {
         const nodeView = document.createElement('div');
         nodeView.classList.add('div-node-view');

         const label = document.createElement('label');
         label.innerHTML = 'div';

         const settingsButton = document.createElement('button');
         settingsButton.innerHTML = '...';
         settingsButton.setAttribute('type', 'button');
         settingsButton.classList.add('btn', 'div-settings-button', 'shadow-none');
         settingsButton.setAttribute('data-bs-toggle', 'modal');
         settingsButton.setAttribute('data-bs-target', '#editDivModal');
         settingsButton.setAttribute('data-pos', String(getPos()));

         const content = document.createElement('div');
         content.classList.add('div-node-view-content');

         if (node.attrs.class)
            content.setAttribute('class', node.attrs.class);

         if (node.attrs.style)
            content.setAttribute('style', node.attrs.style);

         nodeView.append(label, settingsButton, content);

         return {
            dom: nodeView,
            contentDOM: content,
         };
      };
   },

   addKeyboardShortcuts() {
      return {
         Enter: ({ editor }) => {
            const { selection } = editor.state;
            const { $from } = selection;

            const isParagraph = $from.parent.type.name === 'paragraph';
            const isParentDiv = $from.node(-1)?.type.name === 'div';

            if (isParagraph && isParentDiv) {
               const startPosDiv = $from.before(-1);
               const sizeDiv = $from.node(-1).nodeSize;
               const posAfterDiv = startPosDiv + sizeDiv;

               return editor.chain().insertContentAt(posAfterDiv, '<p></p>').run();
            }

            return false;
         },
      };
   },
})