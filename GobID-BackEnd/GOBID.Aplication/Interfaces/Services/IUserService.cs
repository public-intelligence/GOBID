using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.User;
using GOBID.Application.ViewModels.User;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IUserService
{
    public Task<Response<string>> Upsert(UserUpsertDto dto, Guid? applicationId);
    public Task<Response<string>> Delete(Guid userId, Guid deleteBy, string? observation);
    public Task<PagedResponse<UserVm>> ListAsync(int? pageNumber, int? pageSize, UserFilterDto? filter, int? organizationId);
    public Task<Response<UserVm>> GetByIdAsync(Guid userId, int? organizationId = null);
    public Task<Response<PersonnelDto>> GetPersonnelByIdentificationNumber(string identificationNumber);
    public Task<Response<PersonnelDto>> GetPersonnelByIdAsync(Guid personnelId);
    public Task<PagedResponse<UserVm>> ListCountAsync(int? pageNumber, int? pageSize, UserFilterDto? filter);
    public Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    public Task<Response<PersonnelInfoDto>> GetPersonnelInfoByIdAsync(Guid personnelId);
    Task<Response<List<AreaDto>>> GetAreasForOrganizationAsync(Guid organizationId);
    public Task<Response<string>> UpsertUserOrganization(UserOrganizationUpsertDto dto);
    public Task<Response<string>> UpdateUserStatusAsync(UserStatusUpdateDto dto);
    public Task<Response<UserOrganizationDto>> GetUserByOrgAppIdAsync(int orgAppId);
    public Task<Response<UserOrganizationDto>> GetUserOrganizationByUserIdAsync(Guid userId);
    public Task<PagedResponse<UserWithPersonnelDto>> ListUsersByOrganizationApplicationAsync(int organizationApplicationId, int? pageNumber, int? pageSize);
    public Task<PagedResponse<UserWithPersonnelDto>> GetUsersByOrganizationApplicationWithPositionAsync(int organizationApplicationId, string? position, int? pageNumber, int? pageSize);
    public Task<PagedResponse<UserVm>> ListUsersByOrganizationAsync(int organizationId, int? pageNumber, int? pageSize, UserOrganizationFilterDto? filter);

    /// <summary>True si la aplicación está instalada y activa en la organización (alcance de cuentas de servicio, G1).</summary>
    public Task<bool> IsApplicationInOrganizationAsync(Guid applicationId, int organizationId);
    public Task<Response<string>> RegisterPublicAsync(PublicRegisterDto dto);
    public Task<Response<ConfirmEmailDto>> ConfirmEmailAsync(ConfirmEmailDto dto);
    public Task<Response<string>> RegisterPersonAndUserAsync(RegisterPersonAndUserDto dto);
    public Task<Response<UserVm>> GetByEmailAsync(string email);
    Task<Response<UserVm>> GetByIdInfoAsync(Guid UserId, int? organizationId = null);
}