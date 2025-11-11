# Datepicker Quick Start Guide

## TL;DR

Add `data-datepicker=""` to any text input, and it becomes a datepicker. That's it!

```html
<input data-datepicker="" type="text" name="myDate">
```

## Common Patterns

### Standard Date Field
```html
<input data-datepicker="" 
       class="form-control" 
       name="startDate" 
       placeholder="mm/dd/yyyy">
```

### With Icon (Bootstrap Input Group)
```html
<div class="input-group">
    <span class="input-group-text">
        <svg class="icon icon-xs"><!-- calendar icon --></svg>
    </span>
    <input data-datepicker="" class="form-control" type="text">
</div>
```

### Date Range (From/To)
```html
<input data-datepicker data-mode="range" type="text">
```

### Only Future Dates
```html
<input data-datepicker data-min-date="today" type="text">
```

### Custom Format (YYYY-MM-DD)
```html
<input data-datepicker data-date-format="Y-m-d" type="text">
```

### With Time
```html
<input data-datepicker data-enable-time="true" type="text">
```

## Dynamic Content

For inputs added after page load:

```javascript
import { initDatepicker } from '/js/core/datepicker.js';

// After adding input to DOM
const input = document.querySelector('#newDateInput');
initDatepicker(input);
```

## Get/Set Date Programmatically

```javascript
import { getDatepickerInstance, setDatepickerDate } from '/js/core/datepicker.js';

const input = document.querySelector('#myDate');

// Get selected date
const instance = getDatepickerInstance(input);
console.log(instance.selectedDates);

// Set date
setDatepickerDate(input, '01/15/2025');
```

## Common Configurations

| Attribute | Example | Description |
|-----------|---------|-------------|
| `data-datepicker` | `data-datepicker=""` | Enable datepicker (required) |
| `data-date-format` | `data-date-format="Y-m-d"` | Date format |
| `data-min-date` | `data-min-date="today"` | Minimum date |
| `data-max-date` | `data-max-date="+30"` | Maximum date |
| `data-enable-time` | `data-enable-time="true"` | Enable time picker |
| `data-mode` | `data-mode="range"` | Selection mode |

## Need More?

See [DATEPICKER_README.md](./DATEPICKER_README.md) for complete documentation.

