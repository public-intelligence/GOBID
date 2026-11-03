using GOBID.Application.DTOs.ApplicationRole;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.ApplicationRole;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Services;

public class ApplicationRoleService : IApplicationRoleService
{
    private readonly PrincipalContext _context;

    public ApplicationRoleService(PrincipalContext context)
    {
        _context = context;
    }

    public async Task<Response<ApplicationRoleVm>> GetByIdAsync(int applicationRoleId)
    {
        try
        {
            var entity = await _context.ApplicationRoles
                .AsNoTracking()
                .Include(ar => ar.Role)
                .FirstOrDefaultAsync(ar => ar.ApplicationRoleId == applicationRoleId);

            if (entity == null)
                return new Response<ApplicationRoleVm>("Rol de aplicación no encontrado", false);

            return new Response<ApplicationRoleVm>(MapToVm(entity), "OK");
        }
        catch (Exception ex)
        {
            return new Response<ApplicationRoleVm>($"Error al obtener el rol de aplicación: {ex.Message}", false);
        }
    }

    public async Task<Response<List<ApplicationRoleVm>>> ListAsync(
        int? pageNumber,
        int? pageSize,
        Guid? appCredentialId,
        int? organizationApplicationId,
        int? roleId,
        bool? isActive)
    {
        if (pageNumber == null || pageNumber <= 0) pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100) pageSize = 50;

        try
        {
            var query = _context.ApplicationRoles
                .AsNoTracking()
                .Include(ar => ar.Role)
                .AsQueryable();

            if (appCredentialId.HasValue)
                query = query.Where(ar => ar.AppCredentialId == appCredentialId.Value);

            if (organizationApplicationId.HasValue)
                query = query.Where(ar => ar.OrganizationApplicationId == organizationApplicationId.Value);

            if (roleId.HasValue)
                query = query.Where(ar => ar.RoleId == roleId.Value);

            if (isActive.HasValue)
                query = query.Where(ar => ar.IsActive == isActive.Value);

            var items = await query
                .OrderBy(ar => ar.CreatedAt)
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();

            return new Response<List<ApplicationRoleVm>>(items.Select(MapToVm).ToList(), "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<ApplicationRoleVm>>($"Error al listar roles de aplicación: {ex.Message}", false);
        }
    }

    public async Task<Response<ApplicationRoleVm>> UpsertAsync(ApplicationRoleUpsertDto dto, Guid updatedBy)
    {
        try
        {
            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId && r.IsActive);
            if (!roleExists)
                return new Response<ApplicationRoleVm>("El rol especificado no existe o está inactivo", false);

            var orgAppExists = await _context.OrganizationApplications
                .AnyAsync(oa =>
                    oa.OrganizationApplicationId == dto.OrganizationApplicationId &&
                    oa.IsActive);
            if (!orgAppExists)
                return new Response<ApplicationRoleVm>("La organización-aplicación no existe", false);

            var credentialExists = await _context.ApplicationCredentials
                .AnyAsync(c => c.AppCredentialId == dto.AppCredentialId && c.IsActive);
            if (!credentialExists)
                return new Response<ApplicationRoleVm>("La credencial no existe o está inactiva", false);

            ApplicationRole entity;

            if (dto.ApplicationRoleId.HasValue)
            {
                entity = await _context.ApplicationRoles
                    .FirstOrDefaultAsync(ar => ar.ApplicationRoleId == dto.ApplicationRoleId.Value);

                if (entity == null)
                    return new Response<ApplicationRoleVm>("Rol de aplicación no encontrado", false);

                entity.RoleId = dto.RoleId;
                entity.Description = dto.Description;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = updatedBy;
            }
            else
            {
                var duplicate = await _context.ApplicationRoles.AnyAsync(ar =>
                    ar.AppCredentialId == dto.AppCredentialId &&
                    ar.OrganizationApplicationId == dto.OrganizationApplicationId &&
                    ar.RoleId == dto.RoleId &&
                    ar.IsActive);

                if (duplicate)
                    return new Response<ApplicationRoleVm>("Este rol ya está asignado a esta credencial en ese contexto de organización", false);

                entity = new ApplicationRole
                {
                    AppCredentialId = dto.AppCredentialId,
                    OrganizationApplicationId = dto.OrganizationApplicationId,
                    RoleId = dto.RoleId,
                    Description = dto.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = updatedBy
                };
                _context.ApplicationRoles.Add(entity);
            }

            await _context.SaveChangesAsync();

            await _context.Entry(entity).Reference(ar => ar.Role).LoadAsync();

            return new Response<ApplicationRoleVm>(MapToVm(entity), dto.ApplicationRoleId.HasValue
                ? "Rol de aplicación actualizado exitosamente"
                : "Rol de aplicación asignado exitosamente")
            { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new Response<ApplicationRoleVm>($"Error al guardar el rol de aplicación: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int applicationRoleId, Guid updatedBy)
    {
        try
        {
            var entity = await _context.ApplicationRoles
                .FirstOrDefaultAsync(ar => ar.ApplicationRoleId == applicationRoleId);

            if (entity == null)
                return new Response<string>("Rol de aplicación no encontrado", false);

            if (!entity.IsActive)
                return new Response<string>("El rol de aplicación ya está inactivo", false);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            return new Response<string>("Rol de aplicación eliminado exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al eliminar el rol de aplicación: {ex.Message}", false);
        }
    }

    private static ApplicationRoleVm MapToVm(ApplicationRole entity) => new()
    {
        ApplicationRoleId = entity.ApplicationRoleId,
        AppCredentialId = entity.AppCredentialId,
        OrganizationApplicationId = entity.OrganizationApplicationId,
        RoleId = entity.RoleId,
        RoleName = entity.Role?.RoleName,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
