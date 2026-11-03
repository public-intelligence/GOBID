using GOBID.Application.DTOs.ApplicationCredential;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace GOBID.Infrastructure.Services;

public class ApplicationCredentialService : IApplicationCredentialService
{
    private readonly PrincipalContext _context;
    private readonly IConfiguration _config;

    public ApplicationCredentialService(PrincipalContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<Response<AppCredentialCreatedDto>> CreateAsync(Guid applicationId, CreateAppCredentialDto dto, Guid createdBy)
    {
        try
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.IsActive);

            if (application == null)
                return new Response<AppCredentialCreatedDto>("Aplicación no encontrada o inactiva", false);

            var orgAppExists = await _context.OrganizationApplications
                .AnyAsync(oa => oa.OrganizationApplicationId == dto.OrganizationApplicationId
                             && oa.ApplicationId == applicationId
                             && oa.IsActive);

            if (!orgAppExists)
                return new Response<AppCredentialCreatedDto>("OrganizationApplication no encontrada o no pertenece a la aplicación indicada", false);

            var rawSecretBytes = RandomNumberGenerator.GetBytes(32);
            var rawSecret = Convert.ToBase64String(rawSecretBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var secretHash = BCrypt.Net.BCrypt.HashPassword(rawSecret, workFactor: 12);

            var credential = new ApplicationCredential
            {
                AppCredentialId = Guid.NewGuid(),
                ApplicationId = applicationId,
                ClientSecretHash = secretHash,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ExpiresAt = (dto.ExpiryDays.HasValue && dto.ExpiryDays.Value != 0)
                    ? DateTime.UtcNow.AddDays(dto.ExpiryDays.Value)
                    : null
            };

            _context.ApplicationCredentials.Add(credential);

            var now = DateTime.UtcNow;

            var role = new Role
            {
                RoleName = "RoleM2M",
                RoleDescription = $"Rol M2M generado automáticamente para credencial: {dto.Description ?? credential.AppCredentialId.ToString()}",
                SetAutomaticClaims = false,
                OrganizationApplicationId = dto.OrganizationApplicationId,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = createdBy,
                IsSystemRole = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var appRole = new ApplicationRole
            {
                AppCredentialId = credential.AppCredentialId,
                OrganizationApplicationId = dto.OrganizationApplicationId,
                RoleId = role.RoleId,
                Description = $"RoleM2M — {dto.Description}",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = createdBy
            };
            _context.ApplicationRoles.Add(appRole);
            await _context.SaveChangesAsync();

            int createdApplicationRoleId = appRole.ApplicationRoleId;

            foreach (var claimId in dto.ClaimIds.Distinct())
            {
                _context.ApplicationRoleClaims.Add(new ApplicationRoleClaim
                {
                    ApplicationRoleId = appRole.ApplicationRoleId,
                    ClaimId = claimId,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = createdBy
                });
            }

            await _context.SaveChangesAsync();

            return new Response<AppCredentialCreatedDto>
            {
                Succeeded = true,
                Message = "Credencial creada. Guarda el client_secret ahora — no se puede recuperar después.",
                Data = new AppCredentialCreatedDto
                {
                    AppCredentialId = credential.AppCredentialId,
                    ApplicationId = credential.ApplicationId,
                    Description = credential.Description,
                    IsActive = credential.IsActive,
                    CreatedAt = credential.CreatedAt,
                    ExpiresAt = credential.ExpiresAt,
                    ClientSecret = rawSecret,
                    ApplicationRoleId = (int?)createdApplicationRoleId
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<AppCredentialCreatedDto>($"Error al crear la credencial: {ex.Message}", false);
        }
    }

    public async Task<Response<List<AppCredentialDto>>> ListByApplicationAsync(Guid applicationId)
    {
        try
        {
            var credentials = await _context.ApplicationCredentials
                .AsNoTracking()
                .Where(c => c.ApplicationId == applicationId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new AppCredentialDto
                {
                    AppCredentialId = c.AppCredentialId,
                    ApplicationId = c.ApplicationId,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    ExpiresAt = c.ExpiresAt,
                    LastUsedAt = c.LastUsedAt,
                    RevokedAt = c.RevokedAt,
                    RotatedByCredentialId = c.RotatedByCredentialId
                })
                .ToListAsync();

            return new Response<List<AppCredentialDto>>
            {
                Succeeded = true,
                Data = credentials
            };
        }
        catch (Exception ex)
        {
            return new Response<List<AppCredentialDto>>($"Error al listar credenciales: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RevokeAsync(Guid applicationId, Guid appCredentialId, Guid revokedBy)
    {
        try
        {
            var credential = await _context.ApplicationCredentials
                .FirstOrDefaultAsync(c => c.AppCredentialId == appCredentialId && c.ApplicationId == applicationId);

            if (credential == null)
                return new Response<string>("Credencial no encontrada", false);

            if (!credential.IsActive)
                return new Response<string>("La credencial ya estaba revocada", false);

            credential.IsActive = false;
            credential.RevokedAt = DateTime.UtcNow;
            credential.RevokedBy = revokedBy;

            await _context.SaveChangesAsync();

            return new Response<string>("Credencial revocada exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al revocar la credencial: {ex.Message}", false);
        }
    }
}
