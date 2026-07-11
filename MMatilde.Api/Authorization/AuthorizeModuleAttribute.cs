using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MMatilde.Api.Services;
using System.Security.Claims;

namespace MMatilde.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Modulo { get; }

    public AuthorizeModuleAttribute(string modulo) => Modulo = modulo;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
            return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.IsInRole("ADMIN"))
            return;

        var permisos = context.HttpContext.RequestServices.GetRequiredService<PermisosModulosService>();
        var rol = user.FindFirstValue(ClaimTypes.Role);
        if (!await permisos.CanAccessAsync(rol, Modulo))
            context.Result = new ForbidResult();
    }
}
