import Sortable from 'stimulus-sortable'
import { Notyf } from 'notyf';

export default class extends Sortable {
  onUpdate({ item, newIndex }) {
    if (!item.dataset.sortableUpdateUrl) return

    const param = this.resourceNameValue ? `${this.resourceNameValue}[${this.paramNameValue}]` : this.paramNameValue

    const data = new FormData()
    data.append(param, newIndex + 1)

    fetch(item.dataset.sortableUpdateUrl, {
      method: "PATCH",
      body: data
    })
    .then(res => res.json())
    .then(res => {
        const notyf = new Notyf();
        if (res.success) {
            notyf.success('Fields succesfully reordered');
        } else {
            notyf.error(res.error);
        }
    })
    .catch(err => {
        console.error(err);
    });
  }
}