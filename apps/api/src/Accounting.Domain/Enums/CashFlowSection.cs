namespace Accounting.Domain.Enums;

public enum CashFlowSection
{
    None       = 0,
    Cash       = 1,   // Caja / Bancos — saldo inicial y final
    Operating  = 2,   // Activos y pasivos corrientes (cuentas por cobrar, inventario, etc.)
    Investing  = 3,   // Activos no corrientes (equipo, propiedad)
    Financing  = 4,   // Préstamos y patrimonio
}
