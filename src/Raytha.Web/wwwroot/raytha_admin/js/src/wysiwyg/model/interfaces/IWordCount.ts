export interface IWordCount {
   document: {
      words: number,
      characters: number,
      charactersWithoutSpaces: number,
   },
   selection: {
      words: number,
      characters: number,
      charactersWithoutSpaces: number,
   },
}