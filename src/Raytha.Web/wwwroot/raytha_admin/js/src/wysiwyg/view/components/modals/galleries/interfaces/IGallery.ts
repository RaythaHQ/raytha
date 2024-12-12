import { IMediaItem } from './IMediaItem';

export interface IGallery {
   setMediaItems(mediaItems: IMediaItem[]): void,
   addMediaItem(mediaItem: IMediaItem): void,
}