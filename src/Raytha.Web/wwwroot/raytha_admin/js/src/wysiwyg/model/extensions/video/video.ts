import { mergeAttributes, Node } from '@tiptap/core';
import { Plugin } from '@tiptap/pm/state';
import { merge } from 'lodash';
import { SwalAlert } from 'wysiwyg/view/SwalAlert';

export interface VideoOptions {
   fileUploadFn: (file: File) => Promise<string>,
   HTMLAttributes: Record<string, string>,
}

export type SetVideoOptions = {
   src: string,
   width: number,
   height: number,
   controls?: boolean,
   autoplay?: boolean,
   loop?: boolean,
}

// @ts-ignore
declare module '@tiptap/core' {
   interface Commands<ReturnType> {
      video: {
         setVideo: (options: SetVideoOptions) => ReturnType,
      }
   }
}

export const Video = Node.create<VideoOptions>({
   name: 'video',
   group: 'block',
   inline: false,
   draggable: true,
   selectable: true,

   addOptions() {
      return {
         fileUploadFn: null!,
         HTMLAttributes: {},
      };
   },

   addAttributes() {
      return {
         src: {
            default: null
         },
         width: {
            default: 640,
         },
         height: {
            default: 480,
         },
         controls: {
            default: false,
         },
         style: {
            default: 'max-width: 100%; height: auto; object-fit: contain;',
         },
      };
   },


   renderHTML({ HTMLAttributes }) {
      const divAttributes = {
         class: 'video-wrapper',
         contenteditable: 'false',
      };

      const videoAttributes = mergeAttributes(
         this.options.HTMLAttributes,
         HTMLAttributes,
         {
            draggable: 'true',
         }
      );

      return [
         'div',
         divAttributes,
         [
            'video',
            videoAttributes,
         ],
      ];
   },

   parseHTML() {
      return [
         {
            tag: 'div.video-wrapper',
            priority: 51,
            getAttrs: (node) => {
               if (!(node instanceof HTMLElement)) return false;

               const video = node.querySelector('video');
               if (!video) return false;

               return {
                  src: video.getAttribute('src'),
                  width: video.getAttribute('width'),
                  height: video.getAttribute('height'),
                  controls: video.hasAttribute('controls'),
                  style: video.getAttribute('style'),
               };
            },
         },
         {
            tag: 'video',
            getAttrs: (node) => {
               if (!(node instanceof HTMLElement)) return false;

               return {
                  src: node.getAttribute('src'),
                  width: node.getAttribute('width'),
                  height: node.getAttribute('height'),
                  controls: node.hasAttribute('controls'),
                  style: node.getAttribute('style'),
               };
            },
         },
      ];
   },

   addCommands() {
      return {
         setVideo: (options: SetVideoOptions) => ({ commands }) => commands.insertContent({
            type: this.name,
            attrs: {
               ...options,
               style: 'max-width: 100%; height: auto; object-fit: contain;',
            },
         }),
      };
   },

   addProseMirrorPlugins() {
      return [
         new Plugin({
            props: {
               handleDOMEvents: {
                  paste: (view, event) => {
                     const hasFiles = event.clipboardData && event.clipboardData.files && event.clipboardData.files.length;
                     if (!hasFiles) return;

                     const videos = Array.from(event.clipboardData.files).filter(file => /video/i.test(file.type));
                     if (videos.length === 0) return;

                     if (!this.options.fileUploadFn) return;

                     event.preventDefault();
                     const { schema } = view.state;
                     videos.forEach(async video => {
                        try {
                           const url = await this.options.fileUploadFn(video);
                           const node = schema.nodes.video.create({ src: url });
                           const transaction = view.state.tr.insert(view.state.selection.from, node);
                           view.dispatch(transaction);
                        }
                        catch (error) {
                           SwalAlert.showErrorAlert(`Upload failed: ${(error as Error).message}`);
                        }
                     });
                  },
               },
            },
         }),
      ];
   },
})