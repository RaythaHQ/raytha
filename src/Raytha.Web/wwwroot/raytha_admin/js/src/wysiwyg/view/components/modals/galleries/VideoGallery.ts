import { IMediaItem, Gallery } from '.';

export class VideoGallery extends Gallery {
   protected renderMediaItem(mediaItem: IMediaItem): HTMLElement {
      const element = this.createElementFromTemplate(this.getMediaItemTemplate(mediaItem));
      const img = element.querySelector('img');
      if (!img) {
         console.error('Image element not found in the template');

         return element;
      }

      const video = document.createElement('video');

      video.preload = 'metadata';
      video.crossOrigin = "anonymous";
      video.addEventListener('loadedmetadata', () => {
         video.currentTime = 1;
      }, { once: true });

      video.addEventListener('seeked', () => {
         try {
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth || 320;
            canvas.height = video.videoHeight || 240;

            const ctx = canvas.getContext('2d');
            if (!ctx)
               throw new Error('Failed to get canvas context');

            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

            const dataUrl = canvas.toDataURL('image/png');
            img.src = dataUrl;

            video.remove();
            canvas.remove();
         } catch (error) {
            console.error('Error in loadeddata:', error);
         }
      }, { once: true });

      video.src = mediaItem.url;
      video.load();

      return element;
   }

   protected getMediaItemTemplate(mediaItem: IMediaItem): string {
      return `
            <div class="col">
                <img class="object-fit-cover img-thumbnail"
                    data-action="click->${this.controllerIdentifier}#insertVideoFromFileStorage"
                    data-${this.controllerIdentifier}-src-param="${mediaItem.url}">
            </div>`;
   }
}