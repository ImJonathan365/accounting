using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService                _service;
    private readonly IExportService                 _export;
    private readonly IOrgSettingsService            _settings;
    private readonly IValidator<CreateInvoiceDto>   _createValidator;
    private readonly IValidator<CreatePaymentDto>   _paymentValidator;

    public InvoicesController(
        IInvoiceService service,
        IExportService export,
        IOrgSettingsService settings,
        IValidator<CreateInvoiceDto> createValidator,
        IValidator<CreatePaymentDto> paymentValidator)
    {
        _service          = service;
        _export           = export;
        _settings         = settings;
        _createValidator  = createValidator;
        _paymentValidator = paymentValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll(
        Guid orgId, [FromQuery] InvoiceType? type, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(orgId, type, ct));

    [HttpGet("overdue")]
    public async Task<ActionResult<List<OverdueInvoiceDto>>> GetOverdue(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetOverdueAsync(orgId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid orgId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(orgId, id, ct));

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(Guid orgId, [FromBody] CreateInvoiceDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAsync(orgId, dto, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.IssueAsync(orgId, id, ct));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(
        Guid orgId, Guid id, [FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _paymentValidator.ValidateAndThrowAsync(dto, ct);
        return Ok(await _service.RecordPaymentAsync(orgId, id, dto, ct));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<InvoiceDto>> Void(Guid orgId, Guid id, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.VoidAsync(orgId, id, ct));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid orgId, Guid id, CancellationToken ct)
    {
        var inv      = await _service.GetByIdAsync(orgId, id, ct);
        var settings = await _settings.GetAsync(orgId, ct);
        var pdf      = _export.GenerateInvoicePdf(inv, settings);
        return File(pdf, "application/pdf", $"factura-{inv.Number}.pdf");
    }
}
