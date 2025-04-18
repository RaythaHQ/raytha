import { ViewComponent } from '../../base';

export abstract class MenuBaseComponent extends ViewComponent {
   constructor(protected container: HTMLElement, protected controllerIdentifier: string) {
      super(container, controllerIdentifier);
   }

   protected initialize(): void { }

   protected appendToContainer(): void {
      // required empty
   }
}