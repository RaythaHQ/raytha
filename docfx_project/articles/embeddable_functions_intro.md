# Embed Functions Inside Raytha with Custom Code

## Introduction to Raytha Functions

Raytha Functions are a way to write custom code in the administrator portal of Raytha to add functionality to your application. Use cases for this include but are not limited to:

* Create your own custom api endpoints
* Accept form submissions and save response
* Send an email out of the platform
* Make an external HTTP call and return and/or save result
* Webhooks trigger functionality
* [Coming Soon] Timer trigger functionality

In a way, it is effectively writing "code behind" directly into the platform.

<img class="inline-img" src="../images/functions_sidebar.png" />

## Writing code behind

When you write code for a Raytha Function there are few items you need to be aware of.

* <strong>Write your code in javascript.</strong> The code is compiled and run using Microsoft's ClearScript engine which works with V8 implementation of javascript. This gives you access to basic engine javascript. 
* <strong>Triggers.</strong> Functions are triggered by some action. You select the trigger type when you create the function.
* <strong>Built-in objects.</strong> The javascript by itself would not be of much use if you did not have layer of access to the database and other core functionality.

## Triggers

Raytha Functions are triggered by some action. You select the trigger type when you create the function.

<img class="inline-img" src="../images/functions_triggertype.png" />

Triggers come with boilerplate code that the ClearScript engine runs when necessary. Click the link below to learn how to write code for each trigger type.

* [Http Request](/articles/embeddable_functions_httprequest.html)
* [Webhooks](/articles/embeddable_functions_webhooks.html)
* [Coming Soon] Timer

## Built-in Objects

To provide valuable functionality to you while you write Raytha Functions, the following objects are included in the ClearScript engine that you can call.

| Object                             | Notes              |
| ------------------------------     | ------------------|
| API_V1                             | API to Raytha's Application Layer |
| CurrentOrganization                | Object with access to the current org settings like name, timezone, etc. |
| CurrentUser                        | Information about the current logged in user |
| Emailer                            | Send an email from the platform |
| HttpClient                         | Make an external API call |

[Learn how to make use of these objects](/articles/embeddable_functions_builtinobjects.html).

## Settings and Environment Variables

By default, Raytha sets the following three settings in appsettings.json:

| Env Var                            | Default Value |
| ------------------------------     | ------|
| RAYTHA_FUNCTIONS_MAX_ACTIVE        | 5     |
| RAYTHA_FUNCTIONS_TIMEOUT           | 10000 |
| RAYTHA_FUNCTIONS_QUEUE_TIMEOUT     | 10000 |

You can override these by setting environment variables, or modifying appsettings.json, whichever your preference.

If you set `RAYTHA_FUNCTIONS_MAX_ACTIVE` to 0, then you will disable Raytha Functions entirely and the menu option will not even appear in the administrator backend. This setting sets the maximum number of concurrent functions that can run at a time. For example, if you have this setting set to 5, and there are currently 5 functions running simultaneously, and a 6th call is started, the 6th will wait in a queue until a spot becomes available.

`RAYTHA_FUNCTIONS_TIMEOUT` and `RAYTHA_FUNCTIONS_QUEUE_TIMEOUT` are specified in ms, so those default values are 10 seconds.

`RAYTHA_FUNCTIONS_TIMEOUT` specifies the length of time the function has to execute the code. If it takes longer than that, then the function is terminated. `RAYTHA_FUNCTIONS_QUEUE_TIMEOUT` is the length of time a call will wait for a spot to open up if the max active functions is exceeded.

These settings are to ensure safety with your application, helping prevent bad code, or a malicious actor from taking too many compute resources.

