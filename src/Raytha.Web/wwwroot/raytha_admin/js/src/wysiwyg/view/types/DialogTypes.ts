import { SearchAndReplaceDialog } from '../components';

export enum DialogType {
   SearchAndReplaceDialog,
}

export type DialogTypeMap = {
   [DialogType.SearchAndReplaceDialog]: SearchAndReplaceDialog,
}