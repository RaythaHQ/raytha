import { IViewComponent } from './interfaces/IViewComponent';

export abstract class ViewComponent implements IViewComponent {
   private readonly eventHandlers: Map<HTMLElement, Map<string, EventListener>> = new Map();

   protected element: HTMLElement;

   protected abstract render(): HTMLElement;
   protected abstract initialize(): void;

   constructor(protected container: HTMLElement, protected controllerIdentifier: string) {
      if (!container)
         throw new Error('Container is empty');

      if (!controllerIdentifier)
         throw new Error('Controller identifier must be a non-empty');

      this.element = this.render();
      this.appendToContainer();
      this.initialize();
   }

   public getElement(): HTMLElement {
      return this.element;
   }

   public destroy(): void {
      this.unbindAllEvents();
      this.element.remove();
   }

   protected createElementFromTemplate(template: string): HTMLElement {
      const div = document.createElement('div');
      div.innerHTML = template.replaceAll('{{controllerIdentifier}}', this.controllerIdentifier).trim();

      return div.firstChild as HTMLElement;
   }

   protected appendToContainer(): void {
      this.container.appendChild(this.element);
   }

   protected bindEvent(element: HTMLElement, eventType: string, handler: (event: Event) => void): void {
      if (!element)
         throw new Error('Cannot bind event to null element');

      if (!this.eventHandlers.has(element))
         this.eventHandlers.set(element, new Map());

      const elementEventHandlers = this.eventHandlers.get(element);
      if (elementEventHandlers?.has(eventType))
         element.removeEventListener(eventType, elementEventHandlers.get(eventType)!);

      elementEventHandlers?.set(eventType, handler);
      element.addEventListener(eventType, handler);
   }

   protected unbindEvent(element: HTMLElement, eventType: string): void {
      const elementEventHandlers = this.eventHandlers.get(element);
      if (!elementEventHandlers) return;

      const handler = elementEventHandlers.get(eventType);
      if (handler) {
         element.removeEventListener(eventType, handler);
         elementEventHandlers.delete(eventType);
      }

      if (elementEventHandlers.size === 0)
         this.eventHandlers.delete(element);
   }

   protected toggleClass(element: HTMLElement, className: string, force?: boolean): void {
      element.classList.toggle(className, force);
   }

   protected toggleAttribute(element: HTMLElement, attribute: string, force?: boolean): void {
      element.toggleAttribute(attribute, force);
   }

   protected setStyle(style: string, value: string): void {
      this.element.style.setProperty(style, value);
   }

   protected querySelector<T extends HTMLElement>(selector: string): T {
      const element = this.element.querySelector(selector);
      if (!element)
         throw new Error(`Element with selector ${selector} not found in ${this.element}`);

      return element as T;
   }

   protected querySelectorAll<T extends HTMLElement>(selector: string): NodeListOf<T> {
      const elements = this.element.querySelectorAll(selector);
      if (elements.length === 0)
         throw new Error(`Elements with selector ${selector} not found in ${this.element}`);

      return elements as NodeListOf<T>;
   }

   private unbindAllEvents(): void {
      this.eventHandlers.forEach((elementHandlers, element) => {
         elementHandlers.forEach((handler, type) => {
            element.removeEventListener(type, handler);
         });
      });

      this.eventHandlers.clear();
   }
}