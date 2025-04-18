import { ViewComponent } from './ViewComponent';
import { ISynchronizableComponent } from './interfaces/ISynchronizableComponent';
import { SynchronizableComponentKey } from './types/SynchronizableComponentKey';

type Listener<T extends SynchronizableComponentKey> = (value: ISynchronizableComponent[T]) => void;

/**
 * Base class for components that need to be synchronized with each other in different parts of the editor
 * but don't depend on editor state directly.
**/
export abstract class SynchronizableViewComponent<T extends SynchronizableComponentKey> extends ViewComponent {
   private static listeners: Map<SynchronizableComponentKey, Set<Listener<SynchronizableComponentKey>>> = new Map();

   protected abstract getComponentKey(): SynchronizableComponentKey;
   protected abstract onValueChanged(newValue: ISynchronizableComponent[T]): void;

   constructor(container: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);

      if (!this.getComponentKey())
         throw new Error('Component key must be provided');

      this.subscribeToChanges();
   }

   public destroy(): void {
      const key = this.getComponentKey();
      const listeners = SynchronizableViewComponent.listeners.get(key);

      if (listeners) {
         const boundListener = this.onValueChanged.bind(this);
         listeners.delete(boundListener);

         if (listeners.size === 0)
            SynchronizableViewComponent.listeners.delete(key);
      }

      super.destroy();
   }

   protected setValue(value: ISynchronizableComponent[T]): void {
      const key = this.getComponentKey();
      const listeners = SynchronizableViewComponent.listeners.get(key);

      if (!listeners) return;

      try {
         listeners.forEach(listener => listener(value));
      }
      catch (error) {
         console.error(`Error notifying listeners for ${key}:`, error);
      }
   }

   private subscribeToChanges(): void {
      const key = this.getComponentKey();
      const boundListener = this.onValueChanged.bind(this);

      if (!SynchronizableViewComponent.listeners.has(key))
         SynchronizableViewComponent.listeners.set(key, new Set());

      const listeners = SynchronizableViewComponent.listeners.get(key);
      if (listeners && !listeners.has(boundListener))
         listeners.add(boundListener);
   }
}