# [Raytha](https://raytha.com)

[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)
[![Build Status](https://github.com/raythahq/raytha/actions/workflows/tests.yml/badge.svg?branch=dev)](https://github.com/raythahq/raytha/actions)

![Raytha Logo](https://user-images.githubusercontent.com/777005/210120197-61101dee-91c7-4628-8fb4-c0d701843704.png)

Raytha is a lightweight, versatile content management system built with .NET. Create any type of website by configuring custom content types and editing HTML templates directly within the platform.

[![Deploy on Railway](https://railway.com/button.svg)](https://railway.com/deploy/raytha-cms?referralCode=RU52It&utm_medium=integration&utm_source=template&utm_campaign=generic)

**[Website](https://raytha.com) · [Documentation](https://docs.raytha.com) · [User Guide](https://raytha.com/user-guide) · [YouTube](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA)**

<details>
<summary>Watch a 40 second demo</summary>

[![Quick demo]()](https://github.com/user-attachments/assets/1723a191-e4ee-4f1f-aa45-612c3ab57405)






</details>

![Screenshot](https://user-images.githubusercontent.com/777005/232172756-4c1ffd34-ea4f-4dbd-bffc-8a7a22ef9e75.png)

## Features

- **Site Pages & Page Builder** — Build landing pages with a drag-and-drop widget system. No code required.
- **Custom Content Types** — Create and customize content structures without code changes
- **Liquid Templates** — Edit templates directly in the platform using the Liquid templating engine
- **Role-Based Access Control** — Design custom roles with granular permissions
- **Headless API** — Auto-generated REST API for all your content
- **Audit Logs** — Complete audit trail of all changes for security and compliance
- **Single Sign-On** — SAML and JWT authentication for admins and users
- **Revision History** — Revert content and templates to any previous version
- **Flexible Storage** — Local, Azure Blob, or S3-compatible file storage

## Quick Start with Docker

Raytha runs out of the box with Docker and PostgreSQL.

### 1. Clone the repository

```bash
git clone https://github.com/RaythaHQ/raytha.git
cd raytha
```

### 2. Configure environment

```bash
cp .env.example .env
```

### 3. Start Raytha

```bash
docker-compose --env-file .env up
```

### 4. Complete setup

Open [http://localhost:5001](http://localhost:5001) and follow the setup wizard.

Raytha automatically applies database migrations on first run.

### Running over HTTP (Development Mode)

Raytha enforces HTTPS by default. For local development or environments without SSL, the `.env.example` includes:

```
ASPNETCORE_ENVIRONMENT=Development
```

For production with SSL/TLS, change this to `Production`.

### Configuration Options

See `.env.example` for all available settings:

- **SMTP** — Email notifications and password reset
- **Cloud Storage** — S3 or Azure Blob integration
- **Raytha Functions** — Serverless function configuration
- **Security** — HTTPS enforcement and URL import controls

## Build from Source

### Requirements

- .NET 10
- PostgreSQL (default) or SQL Server
- SMTP server (optional)

### Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/RaythaHQ/raytha.git
   ```

2. Configure `appsettings.json` with your database connection string and SMTP settings.
   
   > For local SMTP testing, try [Papercut-SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP).

3. Set `Raytha.Web` as the startup project, build, and run. Migrations apply automatically on first start.

## Why Raytha?

### For Content Teams

Intuitive UI with minimal learning curve. Granular permissions, revision history, and comprehensive audit logs give teams confidence and control.

### For Rapid Prototyping

Know HTML? Build blogs, portfolios, galleries, job boards, and more with just basic web skills. Perfect for quickly validating ideas.

### For .NET Developers

A production-ready foundation with user management, RBAC, SSO, audit logging, and cloud storage integrations. Built on [Clean Architecture](https://github.com/jasontaylordev/CleanArchitecture) patterns.

## Community

- [Documentation](https://raytha.com)
- [GitHub Discussions](https://github.com/RaythaHQ/raytha/discussions)
- [YouTube](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA)
- [Twitter](https://twitter.com/raythahq)

## Contributing

Raytha is open-source under the MIT license. We welcome contributions through issues, discussions, and pull requests.

See [CONTRIBUTING.md](https://github.com/RaythaHQ/raytha/blob/main/CONTRIBUTING.md) for guidelines.

---

Created by [Zack Schwartz](https://twitter.com/apexdodge)
