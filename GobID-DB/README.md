# GOB-ID database

SQL Server scripts that create the **structure** of the GOB-ID database: 29 tables (with their indexes, defaults and foreign keys) and the 54 stored procedures the API calls. **They load no data.**

```
GobID-DB/
  install.sql     Creates everything, in dependency order (SQLCMD script)
  tables/         One file per table: columns, constraints, foreign keys and indexes
  procedures/     One file per stored procedure
```

## Requirements

- SQL Server 2019 or later, or Azure SQL Database. The scripts were validated on SQL Server 2025.
- `sqlcmd`, or SQL Server Management Studio / Azure Data Studio with **SQLCMD mode** turned on: `install.sql` includes the other files with `:r`.

## Create the database

1. Create an empty database:

   ```sql
   CREATE DATABASE [GOBID];
   ```

2. Run `install.sql` **from this folder**, because the `:r` paths are relative to it:

   ```bash
   cd GobID-DB
   sqlcmd -S <server> -d GOBID -U <user> -i install.sql
   ```

`install.sql` stops at the first error (`:on error exit`).

It also sets the session options the objects were created with (`ANSI_NULLS`, `QUOTED_IDENTIFIER` and related). Filtered indexes require them, and the stored procedures keep them. If you run a single file by hand, set them first.

## Database user for the API

The API connects with the connection string `connectionStrings:default` (see the [backend README](../GobID-BackEnd/README.md)). Its user needs to read and write every table and to execute the stored procedures, for example through `db_datareader`, `db_datawriter` and `GRANT EXECUTE`. The scripts create no logins or users.

## Minimum data before the first sign-in

The database starts empty, so the API cannot sign anyone in until some data is loaded. These scripts do not include it; whoever installs GOB-ID must load:

- **Reference catalogs:**
  - `access_types`, the access mechanisms of an application;
  - `policy_types`, the ABAC policy types that the code evaluates: `IP_RANGE` and `TIME_WINDOW`.
- **The GOB-ID application** in `applications`, with `application_id` = `DF7FF72A-5321-F111-B7F2-6045BDB142AD`. The procedure `upsert_organization` uses this identifier: when an organization is created, it installs this application for it and creates a local administrator role with the application's automatic permissions. The application's `redirect_url` must point to the GOB-ID frontend.
- **The permissions of the GOB-ID application** in `claims`: the codes the API checks, such as `super_admin`, `admin_local`, `list_user` and `edit_user`. Mark with `is_automatic = 1` the ones a local administrator receives.
- **A first organization and administrator:**
  - the organization (`organizations`) and its installation of the GOB-ID application (`organization_applications`);
  - a role with the administration permissions (`roles`, `role_claims`);
  - a user (`users`, with a BCrypt password hash);
  - that user's access and role (`user_organizations`, `user_applications`, `user_roles`).

Every other application that signs in through GOB-ID is registered afterwards from the administration console.

## Regenerating the scripts

The scripts were extracted from a working GOB-ID database with [SqlPackage](https://learn.microsoft.com/sql/tools/sqlpackage/sqlpackage), structure only:

```bash
sqlpackage /Action:Extract /SourceConnectionString:"<connection string>" /TargetFile:src \
  /p:ExtractTarget=SchemaObjectType /p:ExtractAllTableData=false \
  /p:ExtractApplicationScopedObjectsOnly=true /p:IgnorePermissions=true \
  /p:IgnoreUserLoginMappings=true /p:IgnoreExtendedProperties=true
```

Copy `src/dbo/Tables` into `tables/` and `src/dbo/StoredProcedures` into `procedures/`. Leave `src/Security` out, because it holds the source server's logins and users. Then keep the table order in `install.sql` consistent with the foreign keys: each table after the tables it references.
