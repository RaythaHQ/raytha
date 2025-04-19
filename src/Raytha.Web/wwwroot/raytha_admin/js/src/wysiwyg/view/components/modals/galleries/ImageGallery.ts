import { Gallery, IMediaItem } from '.';

export class ImageGallery extends Gallery {
   protected getMediaItemTemplate(mediaItem: IMediaItem): string {
      return `
        <div class="col">
          <div class="ratio ratio-4x3">
            <img class="object-fit-cover img-thumbnail"
                loading="lazy"
                data-action="click->${this.controllerIdentifier}#insertImageFromFileStorage"
                data-${this.controllerIdentifier}-src-param="${mediaItem.url}"
                src="${mediaItem.url}"
                title="${mediaItem.fileName}"
                alt="${mediaItem.fileName}">
           </div>
        </div>
        `;
   }

   protected renderMediaItem(mediaItem: IMediaItem): HTMLElement {
      return this.createElementFromTemplate(this.getMediaItemTemplate(mediaItem));
   }
}