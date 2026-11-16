-- GOB-ID database: structure only (tables, indexes, keys and stored procedures). It loads no data.
-- Run it in SQLCMD mode, from this folder, against an empty database:
--   sqlcmd -S <server> -d <database> -U <user> -i install.sql
-- Tables are listed in foreign-key dependency order.

:on error exit

-- Session options the objects were created with (required by the filtered indexes and kept by the procedures).
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Tables
:r tables/access_types.sql
:r tables/applications.sql
:r tables/change_logs.sql
:r tables/claims.sql
:r tables/organizations.sql
:r tables/policy_types.sql
:r tables/security_audit_logs.sql
:r tables/user_applications.sql
:r tables/application_credentials.sql
:r tables/organization_applications.sql
:r tables/organization_contacts.sql
:r tables/organization_ui_customizations.sql
:r tables/personnel.sql
:r tables/policies.sql
:r tables/application_access.sql
:r tables/login_logs.sql
:r tables/policy_conditions.sql
:r tables/roles.sql
:r tables/user_application_policies.sql
:r tables/users.sql
:r tables/application_roles.sql
:r tables/authorization_codes.sql
:r tables/policy_evaluation_logs.sql
:r tables/role_claims.sql
:r tables/user_organizations.sql
:r tables/user_roles.sql
:r tables/user_sessions.sql
:r tables/application_role_claims.sql
:r tables/refresh_tokens.sql

-- Stored procedures
:r procedures/confirm_user_email.sql
:r procedures/create_login_log.sql
:r procedures/delete_access_type.sql
:r procedures/delete_app_access.sql
:r procedures/delete_application.sql
:r procedures/delete_claim.sql
:r procedures/delete_org_app_map.sql
:r procedures/delete_organization.sql
:r procedures/delete_organization_contact.sql
:r procedures/delete_role.sql
:r procedures/delete_role_claim.sql
:r procedures/delete_user_account.sql
:r procedures/delete_user_application.sql
:r procedures/delete_user_role.sql
:r procedures/read_access_types.sql
:r procedures/read_app_accesses.sql
:r procedures/read_applications.sql
:r procedures/read_authenticate_user.sql
:r procedures/read_claims.sql
:r procedures/read_login_logs.sql
:r procedures/read_org_app_maps.sql
:r procedures/read_organization_contacts.sql
:r procedures/read_organizations.sql
:r procedures/read_personnel_by_organization_application.sql
:r procedures/read_random_string.sql
:r procedures/read_role_claims.sql
:r procedures/read_roles.sql
:r procedures/read_successful_logins_by_month.sql
:r procedures/read_summary_counts.sql
:r procedures/read_user_accounts.sql
:r procedures/read_user_applications.sql
:r procedures/read_user_effective_claims.sql
:r procedures/read_user_org_apps.sql
:r procedures/read_user_org_context.sql
:r procedures/read_user_organizations.sql
:r procedures/read_user_reset_token.sql
:r procedures/read_user_roles.sql
:r procedures/update_user_password.sql
:r procedures/update_user_reset_token.sql
:r procedures/update_user_status.sql
:r procedures/upsert_access_type.sql
:r procedures/upsert_app_access.sql
:r procedures/upsert_application.sql
:r procedures/upsert_claim.sql
:r procedures/upsert_generate_string.sql
:r procedures/upsert_org_app_map.sql
:r procedures/upsert_organization.sql
:r procedures/upsert_organization_contact.sql
:r procedures/upsert_role.sql
:r procedures/upsert_role_claim.sql
:r procedures/upsert_user_account.sql
:r procedures/upsert_user_application.sql
:r procedures/upsert_user_organization.sql
:r procedures/upsert_user_role.sql
