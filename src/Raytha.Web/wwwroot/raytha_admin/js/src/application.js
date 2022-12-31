import { Application } from "stimulus"
import { definitionsFromContext } from "stimulus/webpack-helpers"
import { Datepicker } from 'vanillajs-datepicker';
import * as Turbo from "@hotwired/turbo"
import * as bootstrap from 'bootstrap'
import Swal from 'sweetalert2'
import 'simplebar';
import Trix from "trix"

const d = document;
document.addEventListener("turbo:load", function () {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl)
    })

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
            confirmButton: 'btn btn-primary me-3',
            cancelButton: 'btn btn-gray'
        },
        buttonsStyling: false
    });

    // options
    const breakpoints = {
        sm: 540,
        md: 720,
        lg: 960,
        xl: 1140
    };

    var sidebar = document.getElementById('sidebarMenu')
    if(sidebar && d.body.clientWidth < breakpoints.lg) {
        sidebar.addEventListener('shown.bs.collapse', function () {
            document.querySelector('body').style.position = 'fixed';
        });
        sidebar.addEventListener('hidden.bs.collapse', function () {
            document.querySelector('body').style.position = 'relative';
        });
    }

    var iconNotifications = d.querySelector('.notification-bell');
    if (iconNotifications) {
        iconNotifications.addEventListener('shown.bs.dropdown', function () {
            iconNotifications.classList.remove('unread');
        });
    }

    [].slice.call(d.querySelectorAll('[data-background]')).map(function(el) {
        el.style.background = 'url(' + el.getAttribute('data-background') + ')';
    });

    [].slice.call(d.querySelectorAll('[data-background-lg]')).map(function(el) {
        if(document.body.clientWidth > breakpoints.lg) {
            el.style.background = 'url(' + el.getAttribute('data-background-lg') + ')';
        }
    });

    [].slice.call(d.querySelectorAll('[data-background-color]')).map(function(el) {
        el.style.background = 'url(' + el.getAttribute('data-background-color') + ')';
    });

    [].slice.call(d.querySelectorAll('[data-color]')).map(function(el) {
        el.style.color = 'url(' + el.getAttribute('data-color') + ')';
    });

    //Tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })

    var datepickers = [].slice.call(d.querySelectorAll('[data-datepicker]'))
    var datepickersList = datepickers.map(function (el) {
        return new Datepicker(el, {
            buttonClass: 'btn'
          });
    })

    var offcanvasElementList = [].slice.call(document.querySelectorAll('.offcanvas'))
    var offcanvasList = offcanvasElementList.map(function (offcanvasEl) {
      return new bootstrap.Offcanvas(offcanvasEl)
    })
});


//Modify Trix toolbar to add more buttons
document.addEventListener("trix-before-initialize", () => {
    Trix.config.attachments.preview.caption.name = false
    Trix.config.attachments.preview.caption.size = false 

    Trix.config.blockAttributes.heading1 = {
        tagName: "h1",
        terminal: true,
        breakOnReturn: true,
        group: false
    }
    Trix.config.blockAttributes.heading2 = {
        tagName: "h2",
        terminal: true,
        breakOnReturn: true,
        group: false
    }

    Trix.config.blockAttributes.heading3 = {
        tagName: "h3",
        terminal: true,
        breakOnReturn: true,
        group: false
    }

    Trix.config.blockAttributes.heading4 = {
        tagName: "h4",
        terminal: true,
        breakOnReturn: true,
        group: false
    }

    Trix.config.blockAttributes.heading5 = {
        tagName: "h5",
        terminal: true,
        breakOnReturn: true,
        group: false
    }

    Trix.config.blockAttributes.heading6 = {
        tagName: "h6",
        terminal: true,
        breakOnReturn: true,
        group: false
    }
})

const application = Application.start()
const context = require.context("./controllers/", true, /^\.\/.*\.js$/)
application.load(definitionsFromContext(context))