import { IEditorState } from 'wysiwyg/model/interfaces/IEditorState';

type NestedKeys<T> = T extends object
   ? { [K in string & keyof T]: K | `${K & string}.${NestedKeys<T[K]>}` }[string & keyof T]
   : never;

type NestedValue<T, P extends string> = P extends keyof T
   ? T[P]
   : P extends `${infer K}.${infer R}`
      ? K extends keyof T
         ? NestedValue<T[K], R>
         : never
      : never;

export type EditorStateKeys = keyof IEditorState | Partial<IEditorState> | NestedKeys<IEditorState>;
export type EditorStateValue<T> = T extends keyof IEditorState
   ? IEditorState[T]
   : T extends Partial<IEditorState>
      ? T
      : T extends string
         ? NestedValue<IEditorState, T>
         : never;

export type EditorStateSelector<T> = (state: IEditorState) => EditorStateValue<T>;