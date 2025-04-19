export interface IModalBaseComponent {
   bindShowModal(handler: (event: Event) => void): void,
   bindHideModal(handler: (event: Event) => void): void,
   show(): void,
   hide(): void,
   destroy(): void,
}