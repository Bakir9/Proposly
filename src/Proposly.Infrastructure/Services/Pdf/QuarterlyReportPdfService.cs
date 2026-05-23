using Proposly.Application.Reports.Responses;
using Proposly.Application.Reports.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proposly.Infrastructure.Services.Pdf;

public sealed class QuarterlyReportPdfService : IReportPdfService
{
    private static readonly string[] MonthNames =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    public byte[] GenerateQuarterlyReportPdf(QuarterlyReportResponse r)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── Header ──────────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("QUARTERLY FINANCIAL REPORT")
                                .FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Q{r.Quarter} {r.FiscalYear}  ·  {r.StartDate:dd MMM yyyy} – {r.EndDate:dd MMM yyyy}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Currency: {r.Currency}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── KPI Summary ──────────────────────────────────────────────
                    col.Item().PaddingBottom(4).Text("Key Metrics").FontSize(12).SemiBold();
                    col.Item().PaddingBottom(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                        });

                        static IContainer KpiCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten4)
                             .PaddingHorizontal(12).PaddingVertical(10)
                             .Border(1).BorderColor(Colors.Grey.Lighten3);

                        void KpiItem(string label, string value, string? sub = null)
                        {
                            table.Cell().PaddingHorizontal(6).PaddingVertical(5).Element(KpiCell).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium).Bold();
                                c.Item().PaddingTop(3).Text(value).FontSize(13).Bold();
                                if (sub != null)
                                    c.Item().PaddingTop(2).Text(sub).FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        var totalCosts = r.LaborCost + r.TotalExpenses;
                        KpiItem("REVENUE", Fmt(r.OffersAcceptedValue, r.Currency),
                            $"{r.OffersAcceptedCount} accepted offer{(r.OffersAcceptedCount != 1 ? "s" : "")}");
                        KpiItem("TOTAL COSTS", Fmt(totalCosts, r.Currency),
                            $"{r.TotalHoursWorked:F0}h labor + {Fmt(r.TotalExpenses, r.Currency)} expenses");
                        KpiItem("GROSS PROFIT", Fmt(r.GrossProfit, r.Currency),
                            $"{r.ProfitMargin:F1}% margin");
                        KpiItem("CONVERSION RATE", $"{r.ConversionRate:F1}%",
                            $"{r.OffersCreatedCount} offers created");
                        KpiItem("LABOR COST", Fmt(r.LaborCost, r.Currency),
                            $"{r.TotalHoursWorked:F0} hours worked");
                        KpiItem("TOTAL EXPENSES", Fmt(r.TotalExpenses, r.Currency),
                            $"{r.ExpensesByCategory.Count} categories");
                    });

                    // ── Offer Pipeline ───────────────────────────────────────────
                    col.Item().PaddingBottom(4).Text("Offer Pipeline").FontSize(12).SemiBold();
                    col.Item().PaddingBottom(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(60);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(6);
                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Status").SemiBold();
                            h.Cell().Element(HeaderCell).AlignCenter().Text("Count").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Value").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("% of Total").SemiBold();
                        });

                        var totalCreated = r.OffersCreatedValue;
                        var rows = new[]
                        {
                            ("Created",  r.OffersCreatedCount,  r.OffersCreatedValue),
                            ("Accepted", r.OffersAcceptedCount, r.OffersAcceptedValue),
                            ("Rejected", r.OffersRejectedCount, r.OffersRejectedValue),
                            ("Expired",  r.OffersExpiredCount,  r.OffersExpiredValue),
                        };

                        foreach (var (status, count, value) in rows)
                        {
                            var pct = totalCreated > 0 ? value / totalCreated * 100 : 0;
                            table.Cell().Element(DataCell).Text(status);
                            table.Cell().Element(DataCell).AlignCenter().Text(count.ToString());
                            table.Cell().Element(DataCell).AlignRight().Text(Fmt(value, r.Currency));
                            table.Cell().Element(DataCell).AlignRight().Text($"{pct:F1}%");
                        }
                    });

                    // ── Cost Breakdown ───────────────────────────────────────────
                    col.Item().PaddingBottom(4).Text("Cost Breakdown").FontSize(12).SemiBold();
                    col.Item().PaddingBottom(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(6);
                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Category").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Amount").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("% of Costs").SemiBold();
                        });

                        var totalCosts = r.LaborCost + r.TotalExpenses;

                        // Labor row
                        var laborPct = totalCosts > 0 ? r.LaborCost / totalCosts * 100 : 0;
                        table.Cell().Element(DataCell).Text("Labor");
                        table.Cell().Element(DataCell).AlignRight().Text(Fmt(r.LaborCost, r.Currency));
                        table.Cell().Element(DataCell).AlignRight().Text($"{laborPct:F1}%");

                        foreach (var e in r.ExpensesByCategory)
                        {
                            var pct = totalCosts > 0 ? e.Amount / totalCosts * 100 : 0;
                            table.Cell().Element(DataCell).Text(e.Category);
                            table.Cell().Element(DataCell).AlignRight().Text(Fmt(e.Amount, r.Currency));
                            table.Cell().Element(DataCell).AlignRight().Text($"{pct:F1}%");
                        }

                        // Total row
                        static IContainer TotalCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten4).Padding(6);
                        table.Cell().Element(TotalCell).Text("Total Costs").Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(Fmt(totalCosts, r.Currency)).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text("100%").Bold();
                    });

                    // ── P&L Summary ──────────────────────────────────────────────
                    col.Item().PaddingBottom(4).Text("P&L Summary").FontSize(12).SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(2);
                        });

                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6);
                        static IContainer TotalCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten4).Padding(8);

                        var rows = new[]
                        {
                            ("Revenue (Accepted Offers)", r.OffersAcceptedValue, false),
                            ("Labor Costs",               -r.LaborCost, false),
                            ("Other Expenses",            -r.TotalExpenses, false),
                        };

                        foreach (var (label, value, _) in rows)
                        {
                            var color = value >= 0 ? Colors.Black : Colors.Red.Medium;
                            table.Cell().Element(DataCell).Text(label);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(value >= 0 ? Fmt(value, r.Currency) : $"−{Fmt(-value, r.Currency)}")
                                .FontColor(color);
                        }

                        // Gross profit
                        var profitColor = r.GrossProfit >= 0 ? Colors.Green.Darken2 : Colors.Red.Medium;
                        table.Cell().Element(TotalCell).Text("Gross Profit").Bold().FontSize(11);
                        table.Cell().Element(TotalCell).AlignRight()
                            .Text(Fmt(r.GrossProfit, r.Currency)).Bold().FontSize(11).FontColor(profitColor);

                        // Margin row
                        table.Cell().Element(DataCell).Text("Profit Margin").FontColor(Colors.Grey.Medium);
                        table.Cell().Element(DataCell).AlignRight()
                            .Text($"{r.ProfitMargin:F2}%").FontColor(Colors.Grey.Medium);
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Q{r.Quarter} {r.FiscalYear} Financial Report  ·  Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static string Fmt(decimal amount, string currency) =>
        $"{amount:N2} {currency}";

    private static string QuarterRange(int quarter, int startMonth)
    {
        var offset = (quarter - 1) * 3;
        var m1 = MonthNames[(startMonth - 1 + offset) % 12];
        var m3 = MonthNames[(startMonth - 1 + offset + 2) % 12];
        return $"{m1}–{m3}";
    }
}
