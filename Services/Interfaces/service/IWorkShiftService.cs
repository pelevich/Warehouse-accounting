using Domain.Entities;
using Services.DTO;

namespace Services.Interfaces.service;

public interface IWorkShiftService
{
    Task<WorkShift?> GetOpenShiftAsync(CancellationToken ct = default);
    Task<WorkShift> OpenShiftAsync(Guid employeeId, CancellationToken ct = default);
    Task<ShiftReportDto> CloseShiftAsync(Guid shiftId, CancellationToken ct = default);
    Task<ShiftReportDto> GetShiftReportAsync(Guid shiftId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkShift>> GetRecentShiftsAsync(
        int limit = 50, CancellationToken ct = default);
}
