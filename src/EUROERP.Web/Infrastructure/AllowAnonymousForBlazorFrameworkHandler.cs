using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EUROERP.Web.Infrastructure;

public class AllowAnonymousForBlazorFrameworkHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var path = httpContext.Request.Path;
        if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var requirement in context.Requirements)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
