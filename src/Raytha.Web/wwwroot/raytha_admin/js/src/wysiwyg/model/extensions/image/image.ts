import { Image, ImageOptions } from "@tiptap/extension-image";
import { Plugin } from '@tiptap/pm/state';
import { SwalAlert } from "wysiwyg/view/SwalAlert";

export interface ExtendedImageOptions extends ImageOptions {
   fileUploadFn: (file: File) => Promise<string>,
}

export type ImageAttributes = {
   src: string,
   alt?: string,
   title?: string,
   width?: string | undefined,
   height?: string | undefined,
   style?: string | undefined,
};

export const ExtendedImage = Image.extend<ExtendedImageOptions>({
   addAttributes() {
      return {
         ...this.parent?.(),
         width: {
            default: null,
         },
         height: {
            default: null,
         },
         style: {
            default: undefined,
            parseHTML: (element) => {
               return element.style.cssText;
            },
         },
      };
   },

   addProseMirrorPlugins() {
      return [
         new Plugin({
            props: {
               handleDOMEvents: {
                  paste: (view, event) => {
                     const html = event.clipboardData && event.clipboardData?.getData('text/html');

                     if (html && html.includes('<img') && html.includes('src')) return;

                     const hasFiles = event.clipboardData && event.clipboardData.files && event.clipboardData.files.length;
                     if (!hasFiles) return;

                     const images = Array.from(event.clipboardData.files).filter(file => /image/i.test(file.type));
                     if (images.length === 0) return;

                     if (!this.options.fileUploadFn) return;

                     event.preventDefault();
                     const { schema } = view.state;
                     images.forEach(async image => {
                        try {
                           const url = await this.options.fileUploadFn(image);
                           const node = schema.nodes.image.create({ src: url });
                           const transaction = view.state.tr.replaceSelectionWith(node);

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