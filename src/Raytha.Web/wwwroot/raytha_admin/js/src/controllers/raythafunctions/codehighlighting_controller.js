import { Controller } from "stimulus"
import { EditorView, basicSetup } from "codemirror";
import { EditorState } from "@codemirror/state";
import { javascript } from "@codemirror/lang-javascript";
import { indentWithTab } from "@codemirror/commands";
import { keymap } from "@codemirror/view";

export default class extends Controller {
   static targets = ["editor", "textarea"];

   connect() {
      const initialValue = this.textareaTarget.value;
      this.editor = new EditorView({
         state: EditorState.create({
            doc: initialValue,
            extensions: [
                basicSetup, 
                javascript(),
                keymap.of([indentWithTab]),
                EditorView.updateListener.of((update) => {
                   if (update.docChanged) {
                      this.textareaTarget.value = this.editor.state.doc.toString();
                   }
                })
            ]
         }),
         parent: this.editorTarget
      });
   }

   disconnect() {
      if (this.editor) this.editor.destroy();
   }
}