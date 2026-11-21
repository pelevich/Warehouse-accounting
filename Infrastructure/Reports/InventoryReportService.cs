using Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using System.Globalization;
using PdfDocument = QuestPDF.Fluent.Document;
using QuestPDF.Drawing;
using System.IO;

namespace Infrastructure.Reports;

/// <summary>
/// Формирует отдельный PDF-отчёт по одной смене.
/// Данные берутся только через WorkShiftRepository.GetForReportAsync(shiftId).
/// </summary>
public sealed class InventoryReportService : IInventoryReportService
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private readonly IUnitOfWork _unitOfWork;

    static InventoryReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        //var fontPath = Path.Combine(
        //    AppContext.BaseDirectory,
        //    "QuestPDFFonts");

        //using var regular = File.OpenRead(
        //    Path.Combine(fontPath, "NotoSans-Regular.ttf"));

        //FontManager.RegisterFont(regular);

        //using var bold = File.OpenRead(
        //    Path.Combine(fontPath, "NotoSans-Bold.ttf"));

        //FontManager.RegisterFont(bold);
    }

    public InventoryReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GenerateShiftPdfAsync(
        Guid shiftId,
        CancellationToken ct = default)
    {
        if (shiftId == Guid.Empty)
            throw new ArgumentException("Не указан идентификатор смены.", nameof(shiftId));

        var shift = await _unitOfWork.WorkShiftRepository.GetForReportAsync(shiftId, ct)
            ?? throw new InvalidOperationException("Смена не найдена.");

        ct.ThrowIfCancellationRequested();

        var sales = shift.Sales
            .OrderBy(sale => sale.SoldAt)
            .ToArray();

        // В отчёт попадают только проведённые приходы: ReceiptService устанавливает
        // SoldAt при успешном проведении документа.
        var receipts = shift.Receipts
            .Where(receipt => receipt.SoldAt != default)
            .OrderBy(receipt => receipt.SoldAt)
            .ToArray();

        var soldUnits = sales.Sum(sale => sale.Items?.Sum(item => item.Quantity) ?? 0);
        var receivedUnits = receipts.Sum(receipt => receipt.Items?.Sum(item => item.Quantity) ?? 0);
        var totalRevenue = sales.Sum(GetSaleTotal);
        var cashRevenue = sales
            .Where(sale => IsCashPayment(sale.PaymentType))
            .Sum(GetSaleTotal);
        var cardRevenue = sales
            .Where(sale => IsCardPayment(sale.PaymentType))
            .Sum(GetSaleTotal);

        var employeeName = GetEmployeeName(shift.Employee);
        var generatedAt = DateTime.Now;
        var reportKind = shift.ClosedAt.HasValue
            ? "ИТОГОВЫЙ ОТЧЁТ"
            : "ПРОМЕЖУТОЧНЫЙ ОТЧЁТ";

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily("Times New Roman")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken4));

                page.Header().PaddingBottom(12).Column(header =>
                {
                    header.Item()
                        .Text("ОТЧЁТ ПО СМЕНЕ")
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    header.Item()
                        .PaddingTop(3)
                        .Text(reportKind)
                        .FontSize(10)
                        .Bold()
                        .FontColor(shift.ClosedAt.HasValue ? Colors.Green.Darken2 : Colors.Orange.Darken2);

                    header.Item().PaddingTop(8).Text($"Сотрудник: {employeeName}");
                    header.Item().Text($"Открыта: {LocalDateText(shift.OpenedAt)}");
                    header.Item().Text(shift.ClosedAt.HasValue
                        ? $"Закрыта: {LocalDateText(shift.ClosedAt.Value)}"
                        : "Закрыта: смена ещё открыта");
                    header.Item().Text($"Сформирован: {generatedAt:dd.MM.yyyy HH:mm}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(13);
                    ComposeSales(content, sales);
                    ComposeReceipts(content, receipts);
                    ComposeTotals(
                        content,
                        sales.Length,
                        receipts.Length,
                        soldUnits,
                        receivedUnits,
                        totalRevenue,
                        cashRevenue,
                        cardRevenue);
                });

                page.Footer()
                    .PaddingTop(8)
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Страница ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });
            });
        });

        return await Task.Run(() => document.GeneratePdf(), ct);
    }

    private static void ComposeSales(
        ColumnDescriptor content,
        IReadOnlyList<Sale> sales)
    {
        content.Item().Text("1. ПРОДАЖИ").FontSize(13).Bold();

        if (sales.Count == 0)
        {
            content.Item()
                .Text("В этой смене продаж нет.")
                .FontColor(Colors.Grey.Darken1);
            return;
        }

        foreach (var sale in sales)
        {
            var items = sale.Items ?? new List<SaleItem>();
            var saleTotal = GetSaleTotal(sale);

            content.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(9)
                .Column(card =>
                {
                    card.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"Чек № {ShortId(sale.Id)}")
                            .Bold();

                        row.ConstantItem(135)
                            .AlignRight()
                            .Text(LocalDateText(sale.SoldAt))
                            .FontColor(Colors.Grey.Darken2);
                    });

                    card.Item()
                        .PaddingTop(3)
                        .Text($"Способ оплаты: {PaymentTypeText(sale.PaymentType)}")
                        .FontColor(Colors.Grey.Darken2);

                    if (items.Count == 0)
                    {
                        card.Item().PaddingTop(6).Text("В чеке нет товарных позиций.").Italic();
                    }
                    else
                    {
                        card.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(48);
                                columns.ConstantColumn(76);
                                columns.ConstantColumn(82);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Товар").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Кол-во").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Цена").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Сумма").Bold();
                            });

                            foreach (var item in items)
                            {
                                var unitPrice = item.Product?.Price ?? 0m;
                                var lineTotal = unitPrice * item.Quantity;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(item.Product?.Name ?? "Неизвестный товар");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text(Number(item.Quantity));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text(Money(unitPrice));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text(Money(lineTotal));
                            }
                        });
                    }

                    card.Item()
                        .PaddingTop(6)
                        .AlignRight()
                        .Text($"Итого по чеку: {Money(saleTotal)}")
                        .Bold();
                });
        }
    }

    private static void ComposeReceipts(
        ColumnDescriptor content,
        IReadOnlyList<Receipt> receipts)
    {
        content.Item().Text("2. ПРИХОДЫ").FontSize(13).Bold();

        if (receipts.Count == 0)
        {
            content.Item()
                .Text("В этой смене проведённых приходов нет.")
                .FontColor(Colors.Grey.Darken1);
            return;
        }

        foreach (var receipt in receipts)
        {
            var items = receipt.Items ?? new List<ReceiptItem>();
            var units = items.Sum(item => item.Quantity);

            content.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(9)
                .Column(card =>
                {
                    card.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"Документ прихода № {ShortId(receipt.Id)}")
                            .Bold();

                        row.ConstantItem(135)
                            .AlignRight()
                            .Text(LocalDateText(receipt.SoldAt))
                            .FontColor(Colors.Grey.Darken2);
                    });

                    if (items.Count == 0)
                    {
                        card.Item().PaddingTop(6).Text("В приходе нет товарных позиций.").Italic();
                    }
                    else
                    {
                        card.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(65);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Товар").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Кол-во").Bold();
                            });

                            foreach (var item in items)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(item.Product?.Name ?? "Неизвестный товар");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text(Number(item.Quantity));
                            }
                        });
                    }

                    card.Item()
                        .PaddingTop(6)
                        .AlignRight()
                        .Text($"Всего принято: {Number(units)} шт.")
                        .Bold();
                });
        }
    }

    private static void ComposeTotals(
        ColumnDescriptor content,
        int salesCount,
        int receiptsCount,
        int soldUnits,
        int receivedUnits,
        decimal totalRevenue,
        decimal cashRevenue,
        decimal cardRevenue)
    {
        content.Item()
            .PaddingTop(4)
            .BorderTop(1)
            .BorderColor(Colors.Grey.Darken2)
            .PaddingTop(10)
            .Column(summary =>
            {
                summary.Item().Text("3. ИТОГИ СМЕНЫ").FontSize(13).Bold();

                summary.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text($"Чеков продаж: {Number(salesCount)}");
                    row.RelativeItem().Text($"Документов прихода: {Number(receiptsCount)}");
                });

                summary.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Продано товаров: {Number(soldUnits)} шт.").Bold();
                    row.RelativeItem().Text($"Принято товаров: {Number(receivedUnits)} шт.").Bold();
                });

                summary.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Наличными: {Money(cashRevenue)}");
                    row.RelativeItem().Text($"Безналичными: {Money(cardRevenue)}");
                });

                summary.Item()
                    .PaddingTop(8)
                    .Background(Colors.Grey.Lighten4)
                    .Padding(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Text("ОБЩАЯ СУММА ПРОДАЖ").Bold();
                        row.ConstantItem(150)
                            .AlignRight()
                            .Text(Money(totalRevenue))
                            .FontSize(13)
                            .Bold();
                    });
            });
    }

    private static decimal GetSaleTotal(Sale sale) =>
        sale.Items?.Sum(item => item.FullPrice) ?? 0m;

    private static bool IsCashPayment(string? paymentType) =>
        paymentType?.Contains("Наличный", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsCardPayment(string? paymentType) =>
        paymentType?.Contains("Безналичный", StringComparison.OrdinalIgnoreCase) == true;

    private static string PaymentTypeText(string? paymentType) =>
        string.IsNullOrWhiteSpace(paymentType) ? "Не указан" : paymentType;

    private static string GetEmployeeName(Employee? employee)
    {
        if (employee is null)
            return "Не указан";

        var fullName = string.Join(" ", new[]
        {
            employee.LastName,
            employee.FirstName,
            employee.MiddleName
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? "Не указан" : fullName;
    }

    private static DateTime ToLocalTime(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Utc => value.ToLocalTime(),
        // SQLite может вернуть DateTime без Kind. В проекте время записывается через UtcNow.
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
    };

    private static string LocalDateText(DateTime value) =>
        ToLocalTime(value).ToString("dd.MM.yyyy HH:mm", Ru);

    private static string Money(decimal value) =>
        $"{value.ToString("N2", Ru)} ₽";

    private static string Number(int value) =>
        value.ToString("N0", Ru);

    private static string ShortId(Guid id) =>
        id.ToString("N")[..8].ToUpperInvariant();
}
