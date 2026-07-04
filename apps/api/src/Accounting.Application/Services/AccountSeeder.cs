using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IAccountSeeder
{
    Task SeedAsync(Guid orgId, CancellationToken ct = default);
}

public class AccountSeeder : IAccountSeeder
{
    private readonly IAccountRepository _accounts;

    public AccountSeeder(IAccountRepository accounts) => _accounts = accounts;

    public async Task SeedAsync(Guid orgId, CancellationToken ct = default)
    {
        var entries = BuildCatalog(orgId);
        await _accounts.AddRangeAsync(entries, ct);
    }

    private static List<Account> BuildCatalog(Guid orgId)
    {
        var list = new List<Account>();

        // ── 1  ACTIVOS ──────────────────────────────────────────────
        var activos        = Add(list, orgId, "1",      "Activos",                       AccountType.Asset,     null,      false);
        var actCorriente   = Add(list, orgId, "1.1",    "Activo Corriente",               AccountType.Asset,     activos,   false);
        Add(list, orgId, "1.1.01", "Caja",                              AccountType.Asset,     actCorriente);
        Add(list, orgId, "1.1.02", "Banco",                             AccountType.Asset,     actCorriente);
        Add(list, orgId, "1.1.03", "Cuentas por cobrar",                AccountType.Asset,     actCorriente);
        Add(list, orgId, "1.1.04", "Inventario",                        AccountType.Asset,     actCorriente);
        var actNoCorriente = Add(list, orgId, "1.2",    "Activo No Corriente",            AccountType.Asset,     activos,   false);
        Add(list, orgId, "1.2.01", "Propiedades, planta y equipo",      AccountType.Asset,     actNoCorriente);
        Add(list, orgId, "1.2.02", "Depreciación acumulada",            AccountType.Asset,     actNoCorriente);

        // ── 2  PASIVOS ──────────────────────────────────────────────
        var pasivos        = Add(list, orgId, "2",      "Pasivos",                        AccountType.Liability, null,      false);
        var pasCorriente   = Add(list, orgId, "2.1",    "Pasivo Corriente",               AccountType.Liability, pasivos,   false);
        Add(list, orgId, "2.1.01", "Cuentas por pagar",                 AccountType.Liability, pasCorriente);
        Add(list, orgId, "2.1.02", "Préstamos a corto plazo",           AccountType.Liability, pasCorriente);
        var pasNoCorriente = Add(list, orgId, "2.2",    "Pasivo No Corriente",            AccountType.Liability, pasivos,   false);
        Add(list, orgId, "2.2.01", "Préstamos a largo plazo",           AccountType.Liability, pasNoCorriente);

        // ── 3  CAPITAL ──────────────────────────────────────────────
        var capital        = Add(list, orgId, "3",      "Capital",                        AccountType.Equity,    null,      false);
        Add(list, orgId, "3.1",    "Capital social",                    AccountType.Equity,    capital);
        Add(list, orgId, "3.2",    "Utilidades retenidas",              AccountType.Equity,    capital);
        Add(list, orgId, "3.3",    "Utilidad del ejercicio",            AccountType.Equity,    capital);

        // ── 4  INGRESOS ─────────────────────────────────────────────
        var ingresos       = Add(list, orgId, "4",      "Ingresos",                       AccountType.Income,    null,      false);
        Add(list, orgId, "4.1",    "Ingresos por ventas",               AccountType.Income,    ingresos);
        Add(list, orgId, "4.2",    "Otros ingresos",                    AccountType.Income,    ingresos);

        // ── 5  GASTOS ───────────────────────────────────────────────
        var gastos         = Add(list, orgId, "5",      "Gastos",                         AccountType.Expense,   null,      false);
        var gasOp          = Add(list, orgId, "5.1",    "Gastos Operativos",              AccountType.Expense,   gastos,    false);
        Add(list, orgId, "5.1.01", "Sueldos y salarios",                AccountType.Expense,   gasOp);
        Add(list, orgId, "5.1.02", "Alquiler",                          AccountType.Expense,   gasOp);
        Add(list, orgId, "5.1.03", "Servicios públicos",                AccountType.Expense,   gasOp);
        var gasAdm         = Add(list, orgId, "5.2",    "Gastos Administrativos",         AccountType.Expense,   gastos,    false);
        Add(list, orgId, "5.2.01", "Papelería y útiles",                AccountType.Expense,   gasAdm);
        Add(list, orgId, "5.2.02", "Gastos de representación",          AccountType.Expense,   gasAdm);

        return list;
    }

    private static Account Add(
        List<Account> list,
        Guid orgId,
        string code,
        string name,
        AccountType type,
        Account? parent,
        bool isPostable = true)
    {
        var account = new Account
        {
            OrganizationId = orgId,
            Code           = code,
            Name           = name,
            Type           = type,
            ParentId       = parent?.Id,
            IsPostable     = isPostable
        };
        list.Add(account);
        return account;
    }
}
