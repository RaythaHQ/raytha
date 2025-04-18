import { Extension } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';
import { Decoration, DecorationSet } from '@tiptap/pm/view';
import { Node } from '@tiptap/pm/model';

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      invisibleCharacters: {
         toggleInvisibleCharacters: () => ReturnType,
      }
   }
}

export const InvisibleCharacters = Extension.create({
   name: 'invisibleCharacters',

   addOptions() {
      return {
         enabled: false,
      };
   },

   addCommands() {
      return {
         toggleInvisibleCharacters: () => ({ editor }) => {
            const enabled = this.options.enabled;
            this.options.enabled = !enabled;
            editor.view.dispatch(editor.state.tr);

            return true;
         },
      };
   },

   addProseMirrorPlugins() {
      return [
         new Plugin({
            key: new PluginKey('invisibleCharacters'),
            props: {
               decorations: (state) => {
                  if (!this.options.enabled) return;

                  const doc: Node = state.doc;
                  const decorations: Decoration[] = [];

                  doc.descendants((node, pos) => {
                     if (node.type.name === 'paragraph' || node.type.name.startsWith('heading')) {
                        const symbol = node.type.name.startsWith('heading')
                           ? 'invisible-heading'
                           : 'invisible-paragraph';

                        decorations.push(
                           Decoration.widget(pos + node.nodeSize - 1, () => {
                              const span = document.createElement('span');
                              span.className = `invisible-character ${symbol}`;

                              return span;
                           }, { side: 1 }));
                     }

                     if (node.type.name === 'text') {
                        const text = node.text || '';
                        let offset = 0;

                        for (let i = 0; i < text.length; i++) {
                           const char = text[i];
                           if (char === ' ' || char === '\u00A0') {
                              decorations.push(
                                 Decoration.widget(pos + offset, () => {
                                    const span = document.createElement('span');
                                    span.className = 'invisible-character invisible-space';

                                    return span;
                                 }, { side: 1 })
                              );
                           }
                           offset += 1;
                        }
                     }

                     if (node.type.name === 'hardBreak') {
                        decorations.push(
                           Decoration.widget(pos, () => {
                              const span = document.createElement('span');
                              span.className = 'invisible-character invisible-newline';

                              return span;
                           }, { side: 1 })
                        );
                     }
                  });

                  return DecorationSet.create(doc, decorations);
               },
            },
         }),
      ];
   },
});
