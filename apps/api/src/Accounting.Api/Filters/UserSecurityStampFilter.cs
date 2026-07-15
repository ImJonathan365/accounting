using System.Security.Claims;
using Accounting.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Accounting.Api.Filters;

public class UserSecurityStampFilter : IAsyncActionFilter
{
    private readonly IUserRepository _users;
    public UserSecurityStampFilter(IUserRepository users) => _users = users;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sub = context.HttpContext.User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            context.Result = new ObjectResult(new
            {
                type   = "https://httpstatuses.com/401",
                title  = "Token inválido.",
                status = 401
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var stampClaim = context.HttpContext.User.FindFirstValue("security_stamp");
        if (!Guid.TryParse(stampClaim, out var claimStamp))
        {
            context.Result = new ObjectResult(new
            {
                type   = "https://httpstatuses.com/401",
                title  = "Sesión inválida. Inicia sesión de nuevo.",
                status = 401
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var currentStamp = await _users.GetSecurityStampAsync(userId);
        if (currentStamp is null || currentStamp.Value != claimStamp)
        {
            context.Result = new ObjectResult(new
            {
                type   = "https://httpstatuses.com/401",
                title  = "Sesión inválida. Inicia sesión de nuevo.",
                status = 401
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}
