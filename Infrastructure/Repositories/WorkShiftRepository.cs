using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories;

public sealed class WorkShiftRepository : IWorkShiftRepository
{
    private readonly AppDbContext _context;

    public WorkShiftRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<WorkShift?> GetOpenAsync(CancellationToken ct = default)
    {
        return _context.WorkShifts
            .AsNoTracking()
            .Include(s => s.Employee)
            .SingleOrDefaultAsync(s => s.ClosedAt == null, ct);
    }

    // Tracking нужен: сервис изменяет ClosedAt и UnitOfWork сохраняет изменение.
    public Task<WorkShift?> GetByIdForUpdateAsync(
        Guid id, CancellationToken ct = default)
    {
        return _context.WorkShifts
            .SingleOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<WorkShift?> GetForReportAsync(
        Guid id, CancellationToken ct = default)
    {
        return _context.WorkShifts
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Employee)
            .Include(s => s.Sales)
                .ThenInclude(s => s.Items)
            .Include(s => s.Receipts)
                .ThenInclude(r => r.Items)
            .SingleOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<WorkShift>> GetRecentAsync(
        int limit = 50, CancellationToken ct = default)
    {
        if (limit < 1 || limit > 500)
            throw new ArgumentOutOfRangeException(
                nameof(limit), "Допустимый лимит: от 1 до 500.");

        return await _context.WorkShifts
            .AsNoTracking()
            .Include(s => s.Employee)
            .OrderByDescending(s => s.OpenedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    // SaveChanges делает UnitOfWork.CommitAsync.
    public async Task AddAsync(WorkShift shift, CancellationToken ct = default)
    {
        await _context.WorkShifts.AddAsync(shift, ct);
    }
}
