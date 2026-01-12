using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Security
{
    public sealed class PermissionAuthorizeAttribute : TypeFilterAttribute
    {
        public PermissionAuthorizeAttribute(string permissionKey) : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permissionKey };
        }
    }

    public sealed class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly KDContext _context;
        private readonly ILogger<PermissionAuthorizationFilter> _logger;
        private readonly string _permissionKey;

        public PermissionAuthorizationFilter(KDContext context, ILogger<PermissionAuthorizationFilter> logger, string permissionKey)
        {
            _context = context;
            _logger = logger;
            _permissionKey = permissionKey;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var principal = context.HttpContext.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ForbidResult();
                return;
            }

            var roleName = principal.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                context.Result = new ForbidResult();
                return;
            }

            var allowed = await _context.tblRolePermission
                .Where(rp => rp.PermissionKey == _permissionKey && rp.IsAllowed)
                .AnyAsync(rp => rp.Role != null && rp.Role.RoleName == roleName);

            if (!allowed)
            {
                _logger.LogWarning("Permission '{Permission}' denied for role '{Role}'.", _permissionKey, roleName);
                context.Result = new ForbidResult();
            }
        }
    }
}
