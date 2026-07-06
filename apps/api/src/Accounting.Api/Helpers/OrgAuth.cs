using System.Security.Claims;

namespace Accounting.Api.Helpers;

public static class OrgAuth
{
    private const string RoleKey = "OrgRole";

    public static string GetRole(HttpContext ctx) =>
        ctx.Items.TryGetValue(RoleKey, out var r) ? r as string ?? "member" : "member";

    public static bool HasRole(HttpContext ctx, params string[] roles) =>
        roles.Contains(GetRole(ctx));

    public static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue("sub")!);
}
