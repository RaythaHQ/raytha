import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["relatedContentTypeSection", "fieldType", "relatedContentTypeValue"];

    connect() {
        this.toggleSection();
    }

    toggleSection() {
        var fieldIsOneToOneRelationship = this.fieldTypeTarget.value == "one_to_one_relationship"
        this.relatedContentTypeSectionTarget.hidden = !fieldIsOneToOneRelationship;
        this.relatedContentTypeValueTarget.required = fieldIsOneToOneRelationship;
    }
}