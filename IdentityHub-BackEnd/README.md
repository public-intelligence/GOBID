# IdentityHub API

ASP.NET Core (.NET 10) API for **person and legal-entity lookups**. GOB-ID uses it to show who a user is. It is a shared registry: its data is not separated by organization.

See the [repository README](../README.md) for the overall architecture.

## Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/person` | List persons |
| GET | `/api/person/{id}` | Person by id |
| GET | `/api/person/cedula/{cedula}` | Person by national id number |
| POST | `/api/person` | Create a person |
| POST | `/api/person/bulk` | Several persons at once |
| POST | `/api/person/search` | Search persons |
| GET | `/api/organization` | List legal entities |
| GET | `/api/organization/{id}` | Legal entity by id |
| GET | `/api/organization/rnc/{identificationNumber}` | Legal entity by tax id (RNC) |
| POST | `/api/organization` | Create a legal entity |

> **The API has no authentication, including the endpoints that create records.** Run it only on an internal network, reachable by the GOB-ID API and never published to the internet.

## Structure

```
IdentityHub/            API: controllers, Program, configuration
IdentityHub.Service/    Services (persons, legal entities, pagination) and mappers
application/            EF Core context, repositories and migrations
domain/                 Models, DTOs and responses
```

## Database

The schema is managed with EF Core migrations, in the `application` project. To create or update a database:

```bash
dotnet tool install --global dotnet-ef        # once
dotnet ef database update --project application --startup-project IdentityHub
```

The API does not apply migrations on startup.

## Run

```bash
dotnet run --project IdentityHub      # http://localhost:5135 (https://localhost:7246)
```

In development, the API serves its OpenAPI document (`/openapi/v1.json`) and an interactive reference with Scalar (`/scalar`).

## Configuration

`IdentityHub/appsettings.json`. The connection string is `ConnectionStrings:DefaultConnection`. Keep it out of the repository: use .NET user secrets locally, and environment variables (`ConnectionStrings__DefaultConnection`) or a secret store in a server.
