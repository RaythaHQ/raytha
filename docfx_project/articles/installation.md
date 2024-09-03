# Developer Quick Start

A priority of Raytha is to keep the technology footprint small for getting up and running as quickly as possible. However, you do need the minimum requirements listed below:

* .NET 8+
* npm for compiling javascript
* SQL Server Express
* [SMTP](/articles/smtp.md)
* Visual Studio 2022 or VS Code. All tutorials will assume Visual Studio.

The steps to run Raytha locally are that of any typical .NET application.

1. Clone this repository into your local directory.
```
git clone https://github.com/RaythaHQ/raytha.git
```
2. Ensure your appsettings.config has a valid database connection string and SMTP credentials. If you do not have access to an SMTP server for local development, [check out Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP). Super convenient.

3. Make sure Raytha.Web is set as the Default Project. Compile and run. Raytha will apply the database migrations to your database on first run. 

If you prefer to run migrations manually, you can set `APPLY_PENDING_MIGRATIONS` to `false` in appsettings.json or env variables and then open the `Package Manager Console` and run Entity Framework database migrations:

```
dotnet ef database update --project .\src\Raytha.Infrastructure --startup-project .\src\Raytha.Web
```

or run the `FreshCreateOnLatestVersion.sql` script in the /db directory.

4. Compile and run. If everything works, the first screen you see will be an Initial Setup screen. Complete the details and you are good to go.