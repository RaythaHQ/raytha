import { EditorStateKeys, EditorStateSelector, EditorStateValue } from "../types";

export interface ISubscriber<T extends EditorStateKeys> {
   selectors: EditorStateSelector<T>,
   callback: (stateValue: EditorStateValue<T>) => void,
}