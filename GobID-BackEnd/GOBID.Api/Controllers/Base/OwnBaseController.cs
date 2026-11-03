using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace GOBID.Api.Controllers.Base
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    public class OwnBaseController : ControllerBase, IAsyncActionFilter
    {
        private readonly IAuthService _authService;

        public OwnBaseController(IAuthService authService)
        {
            _authService = authService;
        }


        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Los tokens M2M no llevan session_id embebido; su sesión se valida en el refresh
                var isServiceAccount = User.FindFirst("is_service_account")?.Value == "true";
                if (!isServiceAccount)
                {
                    var sessionId = GetSessionIdFromToken();

                    if (!sessionId.HasValue)
                    {
                        context.Result = Unauthorized(new Response<string>("Token sin sesión activa. Por favor inicie sesión nuevamente.", false));
                        return;
                    }

                    var isActive = await _authService.IsSessionActiveAsync(sessionId.Value);
                    if (!isActive)
                    {
                        context.Result = Unauthorized(new Response<string>("La sesión ha sido cerrada. Por favor inicie sesión nuevamente.", false));
                        return;
                    }
                }
            }

            await next();
        }

        protected Guid? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        protected async Task<string[]> GetPermissionFromTokenAsync()
        {
            // M2M token: los permisos vienen embebidos en el JWT como claim "permissions" (JSON array)
            var isServiceAccount = User.FindFirst("is_service_account")?.Value == "true";
            if (isServiceAccount)
            {
                var permissionsClaim = User.FindFirst("permissions")?.Value;
                if (string.IsNullOrEmpty(permissionsClaim))
                    return Array.Empty<string>();

                var perms = System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsClaim);
                return perms ?? Array.Empty<string>();
            }

            // Token de usuario: se cargan los permisos desde la base de datos
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var orgAppIdClaim = User.FindFirst("organization_application_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(orgAppIdClaim))
                return Array.Empty<string>();

            if (!Guid.TryParse(userIdClaim, out var userId) || !int.TryParse(orgAppIdClaim, out var orgAppId))
                return Array.Empty<string>();

            var response = await _authService.GetClaimsAsync(userId, orgAppId);
            if (response == null || !response.Succeeded || response.Data == null || string.IsNullOrEmpty(response.Data.ClaimCode))
                return Array.Empty<string>();

            return response.Data.ClaimCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        protected int GetOrganizationIdFromToken()
        {
            var orgIdClaim = User.FindFirst("organization_id")?.Value;
            if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
                return 0;
            return orgId;
        }

        protected Guid? GetApplicationIdFromToken()
        {
            var appIdClaim = User.FindFirst("application_id")?.Value;
            if (string.IsNullOrEmpty(appIdClaim) || !Guid.TryParse(appIdClaim, out var appId))
                return null;
            return appId;
        }

        protected int? GetOrganizationApplicationIdFromToken()
        {
            var claim = User.FindFirst("organization_application_id")?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                return null;
            return id;
        }

        protected Guid? GetSessionIdFromToken()
        {
            var sessionIdClaim = User.FindFirst("session_id")?.Value;
            if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
                return null;
            return sessionId;
        }

        protected static bool IsSuperAdmin(string[] permissions)
            => permissions.Contains("super_admin");

        protected static bool HasPermissionOrSuperAdmin(string[] permissions, string permission)
            => permissions.Contains(permission) || IsSuperAdmin(permissions);
    }
}
