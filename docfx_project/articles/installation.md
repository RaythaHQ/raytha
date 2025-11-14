# Developer Quick Start

A priority of Raytha is to keep the technology footprint small for getting up and running as quickly as possible. However, you do need the minimum requirements listed below:

* .NET 10
* Postgres (default) or SQL Server Express
* SMTP (optional, for reset password, etc)

The steps to run Raytha locally are that of any typical .NET application.

1. Clone this repository into your local directory.
```
git clone https://github.com/RaythaHQ/raytha.git
```
2. Ensure your appsettings.config has a valid database connection string and SMTP credentials. If you do not have access to an SMTP server for local development, [check out Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP). Super convenient.

3. Make sure Raytha.Web is set as the Default Project. Compile and run. Raytha will apply the database migrations to your database on first run.