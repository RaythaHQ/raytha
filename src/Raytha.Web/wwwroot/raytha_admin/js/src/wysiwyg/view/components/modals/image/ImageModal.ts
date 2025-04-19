import { Tab } from 'bootstrap';
import { ModalBaseComponent } from '../base';
import { Gallery, ImageGallery } from '../galleries';
import { IImageModal, IImageModalFormData } from './interfaces';
import { IMAGE_MODAL_SELECTORS } from './constants/imageModalSelectors';
import { FORM_FIELDS } from './constants/imageModalFormFields';
import template from './templates/imageModal.html';

export class ImageModal extends ModalBaseComponent implements IImageModal {
   private readonly IMAGE_WIDTH = '640';
   private readonly IMAGE_HEIGHT = '480';

   private generalTab: Tab;
   private gallery: Gallery;
   private imageUrlInput: HTMLInputElement;
   private imageWidthInput: HTMLInputElement;
   private imageHeightInput: HTMLInputElement;
   private imageAltTextInput: HTMLInputElement;

   private onImageUrlChange(event: Event): void {
      const imageUrl = (event.target as HTMLInputElement).value;

      const image = new Image();
      image.src = imageUrl;

      image.onload = () => {
         if (image.width > 0) {
            this.imageWidthInput.value = String(image.width);
         }
         if (image.height > 0) {
            this.imageHeightInput.value = String(image.height);
         }
      }
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const galleryTabPanel = this.querySelector(IMAGE_MODAL_SELECTORS.GALLERY_TAB);
      this.gallery = new ImageGallery(galleryTabPanel, this.controllerIdentifier);

      const generalTabElement = this.querySelector(IMAGE_MODAL_SELECTORS.GENERAL_TAB);
      this.generalTab = Tab.getOrCreateInstance(generalTabElement);

      this.imageUrlInput = this.querySelector<HTMLInputElement>(IMAGE_MODAL_SELECTORS.URL_INPUT);
      this.imageWidthInput = this.querySelector<HTMLInputElement>(IMAGE_MODAL_SELECTORS.WIDTH_INPUT);
      this.imageHeightInput = this.querySelector<HTMLInputElement>(IMAGE_MODAL_SELECTORS.HEIGHT_INPUT);
      this.imageAltTextInput = this.querySelector<HTMLInputElement>(IMAGE_MODAL_SELECTORS.ALT_TEXT_INPUT);

      this.bindEvent(this.imageUrlInput, 'change', this.onImageUrlChange.bind(this));
   }

   public showGeneralTab(): void {
      this.generalTab.show();
   }

   public clearInputs(): void {
      this.imageUrlInput.value = '';
      this.imageWidthInput.value = this.IMAGE_WIDTH;
      this.imageHeightInput.value = this.IMAGE_HEIGHT;
      this.imageAltTextInput.value = '';
   }

   public setInputValues(imageUrl: string, imageWidth?: string, imageHeight?: string, imageAltText?: string): void {
      this.imageUrlInput.value = imageUrl ?? '';
      this.imageWidthInput.value = imageWidth ?? this.IMAGE_WIDTH;
      this.imageHeightInput.value = imageHeight ?? this.IMAGE_HEIGHT;
      this.imageAltTextInput.value = imageAltText ?? '';
   }

   public getGallery(): Gallery {
      return this.gallery;
   }

   public getUploadContainer(): HTMLElement {
      return this.querySelector('#imageUploadContainer');
   }

   public getFormValues(element: HTMLFormElement): IImageModalFormData {
      const formData = new FormData(element);

      return {
         src: formData.get(FORM_FIELDS.SRC) as string,
         width: formData.get(FORM_FIELDS.WIDTH) as string,
         height: formData.get(FORM_FIELDS.HEIGHT) as string,
         altText: formData.get(FORM_FIELDS.ALT_TEXT) as string,
      };
   }

   public destroy(): void {
      this.gallery.destroy();
      this.generalTab.dispose();
      super.destroy();
   }
}