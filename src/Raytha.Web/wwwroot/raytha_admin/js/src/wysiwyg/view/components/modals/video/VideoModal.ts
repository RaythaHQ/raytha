import { Tab } from 'bootstrap';
import { ModalBaseComponent } from '../base/ModalBaseComponent';
import { VideoGallery } from '../galleries';
import { IVideoModal, IVideoModalFormData } from './interfaces';
import { VIDEO_MODAL_SELECTORS } from './constants/videoModalSelectors';
import { FORM_FIELDS } from './constants/videoModalFormFields';
import template from './templates/videoModal.html';

export class VideoModal extends ModalBaseComponent implements IVideoModal {
   private readonly DEFAULT_VIDEO_WIDTH = '640';
   private readonly DEFAULT_VIDEO_HEIGHT = '480';

   private generalTab: Tab;
   private gallery: VideoGallery;
   private videoUrlInput: HTMLInputElement;
   private videoWidthInput: HTMLInputElement;
   private videoHeightInput: HTMLInputElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const galleryTabPanel = this.querySelector(VIDEO_MODAL_SELECTORS.GALLERY_TAB_PANEL);
      if (galleryTabPanel)
         this.gallery = new VideoGallery(galleryTabPanel, this.controllerIdentifier);

      const generalTabElement = this.querySelector(VIDEO_MODAL_SELECTORS.GENERAL_TAB_BUTTON);
      this.generalTab = Tab.getOrCreateInstance(generalTabElement);

      this.videoUrlInput = this.querySelector<HTMLInputElement>(VIDEO_MODAL_SELECTORS.VIDEO_URL_INPUT);
      this.videoWidthInput = this.querySelector<HTMLInputElement>(VIDEO_MODAL_SELECTORS.VIDEO_WIDTH_INPUT);
      this.videoHeightInput = this.querySelector<HTMLInputElement>(VIDEO_MODAL_SELECTORS.VIDEO_HEIGHT_INPUT);
   }

   public getUploadContainer(): HTMLElement {
      return this.querySelector(VIDEO_MODAL_SELECTORS.VIDEO_UPLOAD_CONTAINER);
   }

   public getGallery(): VideoGallery {
      return this.gallery;
   }

   public showGeneralTab(): void {
      this.generalTab.show();
   }

   public clearInputs(): void {
      this.videoUrlInput.value = '';
      this.videoWidthInput.value = '';
      this.videoHeightInput.value = '';
   }

   public setVideoUrl(url: string): void {
      this.videoUrlInput.value = url;
   }

   public setInputValues(url: string, width: string, height: string): void {
      this.videoUrlInput.value = url ?? '';
      this.videoWidthInput.value = width ?? this.DEFAULT_VIDEO_WIDTH;
      this.videoHeightInput.value = height ?? this.DEFAULT_VIDEO_HEIGHT;
   }

   public getFormValues(element: HTMLFormElement): IVideoModalFormData {
      const formData = new FormData(element);

      return {
         src: formData.get(FORM_FIELDS.SRC) as string,
         width: formData.get(FORM_FIELDS.WIDTH) as string,
         height: formData.get(FORM_FIELDS.HEIGHT) as string,
      };
   }

   public destroy(): void {
      this.gallery.destroy();
      this.generalTab.dispose();
      super.destroy();
   }
}