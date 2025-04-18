export interface IUppyWrapperDashboardConfig {
   id: string,
   inline: boolean,
   target: HTMLElement,
   height?: number,
   metaFields?: Array<{
      id: string,
      name: string,
   }>,
}