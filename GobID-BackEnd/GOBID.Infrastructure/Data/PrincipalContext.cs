using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Data
{
    public partial class PrincipalContext : DbContext
    {
        public PrincipalContext()
        {
        }

        public PrincipalContext(string CnString): base(GetOptions(CnString))
        {
        }

        public static DbContextOptions GetOptions(string CnString)
        {
            return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), CnString).Options;
        }

        public PrincipalContext(DbContextOptions<PrincipalContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AccessType> AccessTypes { get; set; }

        public virtual DbSet<Domain.Entities.Application> Applications { get; set; }

        public virtual DbSet<ApplicationAccess> ApplicationAccesses { get; set; }

        public virtual DbSet<ChangeLog> ChangeLogs { get; set; }

        public virtual DbSet<Claim> Claims { get; set; }

        public virtual DbSet<LoginLog> LoginLogs { get; set; }

        public virtual DbSet<Organization> Organizations { get; set; }

        public virtual DbSet<OrganizationApplication> OrganizationApplications { get; set; }

        public virtual DbSet<OrganizationContact> OrganizationContacts { get; set; }

        public virtual DbSet<OrganizationUiCustomization> OrganizationUiCustomizations { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<RoleClaim> RoleClaims { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<UserApplication> UserApplications { get; set; }

        public virtual DbSet<UserOrganization> UserOrganizations { get; set; }

        public virtual DbSet<UserRole> UserRoles { get; set; }

        public virtual DbSet<AuthorizationCode> AuthorizationCodes { get; set; }

        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        public virtual DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        public virtual DbSet<UserSession> UserSessions { get; set; }

        public virtual DbSet<ApplicationCredential> ApplicationCredentials { get; set; }

        public virtual DbSet<ApplicationRole> ApplicationRoles { get; set; }

        public virtual DbSet<ApplicationRoleClaim> ApplicationRoleClaims { get; set; }

        public virtual DbSet<PolicyType> PolicyTypes { get; set; }

        public virtual DbSet<Policy> Policies { get; set; }

        public virtual DbSet<PolicyCondition> PolicyConditions { get; set; }

        public virtual DbSet<UserApplicationPolicy> UserApplicationPolicies { get; set; }

        public virtual DbSet<PolicyEvaluationLog> PolicyEvaluationLogs { get; set; }

        public virtual DbSet<Personnel> Personnels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelsConfiguration.Configuration(modelBuilder); 
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}