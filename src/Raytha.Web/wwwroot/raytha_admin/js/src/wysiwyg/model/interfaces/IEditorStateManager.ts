export interface IEditorStateManager {
   onContentChanged(callback: (content: string) => void): void,
   forceUpdate(): void,
   destroy(): void,
}