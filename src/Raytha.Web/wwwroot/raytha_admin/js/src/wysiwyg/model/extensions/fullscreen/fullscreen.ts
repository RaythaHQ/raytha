import { Extension } from '@tiptap/core';
import { SwalAlert } from 'wysiwyg/view/SwalAlert';

export interface FullscreenOptions {
   editorContainerClass: string,
}

export interface FullscreenStorage {
   isFullscreen: boolean,
}

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      fullscreen: {
         toggleFullscreen: () => ReturnType,
      }
   }
}

export const Fullscreen = Extension.create<FullscreenOptions, FullscreenStorage>({
   name: 'fullscreen',

   addStorage() {
      return {
         isFullscreen: false,
      };
   },

   onBeforeCreate() {
      const container = document.querySelector(this.options.editorContainerClass);

      if (!container) return;

      container.addEventListener('fullscreenchange', () => {
         this.storage.isFullscreen = !this.storage.isFullscreen;
      });
   },

   onDestroy() {
      const container = document.querySelector(this.options.editorContainerClass);

      if (!container) return;

      container.removeEventListener('fullscreenchange', () => {
         this.storage.isFullscreen = !this.storage.isFullscreen;
      });
   },

   addCommands() {
      return {
         toggleFullscreen: () => () => {
            const container: HTMLElement | null = document.querySelector(this.options.editorContainerClass);

            if (!container) {
               SwalAlert.showErrorAlert(`Editor container not found: ${this.options.editorContainerClass}`);

               return false;
            };

            if (!this.storage.isFullscreen) {
               container.requestFullscreen().catch((error) => {
                  SwalAlert.showErrorAlert(`Failed to enter fullscreen mode: ${error.message}`);
               });

               return true;
            }
            else if (document.fullscreenElement) {
               document.exitFullscreen();
            }

            return true;
         },
      };
   },
})

