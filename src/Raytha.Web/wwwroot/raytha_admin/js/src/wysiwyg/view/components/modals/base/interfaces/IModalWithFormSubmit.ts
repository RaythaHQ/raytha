export interface IModalWithFormSubmit<T> {
   getFormValues(element: HTMLFormElement): T,
}