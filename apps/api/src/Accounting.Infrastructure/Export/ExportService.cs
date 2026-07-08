using System.Text;
using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Accounting.Infrastructure.Export;

public class ExportService : IExportService
{
    private readonly IReportService                  _reports;
    private readonly IOrgSettingsService             _settings;

    public ExportService(IReportService reports, IOrgSettingsService settings)
    {
        _reports  = reports;
        _settings = settings;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<ExportResult> ExportTrialBalanceAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default)
    {
        var data     = await _reports.GetTrialBalanceAsync(orgId, from, to, ct);
        var settings = await _settings.GetAsync(orgId, ct);
        var name     = $"balance-comprobacion_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}";

        return format.ToLower() == "csv"
            ? new ExportResult(TrialBalanceCsv(data, settings), "text/csv", $"{name}.csv")
            : new ExportResult(TrialBalancePdf(data, settings), "application/pdf", $"{name}.pdf");
    }

    public async Task<ExportResult> ExportIncomeStatementAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default)
    {
        var data     = await _reports.GetIncomeStatementAsync(orgId, from, to, ct);
        var settings = await _settings.GetAsync(orgId, ct);
        var name     = $"estado-resultados_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}";

        return format.ToLower() == "csv"
            ? new ExportResult(IncomeStatementCsv(data, settings), "text/csv", $"{name}.csv")
            : new ExportResult(IncomeStatementPdf(data, settings), "application/pdf", $"{name}.pdf");
    }

    public async Task<ExportResult> ExportBalanceSheetAsync(
        Guid orgId, DateOnly asOf, string format, CancellationToken ct = default)
    {
        var data     = await _reports.GetBalanceSheetAsync(orgId, asOf, ct);
        var settings = await _settings.GetAsync(orgId, ct);
        var name     = $"balance-general_{asOf:yyyy-MM-dd}";

        return format.ToLower() == "csv"
            ? new ExportResult(BalanceSheetCsv(data, settings), "text/csv", $"{name}.csv")
            : new ExportResult(BalanceSheetPdf(data, settings), "application/pdf", $"{name}.pdf");
    }

    public async Task<ExportResult> ExportCashFlowAsync(
        Guid orgId, DateOnly from, DateOnly to, string format, CancellationToken ct = default)
    {
        var data     = await _reports.GetCashFlowAsync(orgId, from, to, ct);
        var settings = await _settings.GetAsync(orgId, ct);
        var name     = $"flujo-efectivo_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}";

        return format.ToLower() == "csv"
            ? new ExportResult(CashFlowCsv(data, settings), "text/csv", $"{name}.csv")
            : new ExportResult(CashFlowPdf(data, settings), "application/pdf", $"{name}.pdf");
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    private record ThemeColors(string HeaderBg, string HeaderFg, string AccentBg, string AccentFg);

    private static ThemeColors GetTheme(ReportTheme theme) => theme switch
    {
        ReportTheme.Minimal     => new("#475569", "#ffffff", "#f1f5f9", "#0f172a"),
        ReportTheme.Corporate   => new("#14532d", "#ffffff", "#dcfce7", "#14532d"),
        _                       => new("#4f46e5", "#ffffff", "#e0e7ff", "#3730a3"),
    };

    // ── Shared PDF helpers ────────────────────────────────────────────────────

    private static string Fmt(decimal value) => $"{value:N2}";
    private static string Acct(decimal value, string currency) =>
        value < 0 ? $"({currency} {Math.Abs(value):N2})" : $"{currency} {value:N2}";

    private static void Header(
        IContainer c, OrgSettingsDto s, ThemeColors t, string title, string period)
    {
        c.Background(t.HeaderBg).Padding(16).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(s.CompanyName).Bold().FontSize(14).FontColor(t.HeaderFg);
                if (s.TaxId is not null)
                    col.Item().Text($"NIT/RUC: {s.TaxId}").FontSize(8).FontColor(t.HeaderFg);
                if (s.Address is not null)
                    col.Item().Text(s.Address).FontSize(8).FontColor(t.HeaderFg);
                if (s.Phone is not null)
                    col.Item().Text(s.Phone).FontSize(8).FontColor(t.HeaderFg);
            });
            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(title).Bold().FontSize(16).FontColor(t.HeaderFg);
                col.Item().Text(period).FontSize(9).FontColor(t.HeaderFg);
            });
        });
    }

    private static void Footer(IContainer c)
    {
        c.BorderTop(1).BorderColor("#e2e8f0").PaddingTop(4).Row(row =>
        {
            row.RelativeItem()
               .Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
               .FontSize(7).FontColor("#94a3b8");
            row.RelativeItem().AlignRight().Text(txt =>
            {
                txt.Span("Página ").FontSize(7).FontColor("#94a3b8");
                txt.CurrentPageNumber().FontSize(7).FontColor("#94a3b8");
                txt.Span(" de ").FontSize(7).FontColor("#94a3b8");
                txt.TotalPages().FontSize(7).FontColor("#94a3b8");
            });
        });
    }

    private static void TableHeaderCell(IContainer c, string text, string bg, string fg, bool right = false)
    {
        var base_ = c.Background(bg).Padding(5);
        var txt   = (right ? base_.AlignRight() : base_).Text(text);
        txt.Bold().FontSize(8).FontColor(fg);
    }

    // ── Trial Balance PDF ─────────────────────────────────────────────────────

    private static byte[] TrialBalancePdf(TrialBalanceDto data, OrgSettingsDto s)
    {
        var theme  = GetTheme(s.Theme);
        var period = $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}";

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);

                page.Header().Element(c => Header(c, s, theme, "Balance de Comprobación", period));
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        // Header
                        foreach (var (txt, right) in new[]
                        {
                            ("Código", false), ("Cuenta", false),
                            ("Débito Total", true), ("Crédito Total", true),
                            ("Saldo Deudor", true), ("Saldo Acreedor", true)
                        })
                            TableHeaderCell(table.Cell(), txt, theme.AccentBg, theme.AccentFg, right);

                        // Rows
                        bool alt = false;
                        foreach (var line in data.Lines)
                        {
                            var bg = alt ? "#f8fafc" : "#ffffff";
                            alt = !alt;
                            table.Cell().Background(bg).Padding(4).Text(line.Code).FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(line.Name).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text(Fmt(line.TotalDebit)).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text(Fmt(line.TotalCredit)).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text(Fmt(line.DebitBalance)).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text(Fmt(line.CreditBalance)).FontSize(8);
                        }

                        // Totals
                        const string totalBg = "#e2e8f0";
                        static void T(IContainer c, string v, bool right = false) =>
                            (right ? c.Background(totalBg).Padding(4).AlignRight()
                                   : c.Background(totalBg).Padding(4))
                            .Text(v).Bold().FontSize(8);

                        T(table.Cell(), "TOTALES");
                        T(table.Cell(), "");
                        T(table.Cell(), Fmt(data.TotalDebit), true);
                        T(table.Cell(), Fmt(data.TotalCredit), true);
                        T(table.Cell(), Fmt(data.TotalDebitBalance), true);
                        T(table.Cell(), Fmt(data.TotalCreditBalance), true);
                    });

                    col.Item().PaddingTop(8).AlignRight()
                       .Background(data.IsBalanced ? "#dcfce7" : "#fee2e2")
                       .Padding(6)
                       .Text(data.IsBalanced ? "Cuadrado" : "Descuadrado")
                       .FontSize(9).Bold()
                       .FontColor(data.IsBalanced ? "#14532d" : "#991b1b");
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    // ── Income Statement PDF ──────────────────────────────────────────────────

    private static byte[] IncomeStatementPdf(IncomeStatementDto data, OrgSettingsDto s)
    {
        var theme  = GetTheme(s.Theme);
        var period = $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}";

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);

                page.Header().Element(c => Header(c, s, theme, "Estado de Resultados", period));
                page.Content().PaddingTop(12).Column(col =>
                {
                    void Section(string title, IncomeStatementSectionDto section, string totalLabel)
                    {
                        col.Item().Background(theme.AccentBg).Padding(6)
                           .Text(title).Bold().FontSize(10).FontColor(theme.AccentFg);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            foreach (var (txt, right) in new[] { ("Código", false), ("Cuenta", false), ("Monto", true) })
                                TableHeaderCell(table.Cell(), txt, "#f8fafc", "#334155", right);

                            bool alt = false;
                            foreach (var line in section.Lines)
                            {
                                var bg = alt ? "#f8fafc" : "#ffffff";
                                alt = !alt;
                                table.Cell().Background(bg).Padding(4).Text(line.Code).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(line.Name).FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight()
                                     .Text(Acct(line.Amount, s.CurrencySymbol)).FontSize(8);
                            }
                        });

                        col.Item().Background("#e2e8f0").Padding(5).AlignRight()
                           .Text($"{totalLabel}: {Acct(section.Total, s.CurrencySymbol)}")
                           .Bold().FontSize(9);

                        col.Item().Height(8);
                    }

                    Section("Ingresos", data.Income, "Total Ingresos");
                    Section("Gastos",   data.Expenses, "Total Gastos");

                    var netColor  = data.IsProfit ? "#14532d" : "#991b1b";
                    var netBg     = data.IsProfit ? "#dcfce7"  : "#fee2e2";
                    var netLabel  = data.IsProfit ? "Utilidad Neta" : "Pérdida Neta";

                    col.Item().Background(netBg).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text(netLabel).Bold().FontSize(12).FontColor(netColor);
                        row.AutoItem().Text(Acct(data.NetIncome, s.CurrencySymbol)).Bold().FontSize(12).FontColor(netColor);
                    });
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    // ── Balance Sheet PDF ─────────────────────────────────────────────────────

    private static byte[] BalanceSheetPdf(BalanceSheetDto data, OrgSettingsDto s)
    {
        var theme  = GetTheme(s.Theme);
        var period = $"Al {data.AsOf:dd/MM/yyyy}";

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);

                page.Header().Element(c => Header(c, s, theme, "Balance General", period));
                page.Content().PaddingTop(12).Column(col =>
                {
                    void Group(BalanceSheetGroupDto group)
                    {
                        col.Item().Background(theme.AccentBg).Padding(6)
                           .Text(group.Title).Bold().FontSize(10).FontColor(theme.AccentFg);

                        foreach (var section in group.Sections)
                        {
                            if (section.Lines.Count == 0) continue;

                            col.Item().PaddingLeft(8).PaddingTop(4)
                               .Text(section.SectionName).Bold().FontSize(9).FontColor("#475569");

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(5);
                                    c.RelativeColumn(2);
                                });

                                foreach (var line in section.Lines)
                                {
                                    table.Cell().Padding(3).PaddingLeft(16).Text(line.Code).FontSize(8);
                                    table.Cell().Padding(3).Text(line.Name).FontSize(8);
                                    table.Cell().Padding(3).AlignRight()
                                         .Text(Acct(line.Balance, s.CurrencySymbol)).FontSize(8);
                                }
                            });

                            col.Item().AlignRight().Background("#f1f5f9").Padding(4)
                               .Text($"Subtotal {section.SectionName}: {Acct(section.Subtotal, s.CurrencySymbol)}")
                               .FontSize(8).Bold();
                        }

                        col.Item().Background("#e2e8f0").Padding(5).AlignRight()
                           .Text($"Total {group.Title}: {Acct(group.Total, s.CurrencySymbol)}")
                           .Bold().FontSize(9);
                        col.Item().Height(8);
                    }

                    Group(data.Assets);
                    Group(data.Liabilities);
                    Group(data.Equity);

                    // Net income line inside equity
                    col.Item().PaddingLeft(8).Background("#f8fafc").Padding(4).Row(row =>
                    {
                        row.RelativeItem().Text("Utilidad / Pérdida acumulada").FontSize(8).FontColor("#475569");
                        row.AutoItem().Text(Acct(data.NetIncome, s.CurrencySymbol)).FontSize(8);
                    });

                    col.Item().Background("#e2e8f0").Padding(5).AlignRight()
                       .Text($"Total Capital + Resultado: {Acct(data.TotalEquity, s.CurrencySymbol)}")
                       .Bold().FontSize(9);

                    col.Item().Height(12);

                    // Verification
                    var balanced = data.IsBalanced;
                    col.Item().Background(balanced ? "#dcfce7" : "#fee2e2").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Verificación de ecuación contable").Bold().FontSize(9)
                                    .FontColor(balanced ? "#14532d" : "#991b1b");
                            c.Item().Text($"Activos: {Acct(data.Assets.Total, s.CurrencySymbol)}  |  " +
                                          $"Pasivos + Capital: {Acct(data.TotalLiabilitiesAndEquity, s.CurrencySymbol)}")
                                    .FontSize(8).FontColor(balanced ? "#14532d" : "#991b1b");
                        });
                        row.AutoItem().AlignMiddle()
                           .Text(balanced ? "CUADRADO" : "DESCUADRADO")
                           .Bold().FontSize(11).FontColor(balanced ? "#14532d" : "#991b1b");
                    });
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    // ── Cash Flow PDF ─────────────────────────────────────────────────────────

    private static byte[] CashFlowPdf(CashFlowDto data, OrgSettingsDto s)
    {
        var theme  = GetTheme(s.Theme);
        var period = $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}";
        var sym    = s.CurrencySymbol;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Element(c => Header(c, s, theme, "Estado de Flujo de Efectivo", period));
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().PaddingBottom(4).Text("Método Indirecto")
                       .FontSize(9).Italic().FontColor("#64748b");

                    // Summary row
                    col.Item().Background(theme.AccentBg).Padding(6).Row(row =>
                    {
                        row.RelativeItem().Text("Saldo inicial de efectivo").Bold().FontSize(9).FontColor(theme.AccentFg);
                        row.AutoItem().Text(Acct(data.BeginningCash, sym)).Bold().FontSize(9).FontColor(theme.AccentFg);
                    });
                    col.Item().Height(8);

                    void Section(CashFlowSectionDto sec, bool showNetIncome)
                    {
                        col.Item().Background(theme.AccentBg).Padding(6)
                           .Text(sec.Title).Bold().FontSize(10).FontColor(theme.AccentFg);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(5); c.RelativeColumn(2); });

                            if (showNetIncome)
                            {
                                table.Cell().Padding(4).Text("Utilidad / Pérdida neta del período").FontSize(8);
                                table.Cell().Padding(4).AlignRight()
                                     .Text(Acct(sec.NetIncome, sym)).FontSize(8);
                            }

                            bool alt = false;
                            foreach (var line in sec.Lines)
                            {
                                var bg = alt ? "#f8fafc" : "#ffffff";
                                alt = !alt;
                                table.Cell().Background(bg).Padding(4).PaddingLeft(showNetIncome ? 16 : 4)
                                     .Text($"{line.AccountCode} — {line.AccountName}").FontSize(8);
                                table.Cell().Background(bg).Padding(4).AlignRight()
                                     .Text(line.Amount >= 0 ? Acct(line.Amount, sym) : $"({Acct(Math.Abs(line.Amount), sym)})")
                                     .FontSize(8).FontColor(line.Amount < 0 ? "#991b1b" : "#0f172a");
                            }
                        });

                        col.Item().Background("#e2e8f0").Padding(5).AlignRight()
                           .Text($"Total {sec.Title}: {Acct(sec.Total, sym)}").Bold().FontSize(9);
                        col.Item().Height(8);
                    }

                    Section(data.Operating, showNetIncome: true);
                    Section(data.Investing,  showNetIncome: false);
                    Section(data.Financing,  showNetIncome: false);

                    // Reconciliation
                    var balanced = Math.Abs(data.BeginningCash + data.NetChange - data.EndingCash) < 0.01m;
                    col.Item().Background(balanced ? "#dcfce7" : "#fee2e2").Padding(8).Column(c =>
                    {
                        c.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Variación neta del período").Bold().FontSize(9)
                               .FontColor(balanced ? "#14532d" : "#991b1b");
                            row.AutoItem().Text(Acct(data.NetChange, sym)).Bold().FontSize(9)
                               .FontColor(balanced ? "#14532d" : "#991b1b");
                        });
                        c.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Saldo final de efectivo").Bold().FontSize(11)
                               .FontColor(balanced ? "#14532d" : "#991b1b");
                            row.AutoItem().Text(Acct(data.EndingCash, sym)).Bold().FontSize(11)
                               .FontColor(balanced ? "#14532d" : "#991b1b");
                        });
                    });
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    // ── CSV generators ────────────────────────────────────────────────────────

    private static byte[] TrialBalanceCsv(TrialBalanceDto data, OrgSettingsDto s)
    {
        var rows = new List<string[]>
        {
            new[] { s.CompanyName, "Balance de Comprobación", $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}" },
            Array.Empty<string>(),
            new[] { "Código", "Cuenta", "Tipo", "Débito Total", "Crédito Total", "Saldo Deudor", "Saldo Acreedor" },
        };
        rows.AddRange(data.Lines.Select(l => new[]
        {
            l.Code, l.Name, l.Type.ToString(),
            l.TotalDebit.ToString("N2"), l.TotalCredit.ToString("N2"),
            l.DebitBalance.ToString("N2"), l.CreditBalance.ToString("N2")
        }));
        rows.Add(new[] { "TOTALES", "", "",
            data.TotalDebit.ToString("N2"), data.TotalCredit.ToString("N2"),
            data.TotalDebitBalance.ToString("N2"), data.TotalCreditBalance.ToString("N2") });
        return ToCsv(rows);
    }

    private static byte[] IncomeStatementCsv(IncomeStatementDto data, OrgSettingsDto s)
    {
        var rows = new List<string[]>
        {
            new[] { s.CompanyName, "Estado de Resultados", $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}" },
            Array.Empty<string>(),
            new[] { "Tipo", "Código", "Cuenta", $"Monto ({s.CurrencySymbol})" },
        };
        foreach (var l in data.Income.Lines)
            rows.Add(new[] { "Ingreso", l.Code, l.Name, l.Amount.ToString("N2") });
        rows.Add(new[] { "TOTAL INGRESOS", "", "", data.Income.Total.ToString("N2") });
        rows.Add(Array.Empty<string>());
        foreach (var l in data.Expenses.Lines)
            rows.Add(new[] { "Gasto", l.Code, l.Name, l.Amount.ToString("N2") });
        rows.Add(new[] { "TOTAL GASTOS", "", "", data.Expenses.Total.ToString("N2") });
        rows.Add(Array.Empty<string>());
        rows.Add(new[] { data.IsProfit ? "UTILIDAD NETA" : "PÉRDIDA NETA", "", "", data.NetIncome.ToString("N2") });
        return ToCsv(rows);
    }

    private static byte[] BalanceSheetCsv(BalanceSheetDto data, OrgSettingsDto s)
    {
        var rows = new List<string[]>
        {
            new[] { s.CompanyName, "Balance General", $"Al {data.AsOf:dd/MM/yyyy}" },
            Array.Empty<string>(),
            new[] { "Grupo", "Sección", "Código", "Cuenta", $"Saldo ({s.CurrencySymbol})" },
        };

        void AddGroup(BalanceSheetGroupDto g)
        {
            foreach (var sec in g.Sections)
                foreach (var l in sec.Lines)
                    rows.Add(new[] { g.Title, sec.SectionName, l.Code, l.Name, l.Balance.ToString("N2") });
            rows.Add(new[] { $"TOTAL {g.Title.ToUpper()}", "", "", "", g.Total.ToString("N2") });
            rows.Add(Array.Empty<string>());
        }

        AddGroup(data.Assets);
        AddGroup(data.Liabilities);
        AddGroup(data.Equity);
        rows.Add(new[] { "Utilidad/Pérdida acumulada", "", "", "", data.NetIncome.ToString("N2") });
        rows.Add(new[] { "TOTAL CAPITAL + RESULTADO", "", "", "", data.TotalEquity.ToString("N2") });
        rows.Add(Array.Empty<string>());
        rows.Add(new[] { "TOTAL PASIVO + CAPITAL", "", "", "", data.TotalLiabilitiesAndEquity.ToString("N2") });
        return ToCsv(rows);
    }

    private static byte[] CashFlowCsv(CashFlowDto data, OrgSettingsDto s)
    {
        var rows = new List<string[]>
        {
            new[] { s.CompanyName, "Estado de Flujo de Efectivo", $"Del {data.From:dd/MM/yyyy} al {data.To:dd/MM/yyyy}", "Método Indirecto" },
            Array.Empty<string>(),
            new[] { "Saldo inicial de efectivo", "", data.BeginningCash.ToString("N2") },
            Array.Empty<string>(),
        };

        void AddSection(CashFlowSectionDto sec, bool showNetIncome)
        {
            rows.Add(new[] { sec.Title });
            if (showNetIncome)
                rows.Add(new[] { "Utilidad / Pérdida neta del período", "", sec.NetIncome.ToString("N2") });
            foreach (var l in sec.Lines)
                rows.Add(new[] { $"  {l.AccountCode}", l.AccountName, l.Amount.ToString("N2") });
            rows.Add(new[] { $"Total {sec.Title}", "", sec.Total.ToString("N2") });
            rows.Add(Array.Empty<string>());
        }

        AddSection(data.Operating, showNetIncome: true);
        AddSection(data.Investing,  showNetIncome: false);
        AddSection(data.Financing,  showNetIncome: false);
        rows.Add(new[] { "Variación neta del período", "", data.NetChange.ToString("N2") });
        rows.Add(new[] { "Saldo final de efectivo",   "", data.EndingCash.ToString("N2") });
        return ToCsv(rows);
    }

    private static byte[] ToCsv(IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sep=,");
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(QuoteCsv)));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string QuoteCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ── Invoice PDF ───────────────────────────────────────────────────────────

    public byte[] GenerateInvoicePdf(InvoiceDto inv, OrgSettingsDto s)
    {
        var cur    = s.CurrencySymbol;
        var accent = "#4f46e5";
        var light  = "#e0e7ff";
        var gray   = "#64748b";

        string Money(decimal v) => $"{cur} {v:N2}";

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);

                // ── Header ──────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(s.CompanyName).Bold().FontSize(16).FontColor("#0f172a");
                            if (s.TaxId    is not null) c.Item().Text($"NIT/RUC: {s.TaxId}").FontSize(8).FontColor(gray);
                            if (s.Address  is not null) c.Item().Text(s.Address).FontSize(8).FontColor(gray);
                            if (s.Phone    is not null) c.Item().Text(s.Phone).FontSize(8).FontColor(gray);
                            if (s.Email    is not null) c.Item().Text(s.Email).FontSize(8).FontColor(gray);
                        });
                        row.ConstantItem(160).Background(accent).Padding(12).Column(c =>
                        {
                            var typeLabel = inv.Type == Accounting.Domain.Enums.InvoiceType.Receivable
                                ? "FACTURA" : "FACTURA DE COMPRA";
                            c.Item().Text(typeLabel).Bold().FontSize(14).FontColor("#ffffff");
                            c.Item().Text($"No. {inv.Number}").FontSize(10).FontColor("#c7d2fe");
                            c.Item().PaddingTop(4).Text($"Fecha: {inv.Date}").FontSize(8).FontColor("#e0e7ff");
                            c.Item().Text($"Vence: {inv.DueDate}").FontSize(8).FontColor("#e0e7ff");
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(accent);
                });

                // ── Content ──────────────────────────────────────────────────
                page.Content().PaddingTop(16).Column(col =>
                {
                    // Contact info
                    col.Item().Background(light).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FACTURAR A:").Bold().FontSize(8).FontColor(accent);
                            c.Item().Text(inv.ContactName).Bold().FontSize(11).FontColor("#0f172a");
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().Text("ESTADO:").Bold().FontSize(8).FontColor(accent);
                            c.Item().Text(inv.StatusLabel).Bold().FontSize(11).FontColor("#0f172a");
                        });
                    });

                    col.Item().PaddingTop(16);

                    // Lines table
                    col.Item().Table(table =>
                    {
                        var hasTax = inv.Lines.Any(l => l.TaxRatePercent > 0);
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.5f);
                            if (hasTax) c.RelativeColumn(1.2f);
                            if (hasTax) c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.8f);
                        });

                        void Th(string t, bool right = false)
                            => TableHeaderCell(table.Cell(), t, accent, "#ffffff", right);

                        Th("Descripción");
                        Th("Cant.", true);
                        Th("Precio unit.", true);
                        if (hasTax) { Th("Impuesto", true); Th("Monto imp.", true); }
                        Th("Total línea", true);

                        foreach (var l in inv.Lines)
                        {
                            void Td(string t, bool right = false, bool bold = false)
                            {
                                var cell = table.Cell().BorderBottom(1).BorderColor("#f1f5f9").Padding(5);
                                var txt  = (right ? cell.AlignRight() : cell).Text(t);
                                if (bold) txt.Bold(); else txt.FontSize(9);
                            }
                            Td(l.Description);
                            Td($"{l.Quantity:N2}", true);
                            Td(Money(l.UnitPrice), true);
                            if (hasTax)
                            {
                                Td(l.TaxRatePercent > 0 ? $"{l.TaxRateName} ({l.TaxRatePercent}%)" : "—", true);
                                Td(l.TaxAmount > 0 ? Money(l.TaxAmount) : "—", true);
                            }
                            Td(Money(l.LineTotal), true);
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(12).AlignRight().Column(tot =>
                    {
                        void TotRow(string label, decimal value, bool highlight = false)
                        {
                            tot.Item().Width(240).Row(r =>
                            {
                                var bg = highlight ? accent : "#f8fafc";
                                var fg = highlight ? "#ffffff" : "#0f172a";
                                r.RelativeItem().Background(bg).PaddingHorizontal(10).PaddingVertical(5)
                                    .Text(label).FontSize(9).FontColor(fg);
                                r.ConstantItem(100).Background(bg).PaddingHorizontal(10).PaddingVertical(5)
                                    .AlignRight().Text(Money(value)).Bold().FontSize(9).FontColor(fg);
                            });
                        }

                        if (inv.TaxTotal > 0)
                        {
                            TotRow("Subtotal", inv.SubTotal);
                            TotRow("Impuesto", inv.TaxTotal);
                        }
                        TotRow("TOTAL", inv.Total, highlight: true);
                        if (inv.Paid > 0)
                        {
                            TotRow("Pagado", inv.Paid);
                            TotRow("Saldo", inv.Balance, highlight: inv.Balance > 0);
                        }
                    });

                    // Notes
                    if (inv.Notes is not null)
                    {
                        col.Item().PaddingTop(16).Background("#f8fafc").Padding(10).Column(c =>
                        {
                            c.Item().Text("NOTAS:").Bold().FontSize(8).FontColor(gray);
                            c.Item().Text(inv.Notes).FontSize(9).FontColor("#374151");
                        });
                    }
                });

                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }
}
