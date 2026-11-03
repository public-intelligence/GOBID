using GOBID.Domain.Entities;
using Microsoft.EntityFrameworkCore;



namespace GOBID.Infrastructure.Data.Configuration
{
    public class ModelsConfiguration
    {

        public static void Configuration(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<AccessType>(entity =>
            {
                entity.ToTable("access_types");

                entity.Property(e => e.AccessTypeId).HasColumnName("access_type_id");
                entity.Property(e => e.AccessTypeDescription)
                    .HasMaxLength(500)
                    .HasColumnName("access_type_description");
                entity.Property(e => e.AccessTypeName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("access_type_name");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            });

            modelBuilder.Entity<Domain.Entities.Application>(entity =>
            {
                entity.ToTable("applications");

                entity.HasIndex(e => e.Code, "UQ_applications_code").IsUnique();

                entity.Property(e => e.ApplicationId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("application_id");
                entity.Property(e => e.ApplicationDescription)
                    .HasMaxLength(500)
                    .HasColumnName("application_description");
                entity.Property(e => e.ApplicationName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("application_name");
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("code");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.IsPublic)
                    .HasDefaultValue(false)
                    .HasColumnName("is_public");
                entity.Property(e => e.RedirectUrl)
                    .HasMaxLength(255)
                    .HasColumnName("redirect_url");
                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("row_version");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            });

            modelBuilder.Entity<ApplicationAccess>(entity =>
            {
                entity.ToTable("application_access");

                entity.Property(e => e.ApplicationAccessId).HasColumnName("application_access_id");
                entity.Property(e => e.AccessTypeId).HasColumnName("access_type_id");
                entity.Property(e => e.ConfigurationJson).HasColumnName("configuration_json");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.AccessType).WithMany(p => p.ApplicationAccesses)
                    .HasForeignKey(d => d.AccessTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_access_access_types");

                entity.HasOne(d => d.OrganizationApplication).WithMany(p => p.ApplicationAccesses)
                    .HasForeignKey(d => d.OrganizationApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_access_org_apps");
            });

            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.ToTable("change_logs");

                entity.Property(e => e.ChangeLogId).HasColumnName("change_log_id");
                entity.Property(e => e.AfterUpdateEntityJson).HasColumnName("after_update_entity_json");
                entity.Property(e => e.BeforeUpdateEntityJson).HasColumnName("before_update_entity_json");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.DidRollback).HasColumnName("did_rollback");
                entity.Property(e => e.EntityId).HasColumnName("entity_id");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.IsDeleting).HasColumnName("is_deleting");
                entity.Property(e => e.LogName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("log_name");
                entity.Property(e => e.Observation)
                    .HasMaxLength(255)
                    .HasColumnName("observation");
                entity.Property(e => e.RollbackDate)
                    .HasPrecision(3)
                    .HasColumnName("rollback_date");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.ToTable("claims");

                entity.HasIndex(e => new { e.ApplicationId, e.Code }, "IX_claims_app_id_code");

                entity.HasIndex(e => new { e.ApplicationId, e.Code }, "UQ_claims_application_code").IsUnique();

                entity.Property(e => e.ClaimId).HasColumnName("claim_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.ClaimDescription)
                    .HasMaxLength(500)
                    .HasColumnName("claim_description");
                entity.Property(e => e.ClaimName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("claim_name");
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("code");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.IsAutomatic)
                    .HasDefaultValue(false)
                    .HasColumnName("is_automatic");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.Application).WithMany(p => p.Claims)
                    .HasForeignKey(d => d.ApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_claims_applications");
            });

            modelBuilder.Entity<LoginLog>(entity =>
            {
                entity.ToTable("login_logs");

                entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt }, "IX_login_logs_org_date")
                    .IsDescending(false, true)
                    .HasFilter("([is_active]=(1))");

                entity.Property(e => e.LoginLogId).HasColumnName("login_log_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.HeaderValues)
                    .HasMaxLength(255)
                    .HasColumnName("header_values");
                entity.Property(e => e.Ip)
                    .HasMaxLength(45)
                    .HasColumnName("ip");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.LoginInfo)
                    .HasMaxLength(500)
                    .HasColumnName("login_info");
                entity.Property(e => e.LoginSuccessful).HasColumnName("login_successful");
                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100)
                    .HasColumnName("correlation_id");

                entity.HasOne(d => d.OrganizationApplication).WithMany(p => p.LoginLogs)
                    .HasForeignKey(d => d.OrganizationApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_login_logs_org_apps");

                entity.HasOne(d => d.Organization).WithMany(p => p.LoginLogs)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_login_logs_organizations");
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organizations");

                entity.HasIndex(e => e.IsActive, "IX_organizations_active");

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Acronym)
                    .HasMaxLength(10)
                    .HasColumnName("acronym");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.Email)
                    .HasMaxLength(320)
                    .HasColumnName("email");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.OrganizationAddress)
                    .HasMaxLength(255)
                    .HasColumnName("organization_address");
                entity.Property(e => e.OrganizationDescription)
                    .HasMaxLength(255)
                    .HasColumnName("organization_description");
                entity.Property(e => e.OrganizationName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("organization_name");
                entity.Property(e => e.Phone)
                    .HasMaxLength(20)
                    .HasColumnName("phone");
                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("row_version");
                entity.Property(e => e.Slogan)
                    .HasMaxLength(255)
                    .HasColumnName("slogan");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.Website)
                    .HasMaxLength(200)
                    .HasColumnName("website");
            });

            modelBuilder.Entity<OrganizationApplication>(entity =>
            {
                entity.ToTable("organization_applications");

                entity.HasIndex(e => e.ApplicationId, "IX_org_apps_app_id");

                entity.HasIndex(e => e.OrganizationId, "IX_org_apps_org_id");

                entity.HasIndex(e => new { e.OrganizationId, e.ApplicationId }, "UQ_org_apps_orgid_appid").IsUnique();

                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("row_version");

                entity.HasOne(d => d.Application).WithMany(p => p.OrganizationApplications)
                    .HasForeignKey(d => d.ApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_org_apps_applications");

                entity.HasOne(d => d.Organization).WithMany(p => p.OrganizationApplications)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_org_apps_organizations");
            });

            modelBuilder.Entity<OrganizationContact>(entity =>
            {
                entity.HasKey(e => e.ContactId);

                entity.ToTable("organization_contacts");

                entity.Property(e => e.ContactId).HasColumnName("contact_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.Email)
                    .HasMaxLength(320)
                    .HasColumnName("email");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.OrganizationContactName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("organization_contact_name");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Phone)
                    .HasMaxLength(20)
                    .HasColumnName("phone");
                entity.Property(e => e.Position)
                    .HasMaxLength(100)
                    .HasColumnName("position");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.Organization).WithMany(p => p.OrganizationContacts)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_org_contacts_organizations");
            });

            modelBuilder.Entity<OrganizationUiCustomization>(entity =>
            {
                entity.HasKey(e => e.UiCustomId);

                entity.ToTable("organization_ui_customizations");

                entity.Property(e => e.UiCustomId).HasColumnName("ui_custom_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CustomizationName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("customization_name");
                entity.Property(e => e.CustomizationValue)
                    .HasMaxLength(255)
                    .HasColumnName("customization_value");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.Organization).WithMany(p => p.OrganizationUiCustomizations)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_org_ui_organizations");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");

                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.PositionId).HasColumnName("position_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.IsSystemRole)
                    .HasDefaultValue(false)
                    .HasColumnName("is_system_role");
                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.RoleDescription)
                    .HasMaxLength(500)
                    .HasColumnName("role_description");
                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("role_name");
                entity.Property(e => e.SetAutomaticClaims).HasColumnName("set_automatic_claims");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.OrganizationApplication).WithMany(p => p.Roles)
                    .HasForeignKey(d => d.OrganizationApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_roles_org_apps");
            });

            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("role_claims");

                entity.Property(e => e.RoleClaimId).HasColumnName("role_claim_id");
                entity.Property(e => e.ClaimId).HasColumnName("claim_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.Claim).WithMany(p => p.RoleClaims)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_role_claims_claims");

                entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_role_claims_roles");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.UserId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("user_id");
                entity.Property(e => e.AdmissionDate).HasColumnName("admission_date");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.DischargeDate).HasColumnName("discharge_date");
                entity.Property(e => e.Email)
                    .HasMaxLength(320)
                    .HasColumnName("email");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.EmailConfirmed)
                    .HasDefaultValue(false)
                    .HasColumnName("email_confirmed");
                entity.Property(e => e.EmailConfirmationToken)
                    .HasMaxLength(2048)
                    .HasColumnName("email_confirmation_token");
                entity.Property(e => e.IsExternal)
                    .HasDefaultValue(false)
                    .HasColumnName("is_external");
                entity.Property(e => e.MustChangePassword).HasColumnName("must_change_password");
                entity.Property(e => e.PasswordResetToken)
                    .HasMaxLength(255)
                    .HasColumnName("password_reset_token");
                entity.Property(e => e.PersonnelId).HasColumnName("personnel_id");
                entity.Property(e => e.PersonPositionId).HasColumnName("person_position_id");
                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("row_version");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.UserPassword)
                    .HasMaxLength(128)
                    .HasColumnName("user_password");
            });

            modelBuilder.Entity<UserApplication>(entity =>
            {
                entity.ToTable("user_applications");

                entity.HasIndex(e => new { e.UserId, e.OrganizationApplicationId }, "UQ_usr_apps_usrid_appid").IsUnique();

                entity.Property(e => e.UserApplicationId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("user_application_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.InternalAppUserCode)
                    .HasMaxLength(50)
                    .HasColumnName("internal_app_user_code");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.IsApplicationAdmin).HasColumnName("is_application_admin");
                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("row_version");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.UserApplicationDescription)
                    .HasMaxLength(500)
                    .HasColumnName("user_application_description");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.OrganizationApplication).WithMany(p => p.UserApplications)
                    .HasForeignKey(d => d.OrganizationApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_apps_org_apps");

                entity.HasOne(d => d.User).WithMany(p => p.UserApplications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_apps_users");
            });

            modelBuilder.Entity<UserOrganization>(entity =>
            {
                entity.ToTable("user_organizations");

                entity.HasKey(e => e.UserOrganizationsId);

                entity.HasIndex(e => new { e.UserId, e.OrganizationId }, "UQ_user_organizations_user_org").IsUnique();

                entity.Property(e => e.UserOrganizationsId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("user_organizations_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Email)
                    .HasMaxLength(320)
                    .HasColumnName("email");
                entity.Property(e => e.Position)
                    .HasMaxLength(100)
                    .HasColumnName("position");
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_organizations_users");

                entity.HasOne(d => d.Organization).WithMany()
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_organizations_organizations");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.UserApplicationId).HasColumnName("user_application_id");
                entity.Property(e => e.UserRoleDescription)
                    .HasMaxLength(500)
                    .HasColumnName("user_role_description");

                entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_roles_roles");

                entity.HasOne(d => d.UserApplication).WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_roles_user_apps");
            });

            modelBuilder.Entity<AuthorizationCode>(entity =>
            {
                entity.ToTable("authorization_codes");

                entity.HasKey(e => e.CodeId);

                entity.Property(e => e.CodeId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("code_id");
                entity.Property(e => e.CodeHash)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnName("code_hash");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.RedirectUri)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("redirect_uri");
                entity.Property(e => e.AuthData)
                    .IsRequired()
                    .HasColumnName("auth_data");
                entity.Property(e => e.IsUsed)
                    .HasDefaultValue(false)
                    .HasColumnName("is_used");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_auth_codes_users");
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");

                entity.HasKey(e => e.TokenId);

                entity.Property(e => e.TokenId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("token_id");
                entity.Property(e => e.TokenHash)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnName("token_hash");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.AuthData)
                    .IsRequired()
                    .HasColumnName("auth_data");
                entity.Property(e => e.IsRevoked)
                    .HasDefaultValue(false)
                    .HasColumnName("is_revoked");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.ReplacedBy).HasColumnName("replaced_by");
                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100)
                    .HasColumnName("correlation_id");
                entity.Property(e => e.SessionId).HasColumnName("session_id");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_refresh_tokens_users");

                entity.HasOne(d => d.Session).WithMany(p => p.RefreshTokens)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_refresh_tokens_sessions");
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.ToTable("user_sessions");

                entity.HasKey(e => e.SessionId);

                entity.Property(e => e.SessionId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("session_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.Ip)
                    .HasMaxLength(45)
                    .HasColumnName("ip");
                entity.Property(e => e.UserAgent)
                    .HasMaxLength(512)
                    .HasColumnName("user_agent");
                entity.Property(e => e.IsTrusted)
                    .HasDefaultValue(false)
                    .HasColumnName("is_trusted");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.LastActivityAt)
                    .HasPrecision(3)
                    .HasColumnName("last_activity_at");
                entity.Property(e => e.AbsoluteExpiresAt)
                    .HasPrecision(3)
                    .HasColumnName("absolute_expires_at");
                entity.Property(e => e.RevokedAt)
                    .HasPrecision(3)
                    .HasColumnName("revoked_at");
                entity.Property(e => e.RevokedByIp)
                    .HasMaxLength(45)
                    .HasColumnName("revoked_by_ip");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_sessions_users");

                entity.HasIndex(e => new { e.UserId, e.ApplicationId, e.IsActive }, "IX_user_sessions_user_active");
                entity.HasIndex(e => e.AbsoluteExpiresAt, "IX_user_sessions_expires")
                    .HasFilter("([is_active]=(1))");
            });

            modelBuilder.Entity<SecurityAuditLog>(entity =>
            {
                entity.ToTable("security_audit_logs");

                entity.HasKey(e => e.AuditLogId);

                entity.Property(e => e.AuditLogId)
                    .UseIdentityColumn()
                    .HasColumnName("audit_log_id");
                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("event_type");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100)
                    .HasColumnName("correlation_id");
                entity.Property(e => e.Ip)
                    .HasMaxLength(45)
                    .HasColumnName("ip");
                entity.Property(e => e.UserAgent)
                    .HasMaxLength(512)
                    .HasColumnName("user_agent");
                entity.Property(e => e.Details).HasColumnName("details");
                entity.Property(e => e.Success).HasColumnName("success");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");

                entity.HasIndex(e => new { e.ApplicationId, e.EventType, e.Ip, e.CreatedAt }, "IX_audit_rate_limit");
            });

            // ── M2M: ApplicationCredential ──────────────────────────────────────
            modelBuilder.Entity<ApplicationCredential>(entity =>
            {
                entity.ToTable("application_credentials");

                entity.HasKey(e => e.AppCredentialId);

                entity.Property(e => e.AppCredentialId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("app_credential_id");
                entity.Property(e => e.ApplicationId).HasColumnName("application_id");
                entity.Property(e => e.ClientSecretHash)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("client_secret_hash");
                entity.Property(e => e.Description)
                    .HasMaxLength(200)
                    .HasColumnName("description");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.ExpiresAt)
                    .HasPrecision(3)
                    .HasColumnName("expires_at");
                entity.Property(e => e.LastUsedAt)
                    .HasPrecision(3)
                    .HasColumnName("last_used_at");
                entity.Property(e => e.RevokedAt)
                    .HasPrecision(3)
                    .HasColumnName("revoked_at");
                entity.Property(e => e.RevokedBy).HasColumnName("revoked_by");
                entity.Property(e => e.RotatedByCredentialId).HasColumnName("rotated_by_credential_id");

                entity.HasOne(d => d.Application).WithMany()
                    .HasForeignKey(d => d.ApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_creds_applications");

                entity.HasOne(d => d.RotatedByCredential).WithMany()
                    .HasForeignKey(d => d.RotatedByCredentialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_creds_rotated_by");

                entity.HasIndex(e => new { e.ApplicationId, e.IsActive }, "IX_app_credentials_app_active")
                    .HasFilter("([is_active]=(1))");
            });

            // ── M2M: ApplicationRole ─────────────────────────────────────────────
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("application_roles");

                entity.HasKey(e => e.ApplicationRoleId);

                entity.Property(e => e.ApplicationRoleId)
                    .UseIdentityColumn()
                    .HasColumnName("application_role_id");
                entity.Property(e => e.AppCredentialId).HasColumnName("app_credential_id");
                entity.Property(e => e.OrganizationApplicationId).HasColumnName("organization_application_id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.Description)
                    .HasMaxLength(500)
                    .HasColumnName("description");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasIndex(e => new { e.AppCredentialId, e.OrganizationApplicationId, e.RoleId }, "UQ_app_roles")
                    .IsUnique();

                entity.HasOne(d => d.ApplicationCredential).WithMany()
                    .HasForeignKey(d => d.AppCredentialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_roles_app_creds");

                entity.HasOne(d => d.OrganizationApplication).WithMany()
                    .HasForeignKey(d => d.OrganizationApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_roles_org_apps");

                entity.HasOne(d => d.Role).WithMany()
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_roles_roles");
            });

            // ── Actualizar RefreshToken: user_id nullable + app_credential_id ───
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");
                entity.Property(e => e.AppCredentialId)
                    .HasColumnName("app_credential_id");

                entity.HasOne(d => d.AppCredential).WithMany(p => p.RefreshTokens)
                    .HasForeignKey(d => d.AppCredentialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_refresh_tokens_app_creds");
            });

            // ── Actualizar UserSession: user_id nullable + app_credential_id ────
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.Property(e => e.AppCredentialId)
                    .HasColumnName("app_credential_id");

                entity.HasOne(d => d.AppCredential).WithMany(p => p.UserSessions)
                    .HasForeignKey(d => d.AppCredentialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_user_sessions_app_creds");
            });

            // ── M2M: ApplicationRoleClaim ─────────────────────────────────────
            modelBuilder.Entity<ApplicationRoleClaim>(entity =>
            {
                entity.ToTable("application_role_claims");

                entity.HasKey(e => e.ApplicationRoleClaimId);

                entity.Property(e => e.ApplicationRoleClaimId)
                    .UseIdentityColumn()
                    .HasColumnName("application_role_claim_id");
                entity.Property(e => e.ApplicationRoleId).HasColumnName("application_role_id");
                entity.Property(e => e.ClaimId).HasColumnName("claim_id");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasIndex(e => new { e.ApplicationRoleId, e.ClaimId }, "UQ_app_role_claims")
                    .IsUnique();

                entity.HasOne(d => d.ApplicationRole).WithMany(p => p.ApplicationRoleClaims)
                    .HasForeignKey(d => d.ApplicationRoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_role_claims_app_roles");

                entity.HasOne(d => d.Claim).WithMany()
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_app_role_claims_claims");
            });

            modelBuilder.Entity<PolicyType>(entity =>
            {
                entity.ToTable("policy_types");
                entity.HasKey(e => e.PolicyTypeId);
                entity.Property(e => e.PolicyTypeId).HasColumnName("policy_type_id").UseIdentityColumn();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasPrecision(3).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            });

            modelBuilder.Entity<Policy>(entity =>
            {
                entity.ToTable("policies");
                entity.HasKey(e => e.PolicyId);
                entity.Property(e => e.PolicyId).HasDefaultValueSql("(newsequentialid())").HasColumnName("policy_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.PolicyTypeId).HasColumnName("policy_type_id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150).HasColumnName("name");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.Effect).IsRequired().HasMaxLength(10).HasColumnName("effect");
                entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasPrecision(3).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt).HasPrecision(3).HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken().HasColumnName("row_version");

                entity.HasOne(d => d.Organization).WithMany()
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Policy_Organization");

                entity.HasOne(d => d.PolicyType).WithMany(p => p.Policies)
                    .HasForeignKey(d => d.PolicyTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Policy_PolicyType");
            });

            modelBuilder.Entity<PolicyCondition>(entity =>
            {
                entity.ToTable("policy_conditions");
                entity.HasKey(e => e.PolicyConditionId);
                entity.Property(e => e.PolicyConditionId).HasColumnName("policy_condition_id").UseIdentityColumn();
                entity.Property(e => e.PolicyId).HasColumnName("policy_id");
                entity.Property(e => e.ConditionKey).IsRequired().HasMaxLength(100).HasColumnName("condition_key");
                entity.Property(e => e.ConditionValue).IsRequired().HasMaxLength(255).HasColumnName("condition_value");

                entity.HasOne(d => d.Policy).WithMany(p => p.PolicyConditions)
                    .HasForeignKey(d => d.PolicyId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_PolicyCondition_Policy");
            });

            modelBuilder.Entity<UserApplicationPolicy>(entity =>
            {
                entity.ToTable("user_application_policies");
                entity.HasKey(e => e.UserApplicationPolicyId);
                entity.Property(e => e.UserApplicationPolicyId).HasDefaultValueSql("(newsequentialid())").HasColumnName("user_application_policy_id");
                entity.Property(e => e.UserApplicationId).HasColumnName("user_application_id");
                entity.Property(e => e.PolicyId).HasColumnName("policy_id");
                entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasPrecision(3).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt).HasPrecision(3).HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.UserApplication).WithMany()
                    .HasForeignKey(d => d.UserApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserApplicationPolicy_UserApplication");

                entity.HasOne(d => d.Policy).WithMany(p => p.UserApplicationPolicies)
                    .HasForeignKey(d => d.PolicyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserApplicationPolicy_Policy");
            });

            modelBuilder.Entity<PolicyEvaluationLog>(entity =>
            {
                entity.ToTable("policy_evaluation_logs");
                entity.HasKey(e => e.PolicyEvaluationLogId);
                entity.Property(e => e.PolicyEvaluationLogId).HasDefaultValueSql("(newsequentialid())").HasColumnName("policy_evaluation_log_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PolicyId).HasColumnName("policy_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.EvaluationResult).IsRequired().HasMaxLength(10).HasColumnName("evaluation_result");
                entity.Property(e => e.FailureReason).HasMaxLength(500).HasColumnName("failure_reason");
                entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).HasMaxLength(500).HasColumnName("user_agent");
                entity.Property(e => e.EvaluatedAt).HasPrecision(3).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("evaluated_at");

                entity.HasOne(d => d.User).WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PolicyEvaluationLog_User");

                entity.HasOne(d => d.Policy).WithMany()
                    .HasForeignKey(d => d.PolicyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PolicyEvaluationLog_Policy");

                entity.HasOne(d => d.Organization).WithMany()
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PolicyEvaluationLog_Organization");
            });

            modelBuilder.Entity<Personnel>(entity =>
            {
                entity.ToTable("personnel");
                entity.HasKey(e => e.PersonnelId);
                entity.Property(e => e.PersonnelId)
                    .HasDefaultValueSql("(newsequentialid())")
                    .HasColumnName("personnel_id");
                entity.Property(e => e.PersonId).HasColumnName("person_id");
                entity.Property(e => e.PersonId).HasColumnName("person_id");
                entity.Property(e => e.PositionId).HasColumnName("position_id");
                entity.Property(e => e.LocalityId).HasColumnName("locality_id");
                entity.Property(e => e.PositionNominateId).HasColumnName("position_nominate_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true)
                    .HasColumnName("is_active");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(3)
                    .HasDefaultValueSql("(sysutcdatetime())")
                    .HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.UpdatedAt)
                    .HasPrecision(3)
                    .HasColumnName("updated_at");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasIndex(e => e.OrganizationId, "IX_personnel_org_id")
                    .HasFilter("([is_active]=(1))");
                entity.HasIndex(e => e.PositionId, "IX_personnel_position_id")
                    .HasFilter("([is_active]=(1))");
                entity.HasIndex(e => new { e.PositionId, e.OrganizationId }, "IX_personnel_position_org")
                    .HasFilter("([is_active]=(1))");

                entity.HasOne(d => d.Organization).WithMany()
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_personnel_organizations");
            });

        }

    }
}
