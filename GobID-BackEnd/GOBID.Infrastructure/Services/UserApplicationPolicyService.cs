using GOBID.Application.DTOs.UserApplicationPolicy;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.UserApplicationPolicy;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Services;

public class UserApplicationPolicyService : IUserApplicationPolicyService
{
    private readonly DbContext _dbContext;

    public UserApplicationPolicyService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response<List<UserApplicationPolicyVm>>> ListByUserApplicationAsync(Guid userApplicationId, bool? isActive)
    {
        try
        {
            var query = _dbContext.Set<UserApplicationPolicy>()
                .Include(x => x.Policy)
                    .ThenInclude(p => p.PolicyType)
                .Where(x => x.UserApplicationId == userApplicationId);

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            var result = await query
                .Select(x => new UserApplicationPolicyVm
                {
                    UserApplicationPolicyId = x.UserApplicationPolicyId,
                    UserApplicationId = x.UserApplicationId,
                    PolicyId = x.PolicyId,
                    PolicyName = x.Policy.Name,
                    PolicyTypeName = x.Policy.PolicyType.Name,
                    Effect = x.Policy.Effect,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new Response<List<UserApplicationPolicyVm>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<UserApplicationPolicyVm>>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> AssignAsync(UserApplicationPolicyUpsertDto dto)
    {
        try
        {
            // Evitar duplicados activos
            var existing = await _dbContext.Set<UserApplicationPolicy>()
                .FirstOrDefaultAsync(x => x.UserApplicationId == dto.UserApplicationId
                                       && x.PolicyId == dto.PolicyId
                                       && x.IsActive);

            if (existing != null)
                return new Response<string>("La política ya está asignada a este usuario", false);

            var entity = new UserApplicationPolicy
            {
                UserApplicationPolicyId = Guid.NewGuid(),
                UserApplicationId = dto.UserApplicationId,
                PolicyId = dto.PolicyId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            };

            await _dbContext.Set<UserApplicationPolicy>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return new Response<string>("Política asignada exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RemoveAsync(Guid userApplicationPolicyId, Guid updatedBy)
    {
        try
        {
            var entity = await _dbContext.Set<UserApplicationPolicy>().FindAsync(userApplicationPolicyId);
            if (entity == null)
                return new Response<string>("Asignación no encontrada", false);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = updatedBy;
            _dbContext.Set<UserApplicationPolicy>().Update(entity);
            await _dbContext.SaveChangesAsync();
            return new Response<string>("Política removida exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }
}
