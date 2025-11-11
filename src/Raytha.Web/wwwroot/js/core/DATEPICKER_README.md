# Datepicker Module

A reusable vanilla JavaScript datepicker solution using [Flatpickr](https://flatpickr.js.org/).

## Features

- **Auto-initialization**: Automatically initializes all date inputs with `[data-datepicker]` attribute
- **Lightweight**: Uses Flatpickr (~20KB minified) loaded from CDN
- **Customizable**: Supports configuration via data attributes
- **Keyboard accessible**: Full keyboard navigation support
- **Mobile friendly**: Works well on touch devices
- **No dependencies**: Pure vanilla JavaScript (no jQuery)

## Basic Usage

### Quick Start

Simply add the `data-datepicker` attribute to any input element:

```html
<input type="text" data-datepicker="" name="startDate" placeholder="mm/dd/yyyy">
```

The datepicker will be automatically initialized when the page loads.

### With Initial Value

```html
<input type="text" data-datepicker="" value="01/15/2025">
```

### Configuration via Data Attributes

You can customize the datepicker behavior using data attributes:

#### Date Format
```html
<input type="text" data-datepicker data-date-format="Y-m-d" placeholder="YYYY-MM-DD">
```

#### Date Range Restrictions
```html
<!-- Only allow dates from today onwards -->
<input type="text" data-datepicker data-min-date="today">

<!-- Only allow dates up to 30 days in the future -->
<input type="text" data-datepicker data-max-date="+30">
```

#### Date + Time Picker
```html
<input type="text" data-datepicker data-enable-time="true">
```

#### Range Picker (select start and end date)
```html
<input type="text" data-datepicker data-mode="range">
```

#### Multiple Date Selection
```html
<input type="text" data-datepicker data-mode="multiple">
```

## Programmatic Usage

### Initialize a Specific Datepicker

```javascript
import { initDatepicker } from '/js/core/datepicker.js';

const input = document.querySelector('#myDateInput');
const instance = initDatepicker(input, {
  minDate: 'today',
  maxDate: new Date().fp_incr(30), // 30 days from now
});
```

### Get Datepicker Instance

```javascript
import { getDatepickerInstance } from '/js/core/datepicker.js';

const input = document.querySelector('[data-datepicker]');
const instance = getDatepickerInstance(input);

// Access Flatpickr methods
console.log(instance.selectedDates);
```

### Set Date Programmatically

```javascript
import { setDatepickerDate } from '/js/core/datepicker.js';

const input = document.querySelector('#startDate');
setDatepickerDate(input, '2025-01-15');
```

### Clear Date

```javascript
import { clearDatepickerDate } from '/js/core/datepicker.js';

const input = document.querySelector('#startDate');
clearDatepickerDate(input);
```

### Destroy Datepicker

```javascript
import { destroyDatepicker, getDatepickerInstance } from '/js/core/datepicker.js';

const input = document.querySelector('[data-datepicker]');
const instance = getDatepickerInstance(input);
destroyDatepicker(instance);
```

## Common Date Formats

- `m/d/Y` - 01/15/2025 (default)
- `Y-m-d` - 2025-01-15 (ISO format)
- `d.m.Y` - 15.01.2025 (European)
- `F j, Y` - January 15, 2025 (long format)
- `M j, Y` - Jan 15, 2025 (short month)

## Integration with Razor Pages

### In a Razor Partial

```html
<div class="mb-3">
    <label class="form-label">Start Date</label>
    <div class="input-group">
        <span class="input-group-text">
            <svg class="icon icon-xs text-gray-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd"></path>
            </svg> 
        </span>
        <input data-datepicker="" 
               class="form-control datepicker-input" 
               name="startDate" 
               value="@Model.StartDate"
               type="text">
    </div>
</div>
```

### With Model Binding

```html
<input data-datepicker="" 
       class="form-control" 
       asp-for="Form.FieldValues[i].Value"
       type="text">
```

## Troubleshooting

### Datepicker Not Working

If the datepicker doesn't initialize:

1. Check browser console for errors
2. Verify the `data-datepicker` attribute is present
3. Ensure the input is in the DOM when the page loads
4. For dynamically added inputs, manually call `initDatepicker()`

### Styling Issues

The module loads Flatpickr's default CSS from CDN. If you need custom styling:

1. Create a custom CSS file to override Flatpickr styles
2. Or download Flatpickr and customize the SCSS source

### Dynamic Content

For date inputs added after page load (e.g., via AJAX):

```javascript
import { initDatepicker } from '/js/core/datepicker.js';

// After adding new input to DOM
const newInput = document.querySelector('#dynamicDateInput');
initDatepicker(newInput);
```

## API Reference

### Functions

- `initDatepicker(input, config)` - Initialize datepicker on a single input
- `initAllDatepickers()` - Initialize all datepickers on page
- `destroyDatepicker(instance)` - Destroy a datepicker instance
- `getDatepickerInstance(input)` - Get Flatpickr instance from input
- `setDatepickerDate(input, date)` - Set date programmatically
- `clearDatepickerDate(input)` - Clear date
- `loadFlatpickr()` - Load Flatpickr library (Promise)
- `isFlatpickrLoaded()` - Check if Flatpickr is loaded

### Data Attributes

- `data-datepicker` - Enable datepicker (required)
- `data-date-format` - Date format string
- `data-min-date` - Minimum selectable date
- `data-max-date` - Maximum selectable date
- `data-enable-time` - Enable time picker
- `data-no-calendar` - Hide calendar (time only)
- `data-mode` - Selection mode: `single`, `multiple`, `range`

## Further Reading

For advanced Flatpickr configuration, see the official documentation:
https://flatpickr.js.org/options/

