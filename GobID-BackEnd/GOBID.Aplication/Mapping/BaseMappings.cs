using AutoMapper;
using GOBID.Application.DTOs.Organization;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.DTOs.Application;
using GOBID.Application.ViewModels.User;
using GOBID.Application.DTOs.User;
using GOBID.Application.DTOs.AccessType;
using GOBID.Application.ViewModels.AccessType;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.DTOs.ApplicationAccess;
using GOBID.Application.ViewModels.ApplicationAccess;
using GOBID.Application.DTOs.Claim;
using GOBID.Application.ViewModels.Claim;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.ViewModels.UserApplication;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.DTOs.UserRole;
using GOBID.Application.ViewModels.UserRole;
using GOBID.Application.DTOs.Roleclaim;
using GOBID.Application.ViewModels.RoleClaim;
using GOBID.Application.DTOs.OrganizationContact;
using GOBID.Application.ViewModels.OrganizationContact;
using GOBID.Application.DTOs.Policies;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.DTOs.UserApplicationPolicy;
using GOBID.Application.ViewModels.UserApplicationPolicy;

namespace GOBID.Application.Mapping;

public class BaseMappings : Profile
{
    public BaseMappings()
    {
        CreateMap<OrganizationDto, OrganizationVm>();

        CreateMap<ApplicationDto, ApplicationVm>();

        CreateMap<UserDto, UserVm>();

        CreateMap<AccessTypeDto, AccessTypeVm>();

        CreateMap<OrganizationApplicationDto, OrganizationApplicationVm>();

        CreateMap<ApplicationAccessDto, ApplicationAccessVm>();

        CreateMap<ClaimDto, ClaimVm>();

        CreateMap<ApplicationUpsertResponseDto, ApplicationUpsertResponseVm>();

        CreateMap<UserApplicationDto, UserApplicationVm>();

        CreateMap<RoleDto, RoleVm>();

        CreateMap<UserRoleDto, UserRoleVm>();
        CreateMap<RoleClaimDto, RoleClaimVm>();

        CreateMap<OrganizationContactDto, OrganizationContactVm>();

        CreateMap<PolicyTypeDto, PolicyTypeVm>();
        CreateMap<PolicyDto, PolicyVm>();
        CreateMap<PolicyConditionDto, PolicyConditionVm>();
        CreateMap<UserApplicationPolicyDto, UserApplicationPolicyVm>();
    }
}
