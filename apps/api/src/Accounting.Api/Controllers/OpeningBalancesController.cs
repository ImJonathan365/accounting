using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations/{orgId:guid}/opening-balances")]
public class OpeningBalancesController : ControllerBase
{
    private readonly IOpeningBalanceService _service;
    public OpeningBalancesController(IOpeningBalanceService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<OpeningBalanceResultDto>> Set(
        Guid orgId, [FromBody] SetOpeningBalancesRequest dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();

        try
        {
            var result = await _service.SetAsync(orgId, dto, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }
}
