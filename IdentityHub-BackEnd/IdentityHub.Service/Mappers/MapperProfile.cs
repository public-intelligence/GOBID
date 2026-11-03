using AutoMapper;
using IdentityHub.Domain.DTOs.Organizations;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Models;

namespace IdentityHub.Service.Mappers;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<LegalEntity, LegalEntityDTO>();
        CreateMap<CreateLegalEntityDTO, LegalEntity>();
        CreateMap<UpdateOrganizationDTO, LegalEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => 
            {
                if (srcMember == null) return false;
                if (srcMember is string str && string.IsNullOrWhiteSpace(str)) return false;

                // Para propiedades del DTO que son nullable enums, verificar si tienen valor
                var property = src.GetType().GetProperty(opts.DestinationMember.Name);
                if (property != null)
                {
                    var propertyType = property.PropertyType;
                    var underlyingType = Nullable.GetUnderlyingType(propertyType);
                    if (underlyingType != null && underlyingType.IsEnum)
                    {
                        // Es un enum nullable, verificar si la propiedad tiene valor
                        var value = property.GetValue(src);
                        return value != null;
                    }
                }

                return true;
            }));
        CreateMap<Person, PersonDTO>();
        CreateMap<CreatePersonDTO, Person>();
        CreateMap<UpdatePersonDTO, Person>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => 
            {
                if (srcMember == null) return false;
                if (srcMember is string str && string.IsNullOrWhiteSpace(str)) return false;

                // Para propiedades del DTO que son nullable enums, verificar si tienen valor
                var property = src.GetType().GetProperty(opts.DestinationMember.Name);
                if (property != null)
                {
                    var propertyType = property.PropertyType;
                    var underlyingType = Nullable.GetUnderlyingType(propertyType);
                    if (underlyingType != null && underlyingType.IsEnum)
                    {
                        // Es un enum nullable, verificar si la propiedad tiene valor
                        var value = property.GetValue(src);
                        return value != null;
                    }
                }

                return true;
            }));
    }
}
