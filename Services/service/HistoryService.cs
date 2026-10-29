
using Services.DTO;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service;

public sealed class HistoryService : IHistoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public HistoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HistoryEmployeeDto>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var employees = await _unitOfWork
            .EmployeeRepository
            .GetAllAsync(ct);

        return employees
            .Select(e => new HistoryEmployeeDto
            {
                Id = e.Id,
                Name = FullName(
                    e.LastName,
                    e.FirstName,
                    e.MiddleName)
            })
            .OrderBy(e => e.Name)
            .ToArray();
    }

    public async Task<IReadOnlyList<HistoryRecordDto>> GetHistoryAsync(
            HistoryKind kind,
            CancellationToken ct = default)
    {
        if (kind == HistoryKind.Sales)
        {
            var sales = await _unitOfWork
                .SaleRepository
                .GetAllAsync(ct);

            return sales.Select(s => new HistoryRecordDto
            {
                Id = s.Id,
                Kind = HistoryKind.Sales,

                DateUtc = s.SoldAt,

                WorkShiftId = s.WorkShiftId,

                EmployeeId = s.WorkShift?.EmployeeId,

                EmployeeName = GetEmployeeName(
                    s.WorkShift?.Employee),

                TotalUnits = s.Items?
                    .Sum(i => i.Quantity) ?? 0,

                TotalAmount = Convert.ToDecimal(
                    s.TotalPrice),

                PaymentType = s.PaymentType ?? "",

                // В текущей модели Sale нет статуса
                // подтверждения. Все сохранённые
                // продажи считаем проведёнными.
                IsCompleted = true,

                Lines = s.Items?
                    .Select(i => new HistoryLineDto
                    {
                        ProductName =
                            i.Product?.Name ?? "Неизвестный товар",

                        Barcode =
                            i.Product?.Barcode ?? "",

                        Quantity = i.Quantity
                    })
                    .ToArray()
                    ?? Array.Empty<HistoryLineDto>()
            })
            .ToArray();
        }

        if (kind == HistoryKind.Receipts)
        {
            var receipts = await _unitOfWork
                .ReceiptRepository
                .GetAllAsync(ct);

            return receipts.Select(r =>
            {
                // SoldAt заполняется при ConfirmAsync().

                return new HistoryRecordDto
                {
                    Id = r.Id,
                    Kind = HistoryKind.Receipts,

                    DateUtc = r.SoldAt != default
                            ? r.SoldAt
                            : null,

                    WorkShiftId = r.WorkShiftId,

                    EmployeeId =
                        r.WorkShift?.EmployeeId,

                    EmployeeName = GetEmployeeName(
                        r.WorkShift?.Employee),

                    TotalUnits = r.Items?
                        .Sum(i => i.Quantity) ?? 0,

                    TotalAmount = null,

                    PaymentType = "",

                    IsCompleted = true,

                    Lines = r.Items?
                        .Select(i => new HistoryLineDto
                        {
                            ProductName =
                                i.Product?.Name
                                ?? "Неизвестный товар",

                            Barcode =
                                i.Product?.Barcode ?? "",

                            Quantity = i.Quantity
                        })
                        .ToArray()
                        ?? Array.Empty<HistoryLineDto>()
                };
            })
            .ToArray();
        }

        throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "Неизвестный тип истории.");
    }

    private static string GetEmployeeName(
        Domain.Entities.Employee? employee)
    {
        if (employee is null)
            return "Не указан";

        return FullName(
            employee.LastName,
            employee.FirstName,
            employee.MiddleName);
    }

    private static string FullName(
        params string?[] parts)
    {
        return string.Join(
            " ",
            parts.Where(p =>
                !string.IsNullOrWhiteSpace(p)));
    }
}
