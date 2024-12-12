import { debounce } from 'lodash';
import { ViewComponent } from '../../../base';
import { IMediaItem } from '..';
import galleryTemplate from '../templates/gallery.html';

export abstract class Gallery extends ViewComponent {
   private readonly debounceSearch: () => void;

   protected mediaGallery: HTMLDivElement;
   protected pagination: HTMLUListElement;
   protected previousButton: HTMLButtonElement;
   protected nextButton: HTMLButtonElement;
   protected pageButtons: HTMLButtonElement[] = [];

   protected readonly itemsPerPage: number = 6;
   protected currentPage: number = 1;
   protected mediaItems: IMediaItem[] = [];
   protected filteredMediaItems: IMediaItem[] = [];

   protected abstract renderMediaItem(mediaItem: IMediaItem): HTMLElement;
   protected abstract getMediaItemTemplate(mediaItem: IMediaItem): string;

   protected searchInput: HTMLInputElement;

   constructor(container: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);
      this.debounceSearch = debounce(() => this.handleSearch(), 300);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(galleryTemplate);
   }

   protected initialize(): void {
      this.initializeHTMLElements();
      this.initializeEventListeners();
   }

   protected initializeHTMLElements(): void {
      this.mediaGallery = this.querySelector<HTMLDivElement>('#galleryItems');
      this.pagination = this.querySelector<HTMLUListElement>('#galleryPagination');
      this.previousButton = this.querySelector<HTMLButtonElement>('#galleryPreviousButton');
      this.nextButton = this.querySelector<HTMLButtonElement>('#galleryNextButton');
      this.searchInput = this.querySelector<HTMLInputElement>('#gallerySearch');
   }

   protected initializeEventListeners(): void {
      this.bindEvent(this.previousButton, 'click', () => this.handlePreviousButtonClick());
      this.bindEvent(this.nextButton, 'click', () => this.handleNextButtonClick());
      this.bindEvent(this.searchInput, 'input', () => this.debounceSearch());

      this.bindEvent(this.searchInput, 'keypress', (event) => {
         if ((event as KeyboardEvent).key === 'Enter') {
            event.preventDefault();
            this.handleSearch();
         }
      });
   }

   // search

   protected handleSearch(): void {
      const searchTerm = this.searchInput.value.toLowerCase();
      this.filteredMediaItems = this.mediaItems.filter(mediaItem =>
         mediaItem.fileName.toLowerCase().includes(searchTerm),
      );

      this.resetPagination();
      this.renderGallery();
   }

   // pagination

   protected handlePreviousButtonClick(): void {
      if (this.currentPage > 1) {
         this.currentPage--;
         this.renderGallery();
      }
   }

   protected handleNextButtonClick(): void {
      if (this.currentPage < this.getTotalPages()) {
         this.currentPage++;
         this.renderGallery();
      }
   }

   private resetPagination(): void {
      this.currentPage = 1;
      this.clearPagination();
      this.createPagination();
   }

   private clearPagination(): void {
      this.pageButtons.forEach(button => button.parentElement?.remove());
      this.pageButtons = [];
   }

   protected getTotalPages(): number {
      const result = Math.ceil(this.filteredMediaItems.length / this.itemsPerPage);

      return result === 0
         ? 1
         : result;
   }

   protected createPagination(): void {
      const totalPages = this.getTotalPages();

      this.toggleClass(this.previousButton, 'disabled', totalPages <= 1);
      this.toggleClass(this.nextButton, 'disabled', totalPages <= 1);

      for (let i = 1; i <= totalPages; i++) {
         const li = document.createElement('li');
         this.toggleClass(li, 'page-item');
         this.toggleClass(li, 'active', i === this.currentPage);

         const button = document.createElement('button');
         button.type = 'button';

         this.toggleClass(button, 'page-link');
         this.toggleClass(button, 'shadow-none');

         button.textContent = i.toString();
         this.bindEvent(button, 'click', () => {
            this.currentPage = i;
            this.renderGallery();
         })

         li.appendChild(button);
         this.pagination.insertBefore(li, this.nextButton.parentElement);
         this.pageButtons.push(button);
      }
   }

   // media items

   public setMediaItems(mediaItems: IMediaItem[]): void {
      this.mediaItems = mediaItems;
      this.filteredMediaItems = [...mediaItems];
      this.resetPagination();
      this.renderGallery();
   }

   public addMediaItem(mediaItem: IMediaItem): void {
      this.mediaItems.push(mediaItem);
      this.filteredMediaItems = [...this.mediaItems];
      this.resetPagination();
      this.renderGallery();
   }

   protected getCurrentPageMediaItems(): IMediaItem[] {
      const startIndex = (this.currentPage - 1) * this.itemsPerPage;
      const endIndex = startIndex + this.itemsPerPage;

      return this.filteredMediaItems.slice(startIndex, endIndex);
   }

   // render html

   protected renderPagination(): void {
      const totalPages = this.getTotalPages();

      this.pageButtons.forEach((button, index) => {
         const pageNumber = index + 1;
         const parentElement = button.parentElement;
         if (parentElement)
            this.toggleClass(parentElement, 'active', pageNumber === this.currentPage);
      });

      this.toggleClass(this.previousButton, 'disabled', this.currentPage === 1);
      this.toggleClass(this.nextButton, 'disabled', this.currentPage === totalPages);
   }

   protected renderGallery(): void {
      this.mediaGallery.innerHTML = '';

      const currentMediaItems = this.getCurrentPageMediaItems();
      currentMediaItems.forEach(mediaItem => {
         const element = this.renderMediaItem(mediaItem);
         this.mediaGallery.appendChild(element);
      });

      this.renderPagination();
   }
}