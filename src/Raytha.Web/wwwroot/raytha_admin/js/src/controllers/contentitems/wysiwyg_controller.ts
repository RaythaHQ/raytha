import { Controller } from "@hotwired/stimulus";
import { UppyWrapper } from "uppyWrapper/UppyWrapper";
import { EditorModel } from "wysiwyg/model/core/EditorModel";
import { IEditorCommands } from "wysiwyg/model/interfaces/IEditorCommands";
import { IEditorModel } from "wysiwyg/model/interfaces/IEditorModel";
import { StateSubscriptionManager } from "wysiwyg/state/core/StateSubscriptionManager";
import { IMediaItem } from "wysiwyg/view/components/modals/galleries/interfaces/IMediaItem";
import { EditorView } from "wysiwyg/view/EditorView";
import { EditorElement } from "wysiwyg/view/enums/EditorViewComponents";
import { IEditorView } from "wysiwyg/view/interfaces/IEditorView";
import { SwalAlert } from "wysiwyg/view/SwalAlert";
import { DialogType } from "wysiwyg/view/types/DialogTypes";
import { ModalType } from "wysiwyg/view/types/ModalTypes";

import "../../../../css/tiptap.editor.css";
import "../../../../css/wysiwyg.container.css";

export default class WysiwygController extends Controller {
   static targets = [
      "editorContainer",
      "editorContent",
   ];

   readonly editorContainerTarget: HTMLElement;
   readonly editorContentTarget: HTMLTextAreaElement;

   static values = {
      usedirectuploadtocloud: Boolean,
      maxfilesize: Number,
      allowmultipleuploads: Boolean,
      autoproceed: Boolean,
      maxnumberoffiles: Number,
      pathbase: String,
      imagemediaitems: Array,
      videomediaitems: Array,
   };

   usedirectuploadtocloudValue: boolean;
   allowmultipleuploadsValue: boolean;
   autoproceedValue?: boolean;
   maxfilesizeValue: number;
   maxnumberoffilesValue?: number;
   pathbaseValue: string;
   imagemediaitemsValue: Array<IMediaItem>;
   videomediaitemsValue: Array<IMediaItem>;

   private editorView: IEditorView;
   private editorModel: IEditorModel;
   private imageUploader: UppyWrapper;
   private videoUploader: UppyWrapper;

   private initializeEditor() {
      const fileUploadFn = async (file: File): Promise<string> => {
         if (file.type.includes('image')) return await this.imageUploader.uploadFile(file);

         if (file.type.includes('video')) return await this.videoUploader.uploadFile(file);

         throw new Error('Invalid file type');
      };

      this.editorView = new EditorView(this.editorContainerTarget, this.identifier);
      this.editorModel = new EditorModel()
         .addRightClickMenuElement(this.editorView.getComponent(EditorElement.RightClickMenu))
         .addTableBubbleMenuElement(this.editorView.getComponent(EditorElement.TableBubbleMenu))
         .addListBubbleMenuElement(this.editorView.getComponent(EditorElement.ListBubbleMenu))
         .addFileUploadFn(fileUploadFn)
         .initializeEditor();

      this.editorView.appendEditor(this.editorModel.getEditorElement());
   }

   private setupShowModals() {
      this.setupLinkModal();
      this.setupImageModal();
      this.setupVideoModal();
      this.setupSourceCodeModal();
      this.setupPreviewModal();
      this.setupDivModal();
      this.setupWordCountModal();
   }

   private setupLinkModal() {
      const linkModal = this.editorView.getModal(ModalType.Link);
      linkModal.bindShowModal(() => {
         if (this.editorModel.getCommands().isImageNode())
            linkModal.hideTextToDisplayInput();

         if (this.editorModel.getCommands().isLinkNode()) {
            const linkAttributes = this.editorModel.getCommands().getLinkAttributes();
            linkModal.setUrl(linkAttributes.href);
            linkModal.setTitle(linkAttributes.title);
            linkModal.setOpenInNewTab(linkAttributes.target === '_blank');
            linkModal.setTextToDisplay(this.editorModel.getCommands().getTextOnCursor());
         } else {
            const text = this.editorModel.getCommands().getSelectedText();
            linkModal.setTextToDisplay(text);
         }
      });

      linkModal.bindHideModal(() => {
         linkModal.showTextToDisplayInput();
         linkModal.clearInputs();
      });
   }

   private setupImageModal() {
      const imageModal = this.editorView.getModal(ModalType.Image);
      imageModal.getGallery().setMediaItems(this.imagemediaitemsValue.map(mi => ({
         fileName: mi.fileName,
         url: mi.url,
      })));

      imageModal.bindShowModal(() => {
         const imageAttributes = this.editorModel.getCommands().getImageAttributes();
         const src = imageAttributes.src;
         const alt = imageAttributes.alt;

         let width, height;

         if (src && !imageAttributes.width && !imageAttributes.height) {
            const image = new Image();
            image.onload = () => {
               width = image.width.toString();
               height = image.height.toString();
            };

            image.src = imageAttributes.src;
         }
         else {
            width = imageAttributes.width;
            height = imageAttributes.height;
         }

         imageModal.setInputValues(src, width, height, alt);
      })

      imageModal.bindHideModal(() => {
         this.imageUploader.cancelAll();
         imageModal.clearInputs();
         imageModal.showGeneralTab();
      })
   }

   private setupVideoModal() {
      const videoModal = this.editorView.getModal(ModalType.Video);
      videoModal.getGallery().setMediaItems(this.videomediaitemsValue.map(mi => ({
         fileName: mi.fileName,
         url: mi.url,
      })));

      videoModal.bindShowModal(() => {
         const videoAttributes = this.editorModel.getCommands().getVideoAttributes();
         videoModal.setInputValues(videoAttributes.src, videoAttributes.width, videoAttributes.height);
         videoModal.showGeneralTab();
      });

      videoModal.bindHideModal(() => {
         videoModal.clearInputs();
         this.videoUploader.cancelAll();
      });
   }

   private setupSourceCodeModal() {
      const sourceCodeModal = this.editorView.getModal(ModalType.SourceCode);
      sourceCodeModal.bindShowModal(() => {
         const html = this.editorModel.getCommands().getHTML();
         sourceCodeModal.setHTMLContent(html);
      });
   }

   private setupPreviewModal() {
      const previewModal = this.editorView.getModal(ModalType.Preview);
      previewModal.bindShowModal(() => {
         const html = this.editorModel.getCommands().getHTML();
         previewModal.setHTMLContent(html);
      });
   }

   private setupDivModal() {
      const divModal = this.editorView.getModal(ModalType.Div);
      divModal.bindShowModal(event => {
         //@ts-ignore
         const button = event.relatedTarget;
         const nodePos = button.getAttribute('data-pos');
         const divAttributes = this.editorModel.getCommands().getDivAttributes(Number(nodePos));
         divModal.setInputs(divAttributes.class, divAttributes.style, nodePos);
      });
   }

   private setupWordCountModal() {
      const wordCountModal = this.editorView.getModal(ModalType.WordCount);
      wordCountModal.bindShowModal(() => {
         const wordCount = this.editorModel.getCommands().getWordCount();

         wordCountModal.setWordCount({
            documentWords: wordCount.document.words,
            documentCharacters: wordCount.document.characters,
            documentCharactersWithoutSpaces: wordCount.document.charactersWithoutSpaces,
            selectionWords: wordCount.selection.words,
            selectionCharacters: wordCount.selection.characters,
            selectionCharactersWithoutSpaces: wordCount.selection.charactersWithoutSpaces,
         });
      })
   }

   private setupEditorContent() {
      const initialContent = this.editorContentTarget.value;
      if (initialContent)
         this.editorModel.getCommands().setContent(initialContent);

      this.editorModel.getStateManager().onContentChanged((content) => {
         this.editorContentTarget.value = content;
      });
   }

   private setupUploaders() {
      this.setupImageUploader();
      this.setupVideoUploader();
   }

   private setupImageUploader() {
      const imageModal = this.editorView.getModal(ModalType.Image);
      this.imageUploader = new UppyWrapper({
         id: 'imageUploader',
         useDirectUploadToCloud: this.usedirectuploadtocloudValue,
         pathbase: this.pathbaseValue,
         autoProceed: false,
         onUploadSuccess: (file, fileUrl) => {
            const image = new Image();
            image.onload = () => {
               imageModal.setInputValues(fileUrl, image.width.toString(), image.height.toString());
            }

            image.src = fileUrl;
            imageModal.showGeneralTab();

            imageModal.getGallery().addMediaItem({
               fileName: file?.name ?? fileUrl.split('/').pop()?.split('_').pop()!,
               url: fileUrl,
            });
         }
      });

      this.imageUploader.setRestrictions({
         allowedFileTypes: ['image/*'],
         maxFileSize: this.maxfilesizeValue,
      });

      this.imageUploader.initializeDashboard({
         id: 'imageUploader',
         inline: true,
         target: this.editorView.getModal(ModalType.Image).getUploadContainer(),
         metaFields: [
            {
               id: "imageAltText",
               name: "Alternative description",
            },
         ]
      })

      this.imageUploader.initializeImageEditor();
   }

   private setupVideoUploader() {
      const videoModal = this.editorView.getModal(ModalType.Video);
      this.videoUploader = new UppyWrapper({
         id: 'videoUploader',
         useDirectUploadToCloud: false,
         pathbase: this.pathbaseValue,
         autoProceed: true,
         onUploadSuccess: (file, fileUrl) => {
            videoModal.getGallery().addMediaItem({
               url: fileUrl,
               fileName: fileUrl,
            });

            const video = document.createElement('video');
            video.src = fileUrl;
            video.addEventListener('loadedmetadata', () => {
               videoModal.setInputValues(fileUrl, video.videoWidth.toString(), video.videoHeight.toString());
            }, { once: true });

            videoModal.showGeneralTab();

            videoModal.getGallery().addMediaItem({
               fileName: file?.name ?? fileUrl.split('/').pop()?.split('_').pop()!,
               url: fileUrl,
            })
         }
      })

      this.videoUploader.setRestrictions({
         allowedFileTypes: ['video/*'],
         maxFileSize: this.maxfilesizeValue,
      });

      this.videoUploader.initializeDashboard({
         id: 'videoUploaderDashboard',
         inline: true,
         target: videoModal.getUploadContainer(),
      })
   }

   private isValidEditorCommand(command: string): command is keyof IEditorCommands {
      const commands = this.editorModel.getCommands();

      return (
         typeof command === "string" &&
         command in commands &&
         typeof commands[command as keyof IEditorCommands] === 'function'
      );
   }

   private executeEditorCommand(command: keyof IEditorCommands, commandFn: Function): boolean | void {
      if (typeof commandFn !== 'function')
         throw new Error(`Command ${command} is not a function`);

      return commandFn.call(this.editorModel.getCommands());
   }

   private handleCommandError(command: string, error: unknown): void {
      const errorMessage = error instanceof Error
         ? error.message
         : `An error occurred while executing the ${command} command`;

      SwalAlert.showErrorAlert(errorMessage);
   }

   public connect() {
      this.initializeEditor();
      this.setupShowModals();
      this.setupUploaders();
      this.setupEditorContent();
   }

   public executeCommand({ params }: { params: { command: keyof IEditorCommands } }) {
      const { command } = params;

      try {
         if (!this.isValidEditorCommand(command))
            throw new Error(`Invalid command: ${command}`);

         const commands = this.editorModel.getCommands();
         const commandFn = commands[command];

         const result = this.executeEditorCommand(command, commandFn);
         if (result === true)
            this.editorModel.getStateManager().forceUpdate();
      } catch (error) {
         this.handleCommandError(command, error);
      }
   }

   public setFontFamily({ params: { fontFamily } }: { params: { fontFamily: string } }) {
      this.editorModel.getCommands().setFontFamily(fontFamily);
   }

   public setFontSize({ params: { fontSize } }: { params: { fontSize: string } }) {
      this.editorModel.getCommands().setFontSize(fontSize);
   }

   public insertHeading({ params: { level } }: { params: { level: number } }) {
      this.editorModel.getCommands().insertHeading(level);
   }

   public setTextAlign({ params: { textAlign } }: { params: { textAlign: string } }) {
      const isImageNode = this.editorModel.getCommands().isImageNode();
      if (isImageNode) {
         this.editorModel.getCommands().setImageAlign(textAlign);
      }
      else {
         this.editorModel.getCommands().setTextAlign(textAlign);
      }
   }

   public setLineHeight({ params: { lineHeight } }: { params: { lineHeight: string } }) {
      this.editorModel.getCommands().setLineHeight(lineHeight);
   }

   public insertTable(event: PointerEvent) {
      const target = event.target as HTMLElement;
      const rows = parseInt(target.dataset.row!);
      const cols = parseInt(target.dataset.col!);

      this.editorModel.getCommands().insertTable(rows, cols);
      this.editorModel.getStateManager().forceUpdate();
   }

   public setTextColor({ params: { color } }: { params: { color: string } }) {
      this.editorModel.getCommands().setTextColor(color);
   }

   public setBackgroundColor({ params: { color } }: { params: { color: string } }) {
      this.editorModel.getCommands().setBackgroundColor(color);
   }

   public insertLink(event: Event) {
      event.preventDefault();
      const linkModal = this.editorView.getModal(ModalType.Link);
      const linkData = linkModal.getFormValues(event.target as HTMLFormElement);

      this.editorModel.getCommands().insertLink(linkData.href, linkData.text, linkData.title, linkData.openInNewTab)
         ? linkModal.hide()
         : SwalAlert.showErrorAlert('Please enter a valid link URL');
   }

   public insertImageByUrl(event: SubmitEvent) {
      event.preventDefault();

      const imageModal = this.editorView.getModal(ModalType.Image);
      const imageData = imageModal.getFormValues(event.target as HTMLFormElement);

      this.editorModel.getCommands().insertImage(imageData.src, imageData.width, imageData.height, imageData.altText)
         ? imageModal.hide()
         : SwalAlert.showErrorAlert('Please enter a valid image URL');
   }

   public insertImageFromFileStorage({ params: { src } }: { params: { src: string } }) {
      this.editorModel.getCommands().insertImage(src);

      this.editorView.getModal(ModalType.Image).hide();
   }

   public insertVideoByUrl(event: SubmitEvent) {
      event.preventDefault();

      const videoModal = this.editorView.getModal(ModalType.Video);
      const videoData = videoModal.getFormValues(event.target as HTMLFormElement);

      this.editorModel.getCommands().insertVideo(videoData.src, videoData.width, videoData.height)
         ? videoModal.hide()
         : SwalAlert.showErrorAlert('Please enter a valid YouTube/Vimeo/Media item URL');
   }

   public insertVideoFromFileStorage({ params: { src } }: { params: { src: string } }) {
      this.editorModel.getCommands().insertVideo(src, '640', '480');

      this.editorView.getModal(ModalType.Video).hide();
   }

   public updateSourceCode(event: SubmitEvent) {
      event.preventDefault();

      const sourceCodeModal = this.editorView.getModal(ModalType.SourceCode);
      const sourceCodeData = sourceCodeModal.getFormValues(event.target as HTMLFormElement);
      try {
         this.editorModel.getCommands().setContent(sourceCodeData.sourceCode);
         sourceCodeModal.hide();
      } catch (error) {
         const err = error as Error;
         SwalAlert.showErrorAlert(`invalid content ${err.toString()}`);
      }
   }

   public insertSpecialCharacter({ params: { character } }: { params: { character: string } }) {
      this.editorModel.getCommands().insertContent(character);
      this.editorView.getModal(ModalType.SpecialCharacters).hide();
   }

   public insertDateTime(event: PointerEvent) {
      const text = (event.target as HTMLElement).textContent;
      this.editorModel.getCommands().insertContent(text!);
   }

   public updateCSSDiv(event: Event) {
      event.preventDefault();

      const divModal = this.editorView.getModal(ModalType.Div);
      const divData = divModal.getFormValues(event.target as HTMLFormElement);
      this.editorModel.getCommands().updateCSSDiv(divData.nodePos, divData.cssClass, divData.cssStyle)
         ? divModal.hide()
         : SwalAlert.showErrorAlert('Please enter a valid CSS class or style');
   }

   public toggleSearchAndReplaceDialog() {
      this.editorView.getDialog(DialogType.SearchAndReplaceDialog).toggleDialog();
   }

   public setSearchTerm(event: Event) {
      const searchTerm = (event.target as HTMLInputElement).value;
      this.editorModel.getCommands().setSearchTerm(searchTerm);
      this.editorModel.getStateManager().forceUpdate();
   }

   public setReplaceTerm(event: Event) {
      const replaceTerm = (event.target as HTMLInputElement).value;
      this.editorModel.getCommands().setReplaceTerm(replaceTerm);
      this.editorModel.getStateManager().forceUpdate();
   }

   public setCaseSensitive(event: Event) {
      const isCaseSensitive = (event.target as HTMLInputElement).checked;
      this.editorModel.getCommands().setCaseSensitive(isCaseSensitive);
      this.editorModel.getStateManager().forceUpdate();
   }

   public disconnect() {
      this.editorModel.destroy();
      this.editorView.destroy();
      this.imageUploader.destroy();
      this.videoUploader.destroy();
      StateSubscriptionManager.resetInstance();
   }
}
