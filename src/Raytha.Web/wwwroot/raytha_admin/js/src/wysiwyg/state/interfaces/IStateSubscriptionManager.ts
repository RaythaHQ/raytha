import { IEditorState } from "wysiwyg/model/interfaces/IEditorState";
import { EditorStateKeys } from "../types";
import { ISubscriber } from "./ISubscriber";

export interface IStateSubscriptionManager {
   subscribe<T extends EditorStateKeys>(subscriber: ISubscriber<T>): void,
   unsubscribe<T extends EditorStateKeys>(subscriber: ISubscriber<T>): void,
   notifySubscribers(newState: IEditorState): void,
}