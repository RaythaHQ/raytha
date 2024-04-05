import { Controller } from "stimulus"
import * as monaco from 'monaco-editor/esm/vs/editor/editor.api';

export default class extends Controller {
    static targets = ["editor", "textarea"]

    connect() {
        this.editor = monaco.editor.create(this.editorTarget,
            {
                value: this.textareaTarget.value,
                language: 'html',
                automaticLayout: true,
                scrollBeyondLastLine: false
            });

        this.boundEditorChangedEvent = this.updateEditorFieldValue.bind(this);
        this.editor.onDidChangeModelContent(this.boundEditorChangedEvent);
    }

    disconnect() {
        this.editor.dispose();
    }

    updateEditorFieldValue() {
        const content = this.editor.getValue();
        this.textareaTarget.value = content;
    }
}