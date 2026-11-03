using IdentityHub.Domain.Models;
using IdentityHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace IdentityHub.Application.Data;

public class IdentityHubDbContext : DbContext
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();

    public IdentityHubDbContext(DbContextOptions<IdentityHubDbContext> options)
      : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AI");

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("persons");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdentificationNumber).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.FirstName, e.LastName });

            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.IdentificationNumber).HasMaxLength(13);
            entity.Property(e => e.PhoneNumber).HasMaxLength(17);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.EmailConfirmed).HasDefaultValue(false);
            entity.Property(e => e.PhoneNumberConfirmed).HasDefaultValue(false);
            entity.Property(e => e.IsExternal).HasDefaultValue(false);
            entity.Property(e => e.Status)
                  .HasDefaultValue(EnumStatus.Active);
            entity.Property(e => e.Gender);
            entity.Property(e => e.ProfilePhotoId);
            });



        modelBuilder.Entity<LegalEntity>(entity =>
        {
            entity.ToTable("legalentities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LegalName).IsUnique();
            entity.HasIndex(e => e.SocialName).IsUnique();
            entity.HasIndex(e => e.IdentificationNumber).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()").ValueGeneratedOnAdd();
            entity.Property(e => e.LegalName).HasMaxLength(100);
            entity.Property(e => e.SocialName).HasMaxLength(100);
            entity.Property(e => e.IdentificationNumber).HasMaxLength(13);
            entity.Property(e => e.PhoneNumber).HasMaxLength(17);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Status)
                  .HasDefaultValue(EnumStatus.Active);
        });

    }

}

