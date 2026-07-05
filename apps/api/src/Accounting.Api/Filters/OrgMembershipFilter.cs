using System.Security.Claims;
using Accounting.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Accounting.Api.Filters;

public class OrgMembershipFilter : IAsyncActionFilter
{
    private readonly IOrganizationRepository _orgs;

    public OrgMembershipFilter(IOrganizationRepository orgs) => _orgs = orgs;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue("orgId", out var orgIdObj) ||
            !Guid.TryParse(orgIdObj?.ToString(), out var orgId))
        {
            context.Result = new BadRequestObjectResult(new
            {
                type   = "https://httpstatuses.com/400",
                title  = "Identificador de organización inválido.",
                status = 400
            });
            return;
        }

        var sub = context.HttpContext.User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                type   = "https://httpstatuses.com/401",
                title  = "Token inválido.",
                status = 401
            });
            return;
        }

        // Single query: get role (null → not a member)
        var role = await _orgs.GetMemberRoleAsync(orgId, userId);
        if (role is null)
        {
            context.Result = new ObjectResult(new
            {
                type   = "https://httpstatuses.com/403",
                title  = "No tienes acceso a esta organización.",
                status = 403
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        // Store role so controllers can check permissions without extra DB queries
        context.HttpContext.Items["OrgRole"] = role;

        await next();
    }
}
