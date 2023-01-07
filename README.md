# [Raytha](https://raytha.com)

[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)

![rsz_color_logo_with_background](https://user-images.githubusercontent.com/777005/210120197-61101dee-91c7-4628-8fb4-c0d701843704.png)

Introducing Raytha, the ultimate content management system for .NET developers!

With Raytha, you'll be able to quickly and easily kickstart the development of your .NET applications. Its lightweight, fast, and self-hosted design makes it perfect for rapid development and deployment.

* Create custom content types without any code
* Integrated Template Engine using popular Liquid syntax
* Audit logs, role-based access controls, and single sign-on options (SAML and Json Web Token)
* Supports local file system storage, Azure Blob, and S3-compatible providers
* REST API automatically generated based on content types (coming soon)

[Learn more about Raytha on our website.](https://raytha.com) and the [Raytha Youtube channel](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA).

## Getting Started

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
