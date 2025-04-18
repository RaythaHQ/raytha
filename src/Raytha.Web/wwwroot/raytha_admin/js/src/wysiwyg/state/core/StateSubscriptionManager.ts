import { IEditorState } from "wysiwyg/model/interfaces/IEditorState";
import { EditorStateKeys } from "../types";
import { IStateSubscriptionManager } from "../interfaces/IStateSubscriptionManager";
import { ISubscriber } from "../interfaces/ISubscriber";

export class StateSubscriptionManager implements IStateSubscriptionManager {
   private static instance: IStateSubscriptionManager | null;
   private subscribers: Array<ISubscriber<any>>;
   private previousState: IEditorState | null;

   private constructor() {
      this.subscribers = [];
      this.previousState = null;
   }

   public static getInstance(): IStateSubscriptionManager {
      if (!this.instance)
         this.instance = new StateSubscriptionManager();

      return this.instance;
   }

   public static resetInstance(): void {
      if (this.instance !== null)
         this.instance = null;
   }

   public subscribe<T extends EditorStateKeys>(subscriber: ISubscriber<T>): void {
      this.subscribers.push(subscriber);
   }

   public unsubscribe<T extends EditorStateKeys>(subscriber: ISubscriber<T>): void {
      this.subscribers = this.subscribers.filter(sub => sub !== subscriber);
   }

   public notifySubscribers(newState: IEditorState): void {
      this.subscribers.forEach(subscriber => {
         try {
            const newValue = subscriber.selectors(newState);
            const previousValue = this.previousState
               ? subscriber.selectors(this.previousState)
               : null;

            if (this.shouldNotifySubscriber(newValue, previousValue))
               subscriber.callback(newValue);
         } catch (error) {
            console.error('Error in subscriber callback:', error);
            this.unsubscribe(subscriber);
         }
      });

      this.previousState = { ...newState };
   }

   private shouldNotifySubscriber<T>(newValue: T, previousValue: T): boolean {
      if (!previousValue) return true;

      if (typeof newValue !== 'object') return newValue !== previousValue;

      return JSON.stringify(newValue) !== JSON.stringify(previousValue);
   }
}