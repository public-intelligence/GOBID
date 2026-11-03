using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GOBID.Infrastructure.Services
{
    public class BaseService
    {
        private readonly IHttpContextAccessor _context;

        public BaseService(IHttpContextAccessor context)
        {
            _context = context;
        }

        //public ApplicationUser GetLoggerUser()
        //{
        //    var identity = _context.HttpContext?.User?.Identity as ClaimsIdentity;

        //    if (identity != null && identity.IsAuthenticated)
        //    {
        //        var clientAreasJson = identity.FindFirst("client_areas")?.Value;

        //        ClientAreaDto areaObject = null;
        //        if (!string.IsNullOrWhiteSpace(clientAreasJson))
        //        {
        //            try
        //            {
        //                areaObject = JsonConvert.DeserializeObject<ClientAreaDto>(clientAreasJson);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error deserializing client_areas: {ex.Message}");
        //            }
        //        }

        //        var userData = new ApplicationUser
        //        {
        //            ClientId = identity.FindFirst("client_id")?.Value,
        //            JwtId = identity.FindFirst("jti")?.Value,
        //            Scope = identity.FindFirst("scope")?.Value,
        //            Scopes = identity.FindAll("scope").Select(c => c.Value).ToList(),
        //            AreaId = identity.FindFirst("client_area_id")?.Value,
        //            Area = identity.FindFirst("client_area")?.Value,
        //            ClientAreaObject = areaObject
        //        };

        //        return userData;
        //    }

        //    return null;
        //}

        public string? GetClientId()
        {
            return GetClaimValue("client_id");
        }

        public string? GetAreaId()
        {
            return GetClaimValue("client_area_id");
        }

        public string? GetAreaName()
        {
            return GetClaimValue("client_area");
        }

        public string? GetScope()
        {
            var identity = _context.HttpContext?.User?.Identity as ClaimsIdentity;

            if (identity != null && identity.IsAuthenticated)
            {
                return identity.FindFirst("scope")?.Value;
            }

            return null;
        }

        public List<string> GetScopes()
        {
            var identity = _context.HttpContext?.User?.Identity as ClaimsIdentity;

            if (identity != null && identity.IsAuthenticated)
            {
                return identity.FindAll("scope").Select(c => c.Value).ToList();
            }

            return new List<string>();
        }

        private string? GetClaimValue(string claimType)
        {
            var identity = _context.HttpContext?.User?.Identity as ClaimsIdentity;

            if (identity != null && identity.IsAuthenticated)
            {
                return identity.FindFirst(claimType)?.Value;
            }

            return null;
        }

        





    }
}
