function bindDeveloperNameSync(sourceId, destinationId) {
    const source = document.getElementById(sourceId);
    const destination = document.getElementById(destinationId);

    if (!source || !destination) return;

    source.addEventListener('input', function () {
        const labelValue = this.value;

        const developerName = labelValue
            .toLowerCase()
            .replace(/\s+/g, '_')
            .replace(/[^a-z0-9_]/g, '')
            .replace(/_+/g, '_');

        destination.value = developerName;
    });
}
