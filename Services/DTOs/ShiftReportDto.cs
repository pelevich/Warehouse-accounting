namespace Services.DTO;

public sealed record ShiftReportDto
{
    public Guid ShiftId { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeFullName { get; init; } = string.Empty;

    public DateTime OpenedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public bool IsOpen => ClosedAt == null;

    public int SalesCount { get; init; }
    public int CashSalesCount { get; init; }
    public int CardSalesCount { get; init; }
    public int ReceiptCount { get; init; }
    public int SoldUnits { get; init; }
    public int ReceivedUnits { get; init; }

    public decimal TotalRevenue { get; init; }
    public decimal CashRevenue { get; init; }
    public decimal CardRevenue { get; init; }
}
