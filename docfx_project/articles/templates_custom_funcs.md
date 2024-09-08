# Custom Raytha liquid functions and filters

The Raytha team has created some custom liquid functions and filters as quality of life enhancements to improve your experience building on top of Raytha. You can also create your own if you are comfortable doing so.

## Functions

Note that every time you call one of these functions, you are making a call to the database, so having many of these run on your template at once may result in performance issues.

### get_content_item_by_id(contentItemId)

Retrieve details for a single content item by its id.

Example usage:

```
{% assign other_related_item = get_content_item_by_id(Target.PublishedContent.related_item.PublishedContent.another_related_item)}

{{ other_related_item.PrimaryField }}
```

### get_content_items(ContentType='developer_name', Filter='odata', OrderBy='odata', PageNumber=1, PageSize=25)

Retrieve the items for the given `ContentType` (required). The other parameters are optional. You can filter, sort, and paginate on these items by following the OData syntax as described [in OData with Templates](/articles/templates_odata.html).

Example usage:

```
{% assign filter = "contains(user_guide,'" | append: Target.PrimaryField | append: "')" %}
{% assign items = get_content_items(ContentType='posts', Filter=filter, OrderBy="order_to_appear_in_user_guide asc", PageSize=25) %}

{% if items.TotalCount > 0 %}
<div id="articles">
    <h2>Table of Contents</h2>   
    <ol>
    {% for item in items.Items %}
        <li><a href="{{ PathBase }}/{{ item.RoutePath }}" target="_blank">{{ item.PrimaryField }}</a></li>
    {% endfor %}
    </ol>
</div>
{% endif %}
```

### get_content_type_by_developer_name(contentTypeDeveloperName)

Retrieve details and field definitions of a content type by passing in the content type developer name.

Example usage:

```
{% assign contentType = get_content_type_by_developer_name('posts') %}
{% assign categoriesField = contentType.ContentTypeFields | where: "DeveloperName", "categories" | first %}
{% for choice in categoriesField.Choices %}
	<a href="{{ PathBase }}/{{ category.DeveloperName }}">{{ choice.Label }}</a>
{% endfor %}
```

### get_main_menu() and get_menu('developerName')

Retrieve the Navigation Menu that is set to the `default`.

Example usage:

```
{% assign menu = get_main_menu() %}
<ul class="navbar-nav me-auto mb-2 mb-lg-0">
{% for menuItem in menu.MenuItems %}
    {% assign menuLabelDownCase = menuItem.Label | downcase %}
    <li class="nav-item">
        <a class="{{ menuItem.CssClassName }} {% if Target.RoutePath == menuItem.Label or ContentType.DeveloperName == menuLabelDownCase %} active {% endif %}" href="{{ menuItem.Url }}">
        {{ menuItem.Label }}
        </a>
    </li>
{% endfor %}
</ul>

{% assign footerMenu = get_menu('footer') %}
...

```

The function builds a tree out of the navigation menu so that you can reach child items as well. The model of a menu item object is the following:

```
string Id
string Label
string Url
bool IsDisabled
bool OpenInNewTab
string? CssClassName
int Ordinal
bool IsFirstItem
bool IsLastItem
IEnumerable<NavigationMenuItem_RenderModel> MenuItems
```

## Filters

### attachment_redirect_url

Output a url that is relative to the current website such as yourdomain.com/raytha/media-items/objectkey/{key}.

Example usage:

```
{{ Target.PublishedContent.attachment.Value | attachment_redirect_url }}
```

> Note: This rendered url does a 302 redirect to the file on the file storage provider and generates a pre-signed or SaS url in the process. This allows your storage bucket to remain completely private. However, if you have a lot of attachment urls on your page, you are increasing the number of requests your website must serve to redirect to these files.

### attachment_public_url

Output the url directly to the file on your file storage provider.

Example usage:

```
{{ Target.PublishedContent.attachment.Value | attachment_public_url }}
```

> Note: These urls will be the direct file on your file storage provider, but does not generate a presigned URL request or SaS url. Therefore, to use this filter, your storage bucket must be set to allow anonymous read-access on blobs.

### raytha_attachment_url (deprecated)

Renamed to `attachment_redirect_url`. To be removed in v1.0.6


### organization_time

Take the value and convert it to the timezone that is set in the system's organization settings. This is useful when using variables `CreationTime` and `LastModificationTime`.

Example usage:

```
{{{ item.CreationTime | organization_time | date: '%c' }}
```

> Note: The above example also uses the `date` filter which comes with the Fluid parser engine. [You can use strftime for formatting](https://ruby-doc.org/core-3.0.0/Time.html#method-i-strftime).

### groupby: "PublishedContent.developer_name"

Perform a groupby operation on an array of items, most commonly used with `Target.Items` and key on the PublishedContent.developer_name attribute. The output will be a dictionary of `key` and `items` as demonstrated below.

Example usage:

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

"Stringify" any of the objects that come out of the rendering engine. This might be useful during development if you are looking to see what the attributes are on the objects so that you can pull them out.

Example usage:

```
{{ Target | json }}
```

