using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using System.Globalization;

namespace Accounting.Application.Services;

public interface IPeriodService
{
    Task<List<PeriodDto>> ListAsync(Guid orgId, int year, CancellationToken ct = default);
    Task<PeriodDto>       CloseAsync(Guid orgId, Guid userId, ClosePeriodDto dto, CancellationToken ct = default);
    Task                  ReopenAsync(Guid orgId, int year, int month, CancellationToken ct = default);
}

public class PeriodService : IPeriodService
{
    private readonly IAccountingPeriodRepository _repo;

    public PeriodService(IAccountingPeriodRepository repo) => _repo = repo;

    public async Task<List<PeriodDto>> ListAsync(Guid orgId, int year, CancellationToken ct = default)
    {
        var closed   = await _repo.GetClosedForYearAsync(orgId, year, ct);
        var closedMap = closed.ToDictionary(p => p.Month);

        return Enumerable.Range(1, 12).Select(month =>
        {
            closedMap.TryGetValue(month, out var period);
            return new PeriodDto(
                year, month,
                MonthName(month),
                period is not null,
                period is null ? null : $"{period.ClosedBy.FirstName} {period.ClosedBy.LastName}",
                period?.ClosedAtUtc);
        }).ToList();
    }

    public async Task<PeriodDto> CloseAsync(Guid orgId, Guid userId, ClosePeriodDto dto, CancellationToken ct = default)
    {
        if (await _repo.IsClosedAsync(orgId, dto.Year, dto.Month, ct))
            throw new InvalidOperationException(
                $"El período {MonthName(dto.Month)} {dto.Year} ya está cerrado.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Year > today.Year || (dto.Year == today.Year && dto.Month > today.Month))
            throw new InvalidOperationException("No se puede cerrar un período futuro.");

        var period = new AccountingPeriod
        {
            OrganizationId = orgId,
            Year           = dto.Year,
            Month          = dto.Month,
            ClosedByUserId = userId,
            ClosedAtUtc    = DateTime.UtcNow,
        };

        await _repo.AddAsync(period, ct);
        await _repo.SaveChangesAsync(ct);

        var saved = await _repo.GetAsync(orgId, dto.Year, dto.Month, ct);

        return new PeriodDto(
            dto.Year, dto.Month, MonthName(dto.Month), true,
            saved is null ? null : $"{saved.ClosedBy.FirstName} {saved.ClosedBy.LastName}",
            saved?.ClosedAtUtc);
    }

    public async Task ReopenAsync(Guid orgId, int year, int month, CancellationToken ct = default)
    {
        var period = await _repo.GetAsync(orgId, year, month, ct)
            ?? throw new KeyNotFoundException(
                $"El período {MonthName(month)} {year} no está cerrado.");

        _repo.Remove(period);
        await _repo.SaveChangesAsync(ct);
    }

    private static string MonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM", new CultureInfo("es-ES"));
}
