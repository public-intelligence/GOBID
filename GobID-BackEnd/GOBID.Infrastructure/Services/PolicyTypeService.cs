using GOBID.Application.DTOs.Policies;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GOBID.Infrastructure.Services;

public class PolicyTypeService : IPolicyTypeService
{
    private readonly DbContext _dbContext;

    public PolicyTypeService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response<List<PolicyTypeVm>>> ListAsync(bool? isActive)
    {
        try
        {
            var query = _dbContext.Set<PolicyType>().AsQueryable();
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            var result = await query
                .OrderBy(x => x.Name)
                .Select(x => new PolicyTypeVm
                {
                    PolicyTypeId = x.PolicyTypeId,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new Response<List<PolicyTypeVm>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<PolicyTypeVm>>($"Error al listar tipos de política: {ex.Message}", false);
        }
    }

    public async Task<Response<PolicyTypeVm>> GetByIdAsync(int policyTypeId)
    {
        try
        {
            var entity = await _dbContext.Set<PolicyType>().FindAsync(policyTypeId);
            if (entity == null)
                return new Response<PolicyTypeVm>("Tipo de política no encontrado", false);

            return new Response<PolicyTypeVm>(new PolicyTypeVm
            {
                PolicyTypeId = entity.PolicyTypeId,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return new Response<PolicyTypeVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> UpsertAsync(PolicyTypeUpsertDto dto)
    {
        try
        {
            if (dto.PolicyTypeId.HasValue && dto.PolicyTypeId > 0)
            {
                var entity = await _dbContext.Set<PolicyType>().FindAsync(dto.PolicyTypeId.Value);
                if (entity == null)
                    return new Response<string>("Tipo de política no encontrado", false);

                entity.Name = dto.Name;
                entity.Description = dto.Description;
                entity.IsActive = dto.IsActive;
                _dbContext.Set<PolicyType>().Update(entity);
            }
            else
            {
                var entity = new PolicyType
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = dto.CreatedBy
                };
                await _dbContext.Set<PolicyType>().AddAsync(entity);
            }

            await _dbContext.SaveChangesAsync();
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int policyTypeId)
    {
        try
        {
            var entity = await _dbContext.Set<PolicyType>().FindAsync(policyTypeId);
            if (entity == null)
                return new Response<string>("Tipo de política no encontrado", false);

            entity.IsActive = false;
            _dbContext.Set<PolicyType>().Update(entity);
            await _dbContext.SaveChangesAsync();
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }
}
