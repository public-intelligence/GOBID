# GOB-ID

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

Shared identity and access control platform for government applications. Many applications share a single sign-on and a common administration of users, institutions, roles, permissions and access policies.

- [Overview](#overview)
- [Architecture](#architecture)
- [Repository structure](#repository-structure)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [How an application signs in](#how-an-application-signs-in)
- [Testing](#testing)
- [Deployment notes](#deployment-notes)
- [Contributing](#contributing)
- [Third-party software](#third-party-software)
- [License](#license)
- [Vision and acknowledgments](#vision-and-acknowledgments)

## Overview

- **Single sign-on.** Applications send users to GOB-ID to sign in and receive a token that carries the user's organization, application and permissions.
- **Organizations and applications.**
  - Each institution is an organization.
  - Each application is installed per organization.
  - A user only reaches the applications of the organizations they have access to.
- **Role-based access control (RBAC).** Roles belong to an application installed in an organization. Their permissions are the application's own codes. Roles can also follow a position in the organizational structure (PDS).
- **Attribute-based access control (ABAC).** Policies by IP range or time window, evaluated at sign-in, on each refresh and on every request to the GOB-ID API.
- **Machine-to-machine (M2M).** Credentials, bound to an application installation, that let a service obtain a token without a user.
- **Administration console.** It manages organizations, applications and their credentials, users, roles, permissions, policies, sessions and staff.
- **IdentityHub.** Person and legal-entity lookups, a shared registry that GOB-ID uses to identify people.

## Architecture

```
 Applications (Bot Factory, PDS, ...) ──redirect──▶ GOB-ID web (Angular): sign-in pages and administration console
        │                                               │
        │  POST /api/v1/Auth/token (code exchange)       ▼
        └─────────────────────────────────────▶ GOB-ID API (.NET 8) ───▶ SQL Server: GOB-ID database
                                                        │                (tables and stored procedures, GobID-DB/)
                                                        ├──▶ IdentityHub API (.NET 10) ───▶ SQL Server: IdentityHub database
                                                        ├──▶ PDS (positions and localities, M2M)
                                                        └──▶ Email (Microsoft Graph or SMTP), error tracking (Exceptionless)
```

## Repository structure

| Folder | Description | Stack |
|---|---|---|
| [`GobID-BackEnd/`](GobID-BackEnd/README.md) | GOB-ID API: authentication, authorization, tokens and administration | .NET 8 |
| [`GobID-FrontEnd/`](GobID-FrontEnd/README.md) | Sign-in pages and administration console | Angular 20 |
| [`GobID-DB/`](GobID-DB/README.md) | SQL scripts that create the GOB-ID database structure (no data) | SQL Server |
| [`IdentityHub-BackEnd/`](IdentityHub-BackEnd/README.md) | IdentityHub API: person and legal-entity lookups | .NET 10 |

## Tech stack

| Component | Stack |
|---|---|
| GOB-ID API | ASP.NET Core 8, EF Core and Dapper over SQL Server stored procedures, JWT (HS256), BCrypt, FluentValidation, AutoMapper, Swagger, Serilog, Exceptionless, Microsoft Graph and MailKit for email |
| GOB-ID web | Angular 20, Tailwind CSS 4, `@public-intelligence/ui-core`, Karma and Jasmine |
| IdentityHub API | ASP.NET Core 10, EF Core with migrations, OpenAPI and Scalar |
| Databases | SQL Server: one database for GOB-ID and one for IdentityHub |

## Getting started

### Prerequisites

- .NET 8 SDK (GOB-ID API) and .NET 10 SDK (IdentityHub)
- Node.js 22 and npm
- SQL Server 2019 or later (or Azure SQL Database), and `sqlcmd`
- A GitHub token with `read:packages`, for `@public-intelligence/ui-core`

### Steps

1. **GOB-ID database.**
   - Create an empty database and run [`GobID-DB/install.sql`](GobID-DB/README.md).
   - Then load the minimum data described there: catalogs, the GOB-ID application and its permissions, and a first administrator.
2. **IdentityHub database.** From `IdentityHub-BackEnd/`:

   ```bash
   dotnet ef database update --project application --startup-project IdentityHub
   ```

3. **GOB-ID API.**
   - Set the secrets with .NET user secrets. The keys are in [`GOBID.Api/secrets.example.json`](GobID-BackEnd/GOBID.Api/secrets.example.json).
   - Run `dotnet run --project GOBID.Api` from `GobID-BackEnd/`. It listens on `http://localhost:5053` and serves Swagger at `/swagger`.
4. **IdentityHub API.** Run `dotnet run --project IdentityHub` from `IdentityHub-BackEnd/`. It listens on `http://localhost:5135`.
5. **GOB-ID web.**
   - Configure `~/.npmrc` for GitHub Packages (see the [frontend README](GobID-FrontEnd/README.md)).
   - Run `npm install && npm start` from `GobID-FrontEnd/`. It opens on `http://localhost:4200`.

Each project README has the details of its configuration.

## How an application signs in

1. **Registration.** A GOB-ID administrator registers the application with its `redirect_url`, installs it in one or more organizations and defines its permissions and roles.
2. **Redirect.** The application sends the user to `{GOB-ID web}/app/{applicationId}/login`.
3. **Return.** After the user signs in, GOB-ID sends them back to the `redirect_url` with an authorization `code` (valid for 5 minutes) and the chosen `orgAppId`.
4. **Code exchange.** The application's backend calls `POST /api/v1/Auth/token` with `code`, `organizationApplicationId` (the `orgAppId` it received) and the same `redirectUri`. It receives:
   - an access token (JWT, 1 hour) with the user, organization, application installation, permissions and session;
   - a rotating refresh token, which is renewed at `POST /api/v1/Auth/refresh`.
5. **Validation.** The application validates tokens with its own application secret: the key id is `app:{applicationId}`.

Services without a user use their M2M credential at `POST /api/v1/Auth/app/token`.

## Testing

There is no meaningful automated test suite in this repository yet. GobID-FrontEnd only has Angular's default spec (`cd GobID-FrontEnd && npm test`), and the GOB-ID API and IdentityHub have no test projects.

## Deployment notes

- **Secrets** are empty strings in the repository. In a server, provide them through environment variables or a secret store. Never commit them.
- **IdentityHub has no authentication.** Keep it on an internal network, reachable only by the GOB-ID API.
- **Behind a reverse proxy,** forward the client IP correctly: ABAC policies by IP range depend on it.
- **Every application** that signs in through GOB-ID needs its `redirect_url` registered exactly as it sends it.

## Contributing

- Work in a branch from `development` and open a pull request against it. `master` receives approved versions.
- Database changes go to [`GobID-DB/`](GobID-DB/README.md): one file per table or stored procedure, and keep `install.sql` in dependency order.

## Third-party software

GOB-ID depends on third-party packages that keep their own licenses. They are declared in the `.csproj` files and in `GobID-FrontEnd/package.json`. Most are MIT, Apache 2.0 or BSD. One needs attention when you deploy:

- **AutoMapper 16** (GOB-ID API and IdentityHub). Since version 15, AutoMapper is distributed under a dual license: the RPL-1.5 or a commercial license from Lucky Penny Software, with a free edition for some organizations. Check whether your organization qualifies, or use version 14, the last one under MIT.

## License

Copyright 2026 Jean Antonio Garcia Periche, Melissa Michelle Muñoz Suro and Rainier Christopher Domingo Mallol Cotes.

Licensed under the [Apache License, Version 2.0](LICENSE). See [`NOTICE`](NOTICE).

## Vision and acknowledgments

> *“Modern cross-institutional services require trusted identity, permissions, and discoverability.”*
>
> — ***Rainier Mallol***, *The Operative Republic*

### Vision

GOB-ID follows the software architecture and best practices of the vision paper [*The Operative Republic: Building an Operating System for Dominican Government Services*](https://publicintelligence.ai/govos), by Rainier Mallol.

The paper argues that the next phase of public-sector modernization will be defined less by portals and more by **Digital Public Infrastructure**: shared, interoperable capabilities that many institutions can reuse instead of rebuilding. Its architecture, **Federated Bounded-Context Microservices with Centralized Orchestration (FeBMO)**, lets each institution retain ownership of its systems and data, while shared identity, workflow governance, audit trails and service standards bring them together as one coherent service environment. Each system is designed to create value on its own and to become more powerful when connected to the others.

GOB-ID is the shared identity layer of that architecture. It provides authentication, authorization, role-based (RBAC) and attribute-based (ABAC) access control, and an application catalogue shared across subscribed institutions, so every application relies on one single sign-on instead of building its own. Its model keeps three questions apart, *who you are*, *where you work* and *what you can do*, so identity, institutional relationships and application permissions stay consistent across the whole ecosystem. Alongside it, **IdentityHub** provides person and company lookups, in line with the paper's intent to reuse official sources of truth without replacing the legal responsibilities of the institutions that own them.

**[Read the vision paper →](https://publicintelligence.ai/govos)**

### Acknowledgments

GOB-ID and IdentityHub were designed and built with the participation of:

| Name | Role |
|---|---|
| Rainier Mallol | Software Architecture Design |
| José Ant. Cid Troncoso | Lead Engineer |
| Angel Peña | Engineer |
| Frailin Ogando | Engineer |
| Cristian Jimenez | Engineer |

These acknowledgments only recognize participation in the design and development of GOB-ID and IdentityHub. They do not grant any copyright, ownership, license or other legal right in the software or its name. All rights belong exclusively to the copyright holders listed under [License](#license).

*Built to help move government services from fragmented coordination to structured execution.*
