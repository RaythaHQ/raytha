# [Raytha](https://raytha.com)

[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)

![rsz_color_logo_with_background](https://user-images.githubusercontent.com/777005/210120197-61101dee-91c7-4628-8fb4-c0d701843704.png)

Raytha is a versatile and lightweight general purpose content management system. Create any type of website by easily configuring custom content types and HTML templates that can be directly edited within the platform.

<strong>🌐 [Raytha.com](https://raytha.com) 📹 [Intro Video](https://www.youtube.com/watch?v=k6VrvqH8PBY) 📖 [User Guide](https://raytha.com/user-guide) 👨‍💻 [Developer Docs](https://docs.raytha.com)</strong>

[![Quick silent demo](https://user-images.githubusercontent.com/777005/232171420-35104db8-4c19-46b5-bbe0-87e4b19316fe.mp4)](https://user-images.githubusercontent.com/777005/232171420-35104db8-4c19-46b5-bbe0-87e4b19316fe.mp4)

## Feature Highlights

* <strong>Content Types:</strong> Effortlessly create and customize content types, no code changes required.
* <strong>Role Based Access Control:</strong> Design customized roles with specific access permissions and assign them to individual users.
* <strong>Built In Template Engine</strong>: Easily modify templates within the platform using the popular Liquid templating engine.
* <strong>Headless Mode:</strong> Instant access to a REST API based on your content, automatically generated for you.
* <strong>Audit Logs:</strong> Built-in audit trail that tracks all edits made for enhanced security.
* <strong>Single Sign On:</strong> SAML and Json Web Token authentication are available out of the box for both administrators and public users.
* <strong>Revisions:</strong> Revisions of all content and templates are stored, enabling you to go back and revert to a previous version if needed.
* <strong>File Storage:</strong> The platform supports local file storage by default, but cloud storage options such as Azure Blob or S3-Compatible can be enabled if desired.

👀 [Learn more about Raytha on our website.](https://raytha.com) and the [Raytha Youtube channel](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA) 📺.

## Why Raytha?

### Content Managers Love It

📝 Content managers love Raytha not only for its minimal learning curve, simplicity, and self-evident UI, but also for its ease of granting different permission levels and roles to various admins, as well as its ability to revert back to previous versions of articles effortlessly. Raytha's audit logs functionality is also highly valued by content administrators, allowing them to keep track of all changes made to the content, and ensuring greater transparency and accountability across the organization.

### Rapid Prototyping

👨‍💻 Know HTML? Raytha makes it easy for project managers and tech-savvy individuals to create custom websites in a snap. You can go the distance with just a basic understanding of HTML. From blogs and corporate sites to photo and video galleries, event websites, job boards, and beyond, Raytha can help you rapidly prototype a new concept.

### Starter Kit for Developers

🚀 If you're a .NET developer looking to jumpstart your web application development, Raytha's boilerplate template can save you valuable time. Raytha offers a host of features including user management, role-based access control (RBAC), single sign-on, and audit logs functionality, as well as interfaces for file storage with Azure Blob and S3-compatible providers. Its architecture is built on the well-known [CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) template, which means that any .NET developer can easily familiarize themselves with the backend functionality. With Raytha, you can hit the ground running and get your web application up and running in no time.

## Developer Quick Start

A priority of Raytha is to keep the technology footprint small for getting up and running as quickly as possible. However, you do need the minimum requirements listed below:

* .NET 6+
* npm for compiling javascript
* SQL Server Express
* SMTP
* Visual Studio 2022 or VS Code. All tutorials will assume Visual Studio.

The steps to run Raytha locally are that of any typical .NET application.

1. Clone this repository into your local directory.
```
git clone https://github.com/RaythaHQ/raytha.git
```
2. Ensure your appsettings.config has a valid database connection string and SMTP credentials. If you do not have access to an SMTP server for local development, [check out Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP). Super convenient.

3. Make sure Raytha.Web is set as the Default Project. Open the `Package Manager Console` and run Entity Framework database migrations:

```
dotnet ef database update --project .\src\Raytha.Infrastructure --startup-project .\src\Raytha.Web
```

Alternatively you can create your database manually and then run the `FreshCreateOnLatestVersion.sql` script in the /db directory.

4. Compile and run. If everything works, the first screen you see will be an Initial Setup screen. Complete the details and you are good to go.

**Optional**

By default, it will use local file system and use the directory specified in appsettings.config. You can adjust the appsettings.config if you have access to Azure Blob or an S3 compatible storage.

## Community support

For general help using Raytha, please refer to official documentation on [raytha.com](https://raytha.com), the [Raytha Youtube channel](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA), or post your questions and feedback in [Github Discussions](https://github.com/RaythaHQ/raytha/discussions). Keep up to date on Raytha news by following @raythahq on [Twitter](https://twitter.com/raythahq) and [Instagram](https://instagram.com/raythahq).

## Contributing to Raytha

Raytha is open-source software, freely distributable under the terms of an MIT license.

We welcome contributions in the form of feature requests, bug reports, pull requests, or thoughtful discussions in the GitHub discussions and issue tracker. Please see the [CONTRIBUTING](https://github.com/RaythaHQ/raytha/blob/main/CONTRIBUTING.md) document for more information.

Raytha was founded by Zack Schwartz [@apexdodge](https://twitter.com/apexdodge)
