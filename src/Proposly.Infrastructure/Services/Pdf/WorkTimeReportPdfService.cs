using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proposly.Infrastructure.Services.Pdf;

/// <summary>
/// The month-end working time record, modelled on QuarterlyReportPdfService.
/// <para>
/// Content is fixed by FR-029: employee, period, daily records, absence by type, target, actual,
/// the monthly difference, any forfeited surplus, and both balances.
/// </para>
/// </summary>
public sealed class WorkTimeReportPdfService : IWorkTimeReportPdfService
{
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June",
         "July", "August", "September", "October", "November", "December"];

    public byte[] GenerateMonthlyReportPdf(MonthlyWorkTimeReportResponse r)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // A provisional month is stamped, so an unapproved draft cannot be passed off as
                // a final record.
                if (r.IsProvisional)
                {
                    page.Background().AlignCenter().AlignMiddle()
                        .Text("PROVISIONAL")
                        .FontSize(90).Bold().FontColor(Colors.Grey.Lighten3);
                }

                page.Content().Column(col =>
                {
                    Header(col, r);
                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    Summary(col, r);
                    BalanceSection(col, r);
                    AbsenceSection(col, r);
                    BreachSection(col, r);
                    DailyRecords(col, r);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span($"{DateTime.UtcNow:dd MMM yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("  ·  page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static void Header(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("WORKING TIME RECORD")
                    .FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                c.Item().Text($"{r.EmployeeName}  ·  {MonthNames[r.Month - 1]} {r.Year}")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(150).AlignRight().Column(c =>
            {
                c.Item().AlignRight().Text($"Status: {r.Status}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);

                if (r.IsRevised)
                {
                    c.Item().AlignRight().Text("Revised since approval")
                        .FontSize(9).FontColor(Colors.Orange.Darken2);
                }

                if (r.IsSelfApproved)
                {
                    c.Item().AlignRight().Text("Self-approved")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                }
            });
        });
    }

    private static void Summary(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        col.Item().PaddingBottom(4).Text("Summary").FontSize(12).SemiBold();

        col.Item().PaddingBottom(12).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(3);
                cols.RelativeColumn(3);
            });

            static IContainer Cell(IContainer c) =>
                c.Background(Colors.Grey.Lighten4).PaddingHorizontal(12).PaddingVertical(10);

            Kpi(table, Cell, "Target hours",
                r.TargetHours.HasValue ? Hours(r.TargetHours.Value) : "unavailable");

            Kpi(table, Cell, "Actual hours", Hours(r.ActualHours));

            Kpi(table, Cell, "Difference",
                r.MonthlyDifference.HasValue ? Signed(r.MonthlyDifference.Value) : "—");
        });

        if (!r.TargetHours.HasValue)
        {
            col.Item().PaddingBottom(10)
                .Text("No employment terms cover this month, so there is nothing to compare the " +
                      "recorded hours against.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
        }
    }

    private static void BalanceSection(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        col.Item().PaddingBottom(4).Text("Flexitime balance").FontSize(12).SemiBold();

        col.Item().PaddingBottom(6).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
            });

            Line(table, "Opening balance", Signed(r.OpeningBalanceHours));
            Line(table, "This month", r.MonthlyDifference.HasValue
                ? Signed(r.MonthlyDifference.Value) : "—");

            if (r.ForfeitedHours > 0m)
            {
                // Forfeiture is never silent: the employee sees exactly what the cap cost them.
                Line(table, $"Forfeited at the {Hours(r.SurplusCapHours ?? 0m)} cap",
                    $"−{Hours(r.ForfeitedHours)}", Colors.Orange.Darken2);
            }

            Line(table, "Closing balance", Signed(r.ClosingBalanceHours), bold: true);
        });

        if (r.DeficitFloorBreached)
        {
            col.Item().PaddingBottom(10)
                .Text($"The balance is below the agreed floor of {Hours(r.DeficitFloorHours ?? 0m)}. " +
                      "This needs a decision by the employer.")
                .FontSize(9).FontColor(Colors.Red.Darken1);
        }
        else if (r.ApproachingCap)
        {
            col.Item().PaddingBottom(10)
                .Text($"The balance is close to the {Hours(r.SurplusCapHours ?? 0m)} cap. " +
                      "Further surplus will be forfeited unless taken as time off.")
                .FontSize(9).FontColor(Colors.Orange.Darken2);
        }
        else
        {
            col.Item().PaddingBottom(10).Text("").FontSize(1);
        }
    }

    private static void AbsenceSection(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        if (r.AbsenceDays.Count == 0) return;

        col.Item().PaddingBottom(4).Text("Absence").FontSize(12).SemiBold();

        col.Item().PaddingBottom(10).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
            });

            foreach (var absence in r.AbsenceDays)
                Line(table, Humanise(absence.Type), $"{absence.Days:0.#} day(s)");
        });
    }

    private static void BreachSection(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        if (r.Breaches.Count == 0) return;

        col.Item().PaddingBottom(4).Text("Working time issues").FontSize(12).SemiBold();

        col.Item().PaddingBottom(10).Column(c =>
        {
            foreach (var breach in r.Breaches)
            {
                var when = breach.Date?.ToString("dd MMM")
                           ?? (breach.WeekStartDate is { } w ? $"week of {w:dd MMM}" : "—");

                c.Item().Text($"{when}  ·  {Humanise(breach.Kind)} " +
                              $"(recorded {breach.ActualValue:0.##}, limit {breach.LimitValue:0.##})")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);
            }

            c.Item().PaddingTop(3)
                .Text("Hours were recorded as reported. These are flagged for review.")
                .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
        });
    }

    private static void DailyRecords(ColumnDescriptor col, MonthlyWorkTimeReportResponse r)
    {
        col.Item().PaddingBottom(4).Text("Daily records").FontSize(12).SemiBold();

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
            });

            table.Header(header =>
            {
                static IContainer HeaderCell(IContainer c) =>
                    c.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4);

                header.Cell().Element(HeaderCell).Text("Date").SemiBold().FontSize(9);
                header.Cell().Element(HeaderCell).Text("Start").SemiBold().FontSize(9);
                header.Cell().Element(HeaderCell).Text("End").SemiBold().FontSize(9);
                header.Cell().Element(HeaderCell).AlignRight().Text("Break").SemiBold().FontSize(9);
                header.Cell().Element(HeaderCell).AlignRight().Text("Hours").SemiBold().FontSize(9);
            });

            static IContainer BodyCell(IContainer c) =>
                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3);

            foreach (var day in r.Days)
            {
                var suffix = day.CrossesMidnight ? " (+1)" : "";

                table.Cell().Element(BodyCell)
                    .Text($"{day.Date:ddd dd MMM}").FontSize(9);
                table.Cell().Element(BodyCell)
                    .Text($"{day.StartTime:HH\\:mm}").FontSize(9);
                table.Cell().Element(BodyCell)
                    .Text($"{day.EndTime:HH\\:mm}{suffix}").FontSize(9);
                table.Cell().Element(BodyCell).AlignRight()
                    .Text($"{day.BreakMinutes} min").FontSize(9);
                table.Cell().Element(BodyCell).AlignRight()
                    .Text(Hours(day.WorkedHours)).FontSize(9);
            }

            table.Cell().ColumnSpan(4).PaddingTop(5).AlignRight()
                .Text("Total").SemiBold().FontSize(9);
            table.Cell().PaddingTop(5).AlignRight()
                .Text(Hours(r.ActualHours)).SemiBold().FontSize(9);
        });
    }

    // --- helpers ---

    private static void Kpi(
        TableDescriptor table, Func<IContainer, IContainer> cell, string label, string value)
    {
        table.Cell().Element(cell).Column(c =>
        {
            c.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(14).Bold();
        });
    }

    private static void Line(
        TableDescriptor table, string label, string value, string? color = null, bool bold = false)
    {
        var labelCell = table.Cell().PaddingVertical(2).Text(label).FontSize(9);
        if (bold) labelCell.SemiBold();

        var valueCell = table.Cell().PaddingVertical(2).AlignRight().Text(value).FontSize(9);
        if (bold) valueCell.SemiBold();
        if (color is not null) valueCell.FontColor(color);
    }

    private static string Hours(decimal hours) => $"{hours:0.##} h";

    private static string Signed(decimal hours)
        => hours >= 0 ? $"+{hours:0.##} h" : $"{hours:0.##} h";

    private static string Humanise(AbsenceType type) => type switch
    {
        AbsenceType.Vacation => "Vacation",
        AbsenceType.SickLeave => "Sick leave",
        AbsenceType.UnpaidLeave => "Unpaid leave",
        AbsenceType.ParentalLeave => "Parental leave",
        _ => "Special leave"
    };

    private static string Humanise(BreachKind kind) => kind switch
    {
        BreachKind.DailyMaximum => "daily maximum exceeded",
        BreachKind.WeeklyMaximum => "weekly maximum exceeded",
        BreachKind.AveragingWindow => "average over the window exceeded",
        BreachKind.InsufficientBreak => "break too short",
        _ => "rest between days too short"
    };
}
