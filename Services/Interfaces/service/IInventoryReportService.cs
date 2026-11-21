namespace Services.Interfaces.service;

/// <summary>
/// Формирует PDF-отчёт только по одной конкретной смене.
/// </summary>
public interface IInventoryReportService
{
    Task<byte[]> GenerateShiftPdfAsync(Guid shiftId, CancellationToken ct = default);
}
