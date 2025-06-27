// Mappings/AuthMappingProfile.cs
using AutoMapper;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Auth;

namespace FallenFaction.Server.Mappings
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<AppUser, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles will be set manually in the service
        }
    }
}