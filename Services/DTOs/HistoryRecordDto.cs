namespace Services.DTO;

public enum HistoryKind { Receipts, Sales }

public sealed class HistoryLineDto
{
    public string ProductName { get; init; } = "";
    public string Barcode { get; init; } = "";
    public int Quantity { get; init; }

    // Для продаж цена берётся из Product.Price.
    // Для приходов цена не заполнена: в текущей модели закупочная цена не хранится.
    public decimal? UnitPrice { get; init; }
    public decimal? LineAmount { get; init; }
}

public sealed class HistoryRecordDto
{
    public Guid Id { get; init; }
    public HistoryKind Kind { get; init; }
    public DateTime? DateUtc { get; init; }
    public Guid? EmployeeId { get; init; }
    public string EmployeeName { get; init; } = "Не указан";
    public Guid? WorkShiftId { get; init; }
    public int TotalUnits { get; init; }
    public decimal? TotalAmount { get; init; }
    public string PaymentType { get; init; } = "";
    public bool? IsCompleted { get; init; }
    public IReadOnlyList<HistoryLineDto> Lines { get; init; } = Array.Empty<HistoryLineDto>();
}
