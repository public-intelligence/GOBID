# GOB-ID API

ASP.NET Core (.NET 8) API for authentication, authorization and tokens: sign-in by redirect, token issuing and refresh, sessions, role-based and attribute-based access control (RBAC and ABAC), machine-to-machine credentials, and the administration of organizations, applications, users, roles, permissions and policies.

See the [repository README](../README.md) for the overall architecture.

## Structure

```
GOBID.Api/              Controllers, middlewares (session and policy enforcement), Startup, configuration
GOBID.Aplication/       DTOs, interfaces, validations, mapping and view models (folder name kept as is)
GOBID.Domain/           Entities
GOBID.Infrastructure/   Services, EF Core context and Dapper repositories, security (token signing keys), email templates
Scripts/                Ad hoc SQL scripts
docs/                   Internal notes on local secrets
```

Data access combines EF Core and Dapper. Most reads and writes run through the stored procedures in [`../GobID-DB`](../GobID-DB/README.md).

## Run

```bash
dotnet run --project GOBID.Api      # http://localhost:5053 (https://localhost:7053)
```

The API serves Swagger at `/swagger`. Routes are versioned: `/api/v1/...`.

## Configuration

`GOBID.Api/appsettings.json` holds the non-secret settings:
- the issuer and audience of the tokens;
- the allowed client origins;
- the IdentityHub, PDS and document service URLs;
- the email settings;
- the session lifetimes.

Secrets are always empty strings in the repository. Their keys are listed, without values, in [`GOBID.Api/secrets.example.json`](GOBID.Api/secrets.example.json):
- `connectionStrings:default`;
- `Jwt:Key` and `Jwt:AppKey`;
- `Exceptionless:ApiKey`;
- `GraphMailSettings:ClientSecret`;
- `PdsApiSettings:M2MClientSecret`;
- `DmsApiSettings:clientSecret`.

Locally, set them with .NET user secrets:

```bash
dotnet user-secrets set --project GOBID.Api "connectionStrings:default" "<connection string>"
```

In a server, use environment variables (`connectionStrings__default`) or a secret store.
