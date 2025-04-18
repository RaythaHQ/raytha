import { EditorStateSelector, EditorStateValue } from 'wysiwyg/state/types';
import { StateSubscriptionManager } from 'wysiwyg/state/core/StateSubscriptionManager';
import { IStateSubscriptionManager } from 'wysiwyg/state/interfaces/IStateSubscriptionManager';
import { ViewComponent } from './ViewComponent';
import { EditorStateKeys } from 'wysiwyg/state/types';

/**
 * Base class for components that need to react to editor state changes.
**/
export abstract class UpdateableViewComponent<T extends EditorStateKeys> extends ViewComponent {
   private stateSubscriptionManager: IStateSubscriptionManager;

   protected abstract getStateSelector(): EditorStateSelector<T>;
   protected abstract onStateChanged(newValue: EditorStateValue<T>): void;

   constructor(container: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);
      this.stateSubscriptionManager = StateSubscriptionManager.getInstance();
      this.subscribeToState();
   }

   protected subscribeToState(): void {
      if (this.stateSubscriptionManager) {
         this.stateSubscriptionManager.subscribe({
            selectors: this.getStateSelector(),
            callback: (stateValue) => {
               this.onStateChanged(stateValue);
            },
         });
      }
   }
}