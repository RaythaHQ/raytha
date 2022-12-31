import { Controller } from "stimulus"
import Sortable from 'sortablejs';
import { Notyf } from 'notyf';

export default class extends Controller {
  static targets = [""];

  connect() {
    Sortable.create(document.getElementById('draggableDivTable'), {
        sort: true,
        handle: '.thead-light',
        animation: 150
    });
  }

  reorder() {

  }
}