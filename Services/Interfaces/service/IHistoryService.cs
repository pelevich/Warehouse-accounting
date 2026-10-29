using Services.DTO;

namespace Services.Interfaces.service
{
    public interface IHistoryService
    {
        Task<IReadOnlyList<HistoryRecordDto>> GetHistoryAsync(
            HistoryKind kind, CancellationToken ct = default);

        Task<IReadOnlyList<HistoryEmployeeDto>> GetEmployeesAsync(
            CancellationToken ct = default);
    }

    public sealed class HistoryEmployeeDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
    }
}
