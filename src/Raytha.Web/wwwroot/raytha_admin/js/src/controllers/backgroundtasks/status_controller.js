import { Controller } from 'stimulus'

export default class extends Controller {
    static targets = ['progressBar', 'statusInfo', 'backToView']

    static values = {
        pathbase: String
    }

    connect() {
        this.currentPath = window.location.pathname + '?json=true';
        this.currentTaskStep = 0;
        this.startCheckingStatus();
    }

    disconnect() {
        this.stopCheckingStatus();
        this.currentPath = '';
        this.currentTaskStep = 0;
    }

    startCheckingStatus() {     
        this.statusCheck = setInterval(() => {
            this.refresh();
        }, 1000);
    }

    stopCheckingStatus() {
        if (this.statusCheck) {
            clearInterval(this.statusCheck)
        }
    }

    refresh() {
        fetch(this.currentPath)
            .then(response => response.json())
            .then(task => {
                console.log(task);
                if (task.status.developerName == 'processing' || task.status.developerName == 'enqueued') {
                    if (this.currentTaskStep != task.taskStep) {
                        this.progressBarTarget.style = `width: ${task.percentComplete}%`;
                        if (task.statusInfo) {
                            var html = "<li class=\"list-group-item\">" + task.statusInfo + "</li>";
                            this.statusInfoTarget.innerHTML += html;
                        }
                        this.currentTaskStep = task.taskStep;
                    }
                } else if (task.status.developerName == 'error') {
                    this.progressBarTarget.classList.add('bg-danger');
                    var html = "<li class=\"list-group-item\">An error has occurred: " + task.errorMessage + "</li>";
                    this.statusInfoTarget.innerHTML += html;
                    this.progressBarTarget.style = `width: 100%`;
                    this.backToViewTarget.style = '';
                    this.stopCheckingStatus();
                } else if (task.status.developerName == 'complete') {
                    this.progressBarTarget.classList.add('bg-success');
                    if (task.statusInfo) {
                       var html = "<li class=\"list-group-item\">" + task.statusInfo + "</li>";
                       this.statusInfoTarget.innerHTML += html;
                    }
                    var html = "<li class=\"list-group-item\">Background task complete</li>";
                    this.statusInfoTarget.innerHTML += html;              
                    this.progressBarTarget.style = `width: 100%`;
                    this.backToViewTarget.style = '';
                    this.stopCheckingStatus();
                    if (task.statusInfo) {
                        const mediaItem = JSON.parse(task.statusInfo);
                        const URL = `${this.pathbaseValue}/raytha/media-items/objectkey/${mediaItem.ObjectKey}`;
                        window.location.href = URL;
                    }
                }
            })
            .catch((error) => {
                console.log(error);
            })
    }
}