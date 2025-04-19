import { Extension } from '@tiptap/core';

import { RightClickMenuPlugin, RightClickMenuPluginProps } from './right-click-menu-plugin';

export type RightClickMenuOptions = Omit<RightClickMenuPluginProps, 'editor' | 'element'> & {
   element: HTMLElement | null,
}

export const RightClickMenu = Extension.create<RightClickMenuOptions>({
   name: 'rightClickMenu',

   addOptions() {
      return {
         element: null,
         tippyOptions: {},
         pluginKey: 'rightClickMenu',
      };
   },

   addProseMirrorPlugins() {
      if (!this.options.element) return [];

      return [
         RightClickMenuPlugin({
            pluginKey: this.options.pluginKey,
            editor: this.editor,
            element: this.options.element,
            tippyOptions: this.options.tippyOptions,
         }),
      ];
   },
})