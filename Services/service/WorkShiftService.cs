using Domain.Entities;
using Microsoft.Extensions.Logging;
using Services.DTO;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service;

public sealed class WorkShiftService : IWorkShiftService
{
    // Эти значения соответствуют PaymentType в существующей SaleViewModel.
    private const string CashPayment = "Наличный способ";
    private const string CardPayment = "Безналичный способ";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkShiftService> _logger;

    private IWorkShiftRepository _shifts =>
    _unitOfWork.WorkShiftRepository;

    private IEmployeeRepository _employees =>
        _unitOfWork.EmployeeRepository;

    public WorkShiftService(
        IUnitOfWork unitOfWork,
        ILogger<WorkShiftService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<WorkShift?> GetOpenShiftAsync(CancellationToken ct = default)
        => _shifts.GetOpenAsync(ct);

    public Task<IReadOnlyList<WorkShift>> GetRecentShiftsAsync(
        int limit = 50, CancellationToken ct = default)
        => _shifts.GetRecentAsync(limit, ct);

    public async Task<WorkShift> OpenShiftAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Не указан сотрудник.", nameof(employeeId));

        var employee = await _employees.GetByIdAsync(employeeId, ct)
            ?? throw new InvalidOperationException("Сотрудник не найден.");

        if (!employee.IsActive)
            throw new InvalidOperationException("Сотрудник отключён.");

        if (await _shifts.GetOpenAsync(ct) is not null)
            throw new InvalidOperationException(
                "Сначала необходимо закрыть текущую смену.");

        var shift = new WorkShift
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            OpenedAt = DateTime.UtcNow,
            ClosedAt = null
        };

        await _shifts.AddAsync(shift, ct);

        try
        {
            await _unitOfWork.CommitAsync(ct);
            _logger.LogInformation(
                "Открыта смена {ShiftId} сотрудником {EmployeeId}",
                shift.Id, employeeId);
            return shift;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка открытия смены сотрудником {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<ShiftReportDto> CloseShiftAsync(
        Guid shiftId, CancellationToken ct = default)
    {
        if (shiftId == Guid.Empty)
            throw new ArgumentException("Не указан ID смены.", nameof(shiftId));

        var shift = await _shifts.GetByIdForUpdateAsync(shiftId, ct)
            ?? throw new InvalidOperationException("Смена не найдена.");

        if (shift.ClosedAt is not null)
            throw new InvalidOperationException("Смена уже закрыта.");

        // Вызывающая логика должна исключить параллельное проведение продажи
        // во время закрытия смены.
        shift.ClosedAt = DateTime.UtcNow;
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Закрыта смена {ShiftId}", shiftId);
        return await GetShiftReportAsync(shiftId, ct);
    }

    public async Task<ShiftReportDto> GetShiftReportAsync(
        Guid shiftId, CancellationToken ct = default)
    {
        if (shiftId == Guid.Empty)
            throw new ArgumentException("Не указан ID смены.", nameof(shiftId));

        var shift = await _shifts.GetForReportAsync(shiftId, ct)
            ?? throw new InvalidOperationException("Смена не найдена.");

        var sales = shift.Sales.ToArray();
        var receipts = shift.Receipts.ToArray();

        // ФИО актуально на момент запроса: snapshot в выбранной модели отсутствует.
        var employee = shift.Employee;
        var fullName = string.Join(" ", new[]
        {
            employee.LastName,
            employee.FirstName,
            employee.MiddleName
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new ShiftReportDto
        {
            ShiftId = shift.Id,
            EmployeeId = shift.EmployeeId,
            EmployeeFullName = fullName,
            OpenedAt = shift.OpenedAt,
            ClosedAt = shift.ClosedAt,

            SalesCount = sales.Length,
            CashSalesCount = sales.Count(s => s.PaymentType == CashPayment),
            CardSalesCount = sales.Count(s => s.PaymentType == CardPayment),
            ReceiptCount = receipts.Length,
            SoldUnits = sales.Sum(s => s.Items?.Sum(i => i.Quantity) ?? 0),
            ReceivedUnits = receipts.Sum(r => r.Items?.Sum(i => i.Quantity) ?? 0),

            // Должно быть записано при завершении продажи (Sale.TotalAmount).
            // Ни в коем случае не вычислять из Product.Price.
            TotalRevenue = sales.Sum(s => s.TotalPrice),
            CashRevenue = sales.Where(s => s.PaymentType == CashPayment)
                .Sum(s => s.TotalPrice),
            CardRevenue = sales.Where(s => s.PaymentType == CardPayment)
                .Sum(s => s.TotalPrice)
        };
    }
}
