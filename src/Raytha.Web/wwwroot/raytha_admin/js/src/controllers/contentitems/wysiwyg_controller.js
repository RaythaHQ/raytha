import { Controller } from 'stimulus'

/* Import TinyMCE */
import tinymce from 'tinymce';

import 'tinymce/skins/ui/oxide/skin.css';

/* Default icons are required. After that, import custom icons if applicable */
import 'tinymce/icons/default';

/* Required TinyMCE components */
import 'tinymce/themes/silver';
import 'tinymce/models/dom';

import 'tinymce/plugins/advlist';
import 'tinymce/plugins/autolink';
import 'tinymce/plugins/lists';
import 'tinymce/plugins/link';
import 'tinymce/plugins/image';
import 'tinymce/plugins/charmap';
import 'tinymce/plugins/preview';
import 'tinymce/plugins/pagebreak';
import 'tinymce/plugins/nonbreaking';
import 'tinymce/plugins/table';
import 'tinymce/plugins/emoticons';
import 'tinymce/plugins/insertdatetime';
import 'tinymce/plugins/wordcount';
import 'tinymce/plugins/directionality';
import 'tinymce/plugins/fullscreen';
import 'tinymce/plugins/searchreplace';
import 'tinymce/plugins/visualblocks';
import 'tinymce/plugins/visualchars';


export default class extends Controller {
    static targets = ['editor']
    static values = { usedirectuploadtocloud: Boolean, mimetypes: String, maxfilesize: Number }

    connect() {
        tinymce.init({
            target: this.editorTarget,
            plugins: 'advlist autolink lists link image charmap preview pagebreak nonbreaking table insertdatetime wordcount directionality fullscreen searchreplace visualblocks visualchars',
            toolbar: 'undo redo | styleselect | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | link image | print preview media fullpage | forecolor backcolor emoticons | spellchecker | table | charmap | insertdatetime | pagebreak | nonbreaking | code',
            toolbar_mode: 'floating',
            tinycomments_mode: 'embedded',
            promotion: false,
            skin: false,
            content_css: false,
            images_upload_url: '/raytha/media-items/upload',
            file_picker_types: 'file image media',
            relative_urls: false,
            remove_script_host: true,
        });
    }

    disconnect() {
        tinymce.activeEditor.destroy();
    }
}