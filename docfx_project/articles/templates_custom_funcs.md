# Custom Raytha liquid functions and filters

The Raytha team has created some custom liquid functions and filters as quality of life enhancements to improve your experience building on top of Raytha. You can also create your own if you are comfortable doing so.

## Functions

Note that every time you call one of these functions, you are making a call to the database, so having many of these run on your template at once may result in performance issues.

### get_content_item_by_id(contentItemId)

You can use this function to make a call to the database to get the details for a single content item by its id.

Usage:

```
{% assign other_related_item = get_content_item_by_id(Target.PublishedContent.related_item.PublishedContent.another_related_item)}

{{ other_related_item.PrimaryField }}
```

### get_content_items(ContentType: developer_name, Filter: odata, OrderBy: odata, PageNumber: int, PageSize: int)

This function will acquire the items for the given `ContentType` (required). The other parameters are optional.

```
{% assign over_five_yrs = get_content_items(ContentType: "posts", Filter: "age > 5", OrderBy: "PrimaryField asc", PageNumber: 1, PageSize: 10)}

{% for item in over_five_yrs.Items %}
    {{ item.PrimaryField }}
{% endfor %}
```

## Filters

### raytha_attachment_url

This filter will take the value and output the rendered url that redirects to the attachment's URL.

Usage:

```
{{ Target.PublishedContent.attachment.Value | raytha_attachment_url }}
```

### organization_time

This filter will take the value and convert it to the timezone that is set in the system's organization settings. This is useful when using variables `CreationTime` and `LastModificationTime`.

Usage:

```
{{{ item.CreationTime | organization_time | date: '%c' }}
```

> Note: The above example also uses the `date` filter which comes with the Fluid parser engine. [You can use strftime for formatting](https://ruby-doc.org/core-3.0.0/Time.html#method-i-strftime).

### groupby: "PublishedContent.developer_name"

This will perform a groupby operation on an array of items, most commonly used with `Target.Items` and key on the PublishedContent.developer_name attribute. The output will be a dictionary of `key` and `items` as demonstrated below.

Usage:

```
{% assign grouped_items = Target.Items | groupby: "PublishedContent.developer_name" %}
{% for grouped_item in grouped_items %}
    {{ grouped_item.key }}
    {% for item in grouped_item.items %}
        {{ item.PublishedContent.developer_name.Value }}
    {% endfor %}
{% endfor %}
```

### json

If you want to "stringify" any of the objects that come out of the rendering engine, you can apply the `json` filter. This might be useful during development if you are looking to see what the attributes are on the objects so that you can pull them out.

Usage:

```
{{ Target | json }}
```

