import { Controller } from 'stimulus'
import debounce from 'lodash.debounce'

export default class extends Controller {
    static targets = ['input', 'hidden', 'results', 'customerspinner', 'addRecordButton', 'selectedLabelSection', 'selectedLabel', 'hiddenPrimaryField']
    static values = { src: String, contenttype: String }

    connect() {
        this.resultsTarget.hidden = true
        this.customerspinnerTarget.hidden = true
        this.inputTarget.hidden = true
        this.addRecordButtonTarget.hidden = true
        this.selectedLabelSectionTarget.hidden = true

        this.inputTarget.setAttribute('autocomplete', 'off')
        this.inputTarget.setAttribute('spellcheck', 'false')

        this.mouseDown = false

        this.onInputChange = debounce(this.onInputChange.bind(this), 300)
        this.onResultsClick = this.onResultsClick.bind(this)
        this.onResultsMouseDown = this.onResultsMouseDown.bind(this)
        this.onInputBlur = this.onInputBlur.bind(this)
        this.onKeydown = this.onKeydown.bind(this)
        this.onInputFocus = this.onInputFocus.bind(this)

        this.inputTarget.addEventListener('keydown', this.onKeydown)
        this.inputTarget.addEventListener('blur', this.onInputBlur)
        this.inputTarget.addEventListener('input', this.onInputChange)
        this.inputTarget.addEventListener('focus', this.onInputFocus)
        this.resultsTarget.addEventListener('mousedown', this.onResultsMouseDown)
        this.resultsTarget.addEventListener('click', this.onResultsClick)

        if (!this.hiddenTarget.value) {
            this.hiddenTarget.value = ''
            this.hiddenPrimaryFieldTarget.value = ''
            this.addRecordButtonTarget.hidden = false
            this.selectedLabelSectionTarget.hidden = true 
        } else {
            this.addRecordButtonTarget.hidden = true
            this.selectedLabelSectionTarget.hidden = false
        }
    }

    disconnect() {
        this.inputTarget.removeEventListener('keydown', this.onKeydown)
        this.inputTarget.removeEventListener('focus', this.onInputFocus)
        this.inputTarget.removeEventListener('blur', this.onInputBlur)
        this.inputTarget.removeEventListener('input', this.onInputChange)
        this.resultsTarget.removeEventListener('mousedown', this.onResultsMouseDown)
        this.resultsTarget.removeEventListener('click', this.onResultsClick)
    }

    sibling(next) {
        const options = Array.from(this.resultsTarget.querySelectorAll('[role="option"]'))
        const selected = this.resultsTarget.querySelector('[aria-selected="true"]')
        const index = options.indexOf(selected)
        const sibling = next ? options[index + 1] : options[index - 1]
        const def = next ? options[0] : options[options.length - 1]
        return sibling || def
    }

    select(target) {
        for (const el of this.resultsTarget.querySelectorAll('[aria-selected="true"]')) {
            el.removeAttribute('aria-selected')
            el.classList.remove('active')
        }
        target.setAttribute('aria-selected', 'true')
        target.classList.add('active')
        this.inputTarget.setAttribute('aria-activedescendant', target.id)
    }

    onKeydown(event) {
        switch (event.key) {
            case 'Escape':
                if (!this.resultsTarget.hidden) {
                    this.hideAndRemoveOptions()
                    event.stopPropagation()
                    event.preventDefault()
                }
                break
            case 'ArrowDown':
                {
                    const item = this.sibling(true)
                    if (item) this.select(item)
                    event.preventDefault()
                }
                break
            case 'ArrowUp':
                {
                    const item = this.sibling(false)
                    if (item) this.select(item)
                    event.preventDefault()
                }
                break
            case 'Tab':
                {
                    const selected = this.resultsTarget.querySelector('[aria-selected="true"]')
                    if (selected) {
                        this.commit(selected)
                    }
                }
                break
            case 'Enter':
                {
                    const selected = this.resultsTarget.querySelector('[aria-selected="true"]')
                    if (selected && !this.resultsTarget.hidden) {
                        this.commit(selected)
                        event.preventDefault()
                    }
                }
                break
        }
    }

    addRecord(event) {
        event.preventDefault()
        this.inputTarget.hidden = false
        this.addRecordButtonTarget.hidden = true
        this.fetchResults()
    }

    removeRecord(event) {
        event.preventDefault()
        this.addRecordButtonTarget.hidden = false
        this.selectedLabelSectionTarget.hidden = true
        this.hiddenTarget.value = ''
        this.hiddenPrimaryFieldTarget.value = ''
    }

    onInputBlur() {
        if (this.mouseDown) return
        this.resultsTarget.hidden = true
        this.inputTarget.hidden = true
        this.addRecordButtonTarget.hidden = this.hiddenTarget.value != ''
    }

    onInputFocus() {
        this.fetchResults()
    }

    commit(selected) {
        if (selected.getAttribute('aria-disabled') === 'true') return

        if (selected instanceof HTMLAnchorElement) {
            selected.click()
            this.resultsTarget.hidden = true
            return
        }

        const textValue = selected.textContent.trim()
        const value = selected.getAttribute('data-autocomplete-value') || textValue
        this.inputTarget.value = textValue

        if (this.hasHiddenTarget) {
            this.hiddenTarget.value = value
            this.hiddenPrimaryFieldTarget.value = textValue
        } else {
            this.inputTarget.value = value
        }

        this.hideAndRemoveOptions()
        this.selectedLabelTarget.value = textValue
        this.selectedLabelSectionTarget.hidden = false
        this.inputTarget.hidden = true

        this.element.dispatchEvent(new CustomEvent('autocomplete.change', {
            bubbles: true,
            detail: { value: value, textValue: textValue }
        }))
    }

    onResultsClick(event) {
        if (!(event.target instanceof Element)) return
        const selected = event.target.closest('[role="option"]')
        if (selected) this.commit(selected)
    }

    onResultsMouseDown() {
        this.mouseDown = true
        this.resultsTarget.addEventListener('mouseup', () => (this.mouseDown = false), { once: true })
    }

    onInputChange() {
        this.element.removeAttribute('value')
        this.fetchResults()
    }

    identifyOptions() {
        let id = 0
        for (const el of this.resultsTarget.querySelectorAll('[role="option"]:not([id])')) {
            el.id = `${this.resultsTarget.id}-option-${id++}`
        }
    }

    hideAndRemoveOptions() {
        this.resultsTarget.hidden = true
        this.resultsTarget.innerHTML = null
    }

    fetchResults() {
        const query = this.inputTarget.value.trim()
        if (query.length < this.minLength) {
            this.hideAndRemoveOptions()
            return
        }

        if (!this.srcValue) return
        if (!this.contenttypeValue) return

        const url = new URL(this.srcValue, window.location.href)
        const params = new URLSearchParams(url.search.slice(1))
        params.append('q', query)
        params.append('relatedContentTypeId', this.contenttypeValue)
        url.search = params.toString()

        this.element.dispatchEvent(new CustomEvent('loadstart'))
        this.customerspinnerTarget.hidden = false;
        fetch(url.toString())
            .then(response => response.text())
            .then(html => {
                this.resultsTarget.innerHTML = html
                this.identifyOptions()
                const hasResults = !!this.resultsTarget.querySelector('[role="option"]')
                this.resultsTarget.hidden = !hasResults
                this.element.dispatchEvent(new CustomEvent('load'))
                this.element.dispatchEvent(new CustomEvent('loadend'))
                this.customerspinnerTarget.hidden = true;
            })
            .catch(() => {
                this.element.dispatchEvent(new CustomEvent('error'))
                this.element.dispatchEvent(new CustomEvent('loadend'))
                this.customerspinnerTarget.hidden = true;
            })
    }

    open() {
        if (!this.resultsTarget.hidden) return
        this.resultsTarget.hidden = false
        this.element.setAttribute('aria-expanded', 'true')
        this.element.dispatchEvent(new CustomEvent('toggle', { detail: { input: this.input, results: this.results } }))
    }

    close() {
        if (this.resultsTarget.hidden) return
        this.resultsTarget.hidden = true
        this.inputTarget.removeAttribute('aria-activedescendant')
        this.element.setAttribute('aria-expanded', 'false')
        this.element.dispatchEvent(new CustomEvent('toggle', { detail: { input: this.input, results: this.results } }))
    }

    get minLength() {
        const minLength = this.data.get("min-length")
        if (!minLength) {
            return 0
        }
        return parseInt(minLength, 10)
    }
}