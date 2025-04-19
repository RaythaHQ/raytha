import { Editor, posToDOMRect, } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';
import { EditorView } from '@tiptap/pm/view';
// @ts-ignore
import tippy, { Instance, Props } from 'tippy.js';

export interface RightClickMenuPluginProps {
   /**
    * The plugin key.
    * @type {PluginKey | string}
    */
   pluginKey: PluginKey | string,

   /**
    * The editor instance.
    */
   editor: Editor,

   /**
    * The DOM element that contains your menu.
    * @type {HTMLElement}
    * @default null
    */
   element: HTMLElement,

   /**
    * The options for the tippy.js instance.
    * @see https://atomiks.github.io/tippyjs/v6/all-props/
    */
   tippyOptions?: Partial<Props>,
}

export type RightClickMenuViewProps = RightClickMenuPluginProps & {
   view: EditorView,
}

export class RightClickMenuView {
   public editor: Editor;
   public element: HTMLElement;
   public view: EditorView;
   public tippy: Instance | undefined;
   public tippyOptions?: Partial<Props>;

   constructor({
      editor,
      element,
      view,
      tippyOptions = {},
   }: RightClickMenuViewProps) {
      this.editor = editor;
      this.element = element;
      this.view = view;

      this.view.dom.addEventListener('contextmenu', this.handleContextMenu);
      document.addEventListener('click', this.handleClick);
      this.tippyOptions = tippyOptions;
      // Detaches menu content from its current parent
      this.element.remove();
      this.element.style.visibility = 'visible';
   }

   private handleContextMenu = (event: MouseEvent) => {
      event.preventDefault();
      this.createTooltip();
      this.tippy?.setProps({
         getReferenceClientRect: this.tippyOptions?.getReferenceClientRect || (() => {
            const { from, to } = this.view.state.selection;

            return posToDOMRect(this.view, from, to);
         }),
      })

      this.tippy?.show();
   }

   private handleClick = () => {
      this.tippy?.hide();
   }

   private createTooltip() {
      const { element: editorElement } = this.editor.options;
      const editorIsAttached = !!editorElement.parentElement;

      if (this.tippy || !editorIsAttached) return;

      this.tippy = tippy(editorElement, {
         duration: 0,
         getReferenceClientRect: null,
         content: this.element,
         interactive: true,
         trigger: 'manual',
         placement: 'auto',
         hideOnClick: true,
         ...this.tippyOptions,
      });
   }

   show() {
      this.tippy?.show();
   }

   hide() {
      this.tippy?.hide();
   }

   destroy() {
      this.tippy?.destroy();
   }
}

export const RightClickMenuPlugin = (options: RightClickMenuPluginProps) => {
   return new Plugin({
      key: typeof options.pluginKey === 'string' ? new PluginKey(options.pluginKey) : options.pluginKey,
      view: view => new RightClickMenuView({ view, ...options }),
   });
}