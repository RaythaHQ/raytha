import { Controller } from 'stimulus'
import Swal from 'sweetalert2'

export default class extends Controller {
    static values = { title: String }
    static targets = ['form']

    warning(event) {
        event.preventDefault();
        Swal.fire({
            title: this.titleValue,
            showCancelButton: true,
            confirmButtonText: `Confirm`,
            icon: "warning"
        }).then((result) => {
            if (result.isConfirmed) {
                this.formTarget.submit();
            }
        });
    }
}