using Accounting.Api.Filters;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;
    private readonly IExportService _export;

    public ReportsController(IReportService service, IExportService export)
    {
        _service = service;
        _export  = export;
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceDto>> TrialBalance(
        Guid orgId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await _service.GetTrialBalanceAsync(
            orgId, from ?? new DateOnly(today.Year, 1, 1), to ?? today, ct));
    }

    [HttpGet("income-statement")]
    public async Task<ActionResult<IncomeStatementDto>> IncomeStatement(
        Guid orgId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await _service.GetIncomeStatementAsync(
            orgId, from ?? new DateOnly(today.Year, 1, 1), to ?? today, ct));
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetDto>> BalanceSheet(
        Guid orgId, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await _service.GetBalanceSheetAsync(orgId, asOf ?? today, ct));
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<LedgerDto>> Ledger(
        Guid orgId, [FromQuery] Guid accountId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await _service.GetLedgerAsync(
            orgId, accountId,
            from ?? new DateOnly(today.Year, 1, 1), to ?? today, ct));
    }

    [HttpGet("trial-balance/export")]
    public async Task<IActionResult> ExportTrialBalance(
        Guid orgId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var today  = DateOnly.FromDateTime(DateTime.Today);
        var result = await _export.ExportTrialBalanceAsync(
            orgId, from ?? new DateOnly(today.Year, 1, 1), to ?? today, format, ct);
        return File(result.Data, result.ContentType, result.FileName);
    }

    [HttpGet("income-statement/export")]
    public async Task<IActionResult> ExportIncomeStatement(
        Guid orgId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var today  = DateOnly.FromDateTime(DateTime.Today);
        var result = await _export.ExportIncomeStatementAsync(
            orgId, from ?? new DateOnly(today.Year, 1, 1), to ?? today, format, ct);
        return File(result.Data, result.ContentType, result.FileName);
    }

    [HttpGet("balance-sheet/export")]
    public async Task<IActionResult> ExportBalanceSheet(
        Guid orgId, [FromQuery] DateOnly? asOf,
        [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var today  = DateOnly.FromDateTime(DateTime.Today);
        var result = await _export.ExportBalanceSheetAsync(orgId, asOf ?? today, format, ct);
        return File(result.Data, result.ContentType, result.FileName);
    }
}
