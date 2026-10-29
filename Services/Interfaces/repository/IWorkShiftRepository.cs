using Domain.Entities;

namespace Services.Interfaces.repository;

public interface IWorkShiftRepository
{
    Task<WorkShift?> GetOpenAsync(CancellationToken ct = default);
    Task<WorkShift?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<WorkShift?> GetForReportAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkShift>> GetRecentAsync(
        int limit = 50, CancellationToken ct = default);
    Task AddAsync(WorkShift shift, CancellationToken ct = default);
}
