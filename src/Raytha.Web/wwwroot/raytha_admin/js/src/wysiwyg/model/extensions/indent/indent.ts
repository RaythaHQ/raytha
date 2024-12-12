// Sources:
//https://github.com/ueberdosis/tiptap/issues/1036#issuecomment-981094752
//https://github.com/django-tiptap/django_tiptap/blob/main/django_tiptap/templates/forms/tiptap_textarea.html#L453-L602

import { Extension } from '@tiptap/core';
import { TextSelection, AllSelection, Transaction } from "@tiptap/pm/state";
import { Node } from "@tiptap/pm/model";

export interface IndentOptions {
   types: string[],
   indentLevels: number[],
   defaultIndentLevel: number,
}

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      indent: {
         indent: () => ReturnType,
         outdent: () => ReturnType,
      }
   }
}

export const Indent = Extension.create<IndentOptions>({
   name: 'indent',

   addOptions() {
      return {
         types: ['heading', 'paragraph'],
         indentLevels: [0, 30, 60, 90, 120, 150, 180, 210],
         defaultIndentLevel: 0,
      };
   },

   addGlobalAttributes() {
      return [
         {
            types: this.options.types,
            attributes: {
               indent: {
                  default: this.options.defaultIndentLevel,
                  renderHTML: attributes => {
                     if (!attributes.indent) return {};

                     return {
                        style: `padding-left: ${attributes.indent}px!important;`,
                     };
                  },
                  parseHTML: element => parseInt(element.style.paddingLeft) || this.options.defaultIndentLevel,
               },
            },
         },
      ];
   },

   addCommands() {
      return {
         indent: () => ({ tr, state, dispatch, editor }) => {
            const { selection } = state;
            tr = tr.setSelection(selection);
            tr = updateIndentLevel(tr, IndentProps.more);

            if (tr.docChanged) {
               dispatch && dispatch(tr);

               return true
            }

            editor.chain().focus().run();

            return false;
         },
         outdent: () => ({ tr, state, dispatch, editor }) => {
            const { selection } = state;
            tr = tr.setSelection(selection);
            tr = updateIndentLevel(tr, IndentProps.less);

            if (tr.docChanged) {
               dispatch && dispatch(tr);

               return true;
            }

            editor.chain().focus().run();

            return false;
         },
      };
   },

   addKeyboardShortcuts() {
      return {
         Tab: () => {
            if (!(this.editor.isActive('bulletList') || this.editor.isActive('orderedList'))) return this.editor.commands.indent();

            return false;
         },
         'Shift-Tab': () => {
            if (!(this.editor.isActive('bulletList') || this.editor.isActive('orderedList'))) return this.editor.commands.outdent();

            return false;
         },
         Backspace: () => {
            if (!(this.editor.isActive('bulletList') || this.editor.isActive('orderedList'))) return this.editor.commands.outdent();

            return false;
         },
      };
   },
})

const clamp = (val: number, min: number, max: number): number => Math.min(Math.max(val, min), max);

const IndentProps = {
   min: 0,
   max: 210,
   more: 30,
   less: -30,
};

function isBulletListNode(node: Node) {
   return node.type.name === 'bullet_list';
}

function isOrderedListNode(node: Node) {
   return node.type.name === 'order_list';
}

function isListNode(node: Node) {
   return isBulletListNode(node) || isOrderedListNode(node);
}

function setNodeIndentMarkup(tr: Transaction, pos: number, delta: number) {
   if (!tr.doc) return tr;

   const node = tr.doc.nodeAt(pos);
   if (!node) return tr;

   const minIndent = IndentProps.min;
   const maxIndent = IndentProps.max;

   const currentIndent = node.attrs.indent || 0;
   const proposedIndent = currentIndent + delta;

   const indent = clamp(proposedIndent, minIndent, maxIndent);

   if (indent === node.attrs.indent) return tr;

   const nodeAttrs = {
      ...node.attrs,
      indent,
   };

   return tr.setNodeMarkup(pos, node.type, nodeAttrs, node.marks);
}

const updateIndentLevel = (tr: Transaction, delta: number) => {
   const { doc, selection } = tr;

   if (!doc || !selection) return tr;

   if (!(selection instanceof TextSelection || selection instanceof AllSelection)) return tr;

   const { from, to } = selection;

   doc.nodesBetween(from, to, (node: Node, pos: number) => {
      const nodeType = node.type;

      if (nodeType.name === 'paragraph' || nodeType.name === 'heading') {
         tr = setNodeIndentMarkup(tr, pos, delta);

         return false;
      }

      if (isListNode(node)) {
         return false;
      }

      return true;
   });

   return tr;
}