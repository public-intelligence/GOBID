using GOBID.Application.DTOs.ApplicationRoleClaim;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.ApplicationRoleClaim;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Services;

public class ApplicationRoleClaimService : IApplicationRoleClaimService
{
    private readonly PrincipalContext _context;

    public ApplicationRoleClaimService(PrincipalContext context)
    {
        _context = context;
    }

    public async Task<Response<List<ApplicationRoleClaimVm>>> AddClaimsAsync(CreateApplicationRoleClaimDto dto, Guid createdBy)
    {
        try
        {
            var roleExists = await _context.ApplicationRoles
                .AnyAsync(ar => ar.ApplicationRoleId == dto.ApplicationRoleId && ar.IsActive);

            if (!roleExists)
                return new Response<List<ApplicationRoleClaimVm>>("Rol de aplicaci\u00f3n no encontrado o inactivo", false);

            var existingLinks = await _context.ApplicationRoleClaims
                .Where(arc => arc.ApplicationRoleId == dto.ApplicationRoleId && dto.ClaimIds.Contains(arc.ClaimId))
                .ToListAsync();

            var now = DateTime.UtcNow;
            var added = new List<ApplicationRoleClaim>();

            foreach (var claimId in dto.ClaimIds.Distinct())
            {
                var existing = existingLinks.FirstOrDefault(e => e.ClaimId == claimId);
                if (existing != null)
                {
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        existing.UpdatedAt = now;
                        existing.UpdatedBy = createdBy;
                        added.Add(existing);
                    }
                    continue;
                }

                var link = new ApplicationRoleClaim
                {
                    ApplicationRoleId = dto.ApplicationRoleId,
                    ClaimId = claimId,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = createdBy
                };
                _context.ApplicationRoleClaims.Add(link);
                added.Add(link);
            }

            await _context.SaveChangesAsync();

            var result = await _context.ApplicationRoleClaims
                .AsNoTracking()
                .Include(arc => arc.Claim)
                .Where(arc => arc.ApplicationRoleId == dto.ApplicationRoleId && dto.ClaimIds.Contains(arc.ClaimId))
                .Select(arc => MapToVm(arc))
                .ToListAsync();

            return new Response<List<ApplicationRoleClaimVm>>(result, "Claims vinculados correctamente");
        }
        catch (Exception ex)
        {
            return new Response<List<ApplicationRoleClaimVm>>($"Error al vincular claims: {ex.Message}", false);
        }
    }

    public async Task<Response<List<ApplicationRoleClaimVm>>> ListByApplicationRoleAsync(int applicationRoleId)
    {
        try
        {
            var items = await _context.ApplicationRoleClaims
                .AsNoTracking()
                .Include(arc => arc.Claim)
                .Where(arc => arc.ApplicationRoleId == applicationRoleId && arc.IsActive)
                .Select(arc => MapToVm(arc))
                .ToListAsync();

            return new Response<List<ApplicationRoleClaimVm>>(items, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<ApplicationRoleClaimVm>>($"Error al listar claims: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RemoveAsync(int applicationRoleClaimId, Guid updatedBy)
    {
        try
        {
            var link = await _context.ApplicationRoleClaims
                .FirstOrDefaultAsync(arc => arc.ApplicationRoleClaimId == applicationRoleClaimId);

            if (link == null)
                return new Response<string>("V\u00ednculo no encontrado", false);

            link.IsActive = false;
            link.UpdatedAt = DateTime.UtcNow;
            link.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            return new Response<string>("V\u00ednculo eliminado correctamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al eliminar el v\u00ednculo: {ex.Message}", false);
        }
    }

    private static ApplicationRoleClaimVm MapToVm(ApplicationRoleClaim arc) => new()
    {
        ApplicationRoleClaimId = arc.ApplicationRoleClaimId,
        ApplicationRoleId = arc.ApplicationRoleId,
        ClaimId = arc.ClaimId,
        ClaimName = arc.Claim?.ClaimName,
        ClaimCode = arc.Claim?.Code,
        IsActive = arc.IsActive,
        CreatedAt = arc.CreatedAt,
        UpdatedAt = arc.UpdatedAt
    };
}
