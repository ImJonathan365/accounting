using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Export;
using FluentAssertions;
using NSubstitute;
using QuestPDF.Infrastructure;

namespace Accounting.Tests.Unit.Services;

public class ExportServiceTests
{
    private readonly IReportService     _reports  = Substitute.For<IReportService>();
    private readonly IOrgSettingsService _settings = Substitute.For<IOrgSettingsService>();
    private readonly ExportService      _sut;

    private static readonly Guid     OrgId    = Guid.NewGuid();
    private static readonly DateOnly From     = new(2026, 1, 1);
    private static readonly DateOnly To       = new(2026, 12, 31);
    private static readonly OrgSettingsDto DefaultSettings =
        new(OrgId, "Empresa Test", null, null, null, null, null, "$", ReportTheme.Minimal);

    public ExportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        _sut = new ExportService(_reports, _settings);

        _settings.GetAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(DefaultSettings);

        _reports.GetTrialBalanceAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new TrialBalanceDto(From, To,
                Lines: new List<TrialBalanceLineDto>(),
                TotalDebit: 0, TotalCredit: 0,
                TotalDebitBalance: 0, TotalCreditBalance: 0,
                IsBalanced: true));

        _reports.GetIncomeStatementAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new IncomeStatementDto(From, To,
                Income:   new IncomeStatementSectionDto("Ingresos", new List<IncomeStatementLineDto>(), 0),
                Expenses: new IncomeStatementSectionDto("Gastos",   new List<IncomeStatementLineDto>(), 0),
                NetIncome: 0, IsProfit: true));

        _reports.GetBalanceSheetAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new BalanceSheetDto(To,
                Assets:      new BalanceSheetGroupDto("Activos",    new List<BalanceSheetSectionDto>(), 0),
                Liabilities: new BalanceSheetGroupDto("Pasivos",    new List<BalanceSheetSectionDto>(), 0),
                Equity:      new BalanceSheetGroupDto("Patrimonio", new List<BalanceSheetSectionDto>(), 0),
                NetIncome: 0, TotalEquity: 0, TotalLiabilitiesAndEquity: 0, IsBalanced: true));

        _reports.GetCashFlowAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new CashFlowDto(From, To,
                BeginningCash: 0,
                Operating:  new CashFlowSectionDto("Operaciones",    new List<CashFlowLineDto>(), 0, 0),
                Investing:  new CashFlowSectionDto("Inversión",      new List<CashFlowLineDto>(), 0, 0),
                Financing:  new CashFlowSectionDto("Financiamiento", new List<CashFlowLineDto>(), 0, 0),
                NetChange: 0, EndingCash: 0));
    }

    // ── TrialBalance ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportTrialBalanceAsync_Pdf_ReturnsNonEmptyBytesWithPdfContentType()
    {
        var result = await _sut.ExportTrialBalanceAsync(OrgId, From, To, "pdf");

        result.ContentType.Should().Be("application/pdf");
        result.Data.Should().NotBeEmpty();
        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task ExportTrialBalanceAsync_Csv_ReturnsCsvContentType()
    {
        var result = await _sut.ExportTrialBalanceAsync(OrgId, From, To, "csv");

        result.ContentType.Should().Be("text/csv");
        result.Data.Should().NotBeEmpty();
        result.FileName.Should().EndWith(".csv");
    }

    // ── IncomeStatement ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExportIncomeStatementAsync_Pdf_ReturnsNonEmptyBytesWithPdfContentType()
    {
        var result = await _sut.ExportIncomeStatementAsync(OrgId, From, To, "pdf");

        result.ContentType.Should().Be("application/pdf");
        result.Data.Should().NotBeEmpty();
    }

    // ── BalanceSheet ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBalanceSheetAsync_Pdf_ReturnsNonEmptyBytesWithPdfContentType()
    {
        var result = await _sut.ExportBalanceSheetAsync(OrgId, To, "pdf");

        result.ContentType.Should().Be("application/pdf");
        result.Data.Should().NotBeEmpty();
    }

    // ── CashFlow ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportCashFlowAsync_Pdf_ReturnsNonEmptyBytesWithPdfContentType()
    {
        var result = await _sut.ExportCashFlowAsync(OrgId, From, To, "pdf");

        result.ContentType.Should().Be("application/pdf");
        result.Data.Should().NotBeEmpty();
    }

    // ── GenerateInvoicePdf ────────────────────────────────────────────────────

    [Fact]
    public void GenerateInvoicePdf_ReturnsNonEmptyBytes()
    {
        var invoice = new InvoiceDto(
            Id:                Guid.NewGuid(),
            Type:              InvoiceType.Receivable,
            ContactId:         Guid.NewGuid(),
            ContactName:       "Cliente Test",
            Number:            "F-001",
            Date:              "2026-01-15",
            DueDate:           "2026-02-15",
            Status:            InvoiceStatus.Draft,
            StatusLabel:       "Borrador",
            ArApAccountId:     Guid.NewGuid(),
            ArApAccountName:   "Cuentas por Cobrar",
            Notes:             null,
            JournalEntryId:    null,
            SubTotal:          100m,
            TaxTotal:          16m,
            Total:             116m,
            Paid:              0m,
            Balance:           116m,
            Lines:             new List<InvoiceLineDto>(),
            Payments:          new List<InvoicePaymentDto>());

        var bytes = _sut.GenerateInvoicePdf(invoice, DefaultSettings);

        bytes.Should().NotBeEmpty();
    }
}
