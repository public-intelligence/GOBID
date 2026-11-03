using GOBID.Application.DTOs.Policies;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Services;

public class PolicyService : IPolicyService
{
    private readonly DbContext _dbContext;

    public PolicyService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response<List<PolicyVm>>> ListByOrganizationAsync(int organizationId, bool? isActive)
    {
        try
        {
            var query = _dbContext.Set<Policy>()
                .Include(p => p.PolicyType)
                .Include(p => p.PolicyConditions)
                .Where(p => p.OrganizationId == organizationId);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var result = await query
                .OrderBy(p => p.Name)
                .Select(p => new PolicyVm
                {
                    PolicyId = p.PolicyId,
                    OrganizationId = p.OrganizationId,
                    PolicyTypeId = p.PolicyTypeId,
                    PolicyTypeName = p.PolicyType.Name,
                    Name = p.Name,
                    Description = p.Description,
                    Effect = p.Effect,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Conditions = p.PolicyConditions.Select(c => new PolicyConditionVm
                    {
                        PolicyConditionId = c.PolicyConditionId,
                        ConditionKey = c.ConditionKey,
                        ConditionValue = c.ConditionValue
                    }).ToList()
                })
                .ToListAsync();

            return new Response<List<PolicyVm>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<PolicyVm>>($"Error al listar políticas: {ex.Message}", false);
        }
    }

    public async Task<Response<PolicyVm>> GetByIdAsync(Guid policyId)
    {
        try
        {
            var policy = await _dbContext.Set<Policy>()
                .Include(p => p.PolicyType)
                .Include(p => p.PolicyConditions)
                .FirstOrDefaultAsync(p => p.PolicyId == policyId);

            if (policy == null)
                return new Response<PolicyVm>("Política no encontrada", false);

            return new Response<PolicyVm>(new PolicyVm
            {
                PolicyId = policy.PolicyId,
                OrganizationId = policy.OrganizationId,
                PolicyTypeId = policy.PolicyTypeId,
                PolicyTypeName = policy.PolicyType?.Name,
                Name = policy.Name,
                Description = policy.Description,
                Effect = policy.Effect,
                IsActive = policy.IsActive,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
                Conditions = policy.PolicyConditions.Select(c => new PolicyConditionVm
                {
                    PolicyConditionId = c.PolicyConditionId,
                    ConditionKey = c.ConditionKey,
                    ConditionValue = c.ConditionValue
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return new Response<PolicyVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> UpsertAsync(PolicyUpsertDto dto)
    {
        try
        {
            if (dto.PolicyId.HasValue && dto.PolicyId != Guid.Empty)
            {
                var entity = await _dbContext.Set<Policy>()
                    .Include(p => p.PolicyConditions)
                    .FirstOrDefaultAsync(p => p.PolicyId == dto.PolicyId.Value);

                if (entity == null)
                    return new Response<string>("Política no encontrada", false);

                entity.Name = dto.Name;
                entity.Description = dto.Description;
                entity.Effect = dto.Effect;
                entity.IsActive = dto.IsActive;
                entity.PolicyTypeId = dto.PolicyTypeId;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = dto.UpdatedBy;

                // Reemplazar condiciones
                _dbContext.Set<PolicyCondition>().RemoveRange(entity.PolicyConditions);
                entity.PolicyConditions = dto.Conditions.Select(c => new PolicyCondition
                {
                    PolicyId = entity.PolicyId,
                    ConditionKey = c.ConditionKey,
                    ConditionValue = c.ConditionValue
                }).ToList();

                _dbContext.Set<Policy>().Update(entity);
            }
            else
            {
                var entity = new Policy
                {
                    PolicyId = Guid.NewGuid(),
                    OrganizationId = dto.OrganizationId,
                    PolicyTypeId = dto.PolicyTypeId,
                    Name = dto.Name,
                    Description = dto.Description,
                    Effect = dto.Effect,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = dto.CreatedBy
                };

                entity.PolicyConditions = dto.Conditions.Select(c => new PolicyCondition
                {
                    PolicyId = entity.PolicyId,
                    ConditionKey = c.ConditionKey,
                    ConditionValue = c.ConditionValue
                }).ToList();

                await _dbContext.Set<Policy>().AddAsync(entity);
            }

            await _dbContext.SaveChangesAsync();
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid policyId)
    {
        try
        {
            var entity = await _dbContext.Set<Policy>().FindAsync(policyId);
            if (entity == null)
                return new Response<string>("Política no encontrada", false);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            _dbContext.Set<Policy>().Update(entity);
            await _dbContext.SaveChangesAsync();
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }
}
