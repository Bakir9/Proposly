using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace Proposly.Infrastructure.Services.Pdf;

public sealed class OfferPdfService : IPdfService
{
    private static readonly byte[]? LogoBytes = LoadLogo();

    private static byte[]? LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null) return null;
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public byte[] GenerateOfferPdf(OfferDetailResponse offer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(48);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Grey.Darken3));

                page.Content().Column(col =>
                {
                    // ── Header ─────────────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Left: logo + company name
                        row.RelativeItem().Row(r =>
                        {
                            if (LogoBytes is not null)
                            {
                                r.ConstantItem(36).Height(36).Image(LogoBytes).FitArea();
                                r.ConstantItem(10);
                            }
                            r.RelativeItem().AlignMiddle()
                                .Text(offer.CompanyName).FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
                        });

                        // Right: company contact info
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(offer.CompanyStreet))
                                c.Item().AlignRight().Text(offer.CompanyStreet).FontSize(9).FontColor(Colors.Grey.Medium);
                            var cityLine = string.Join(" ", new[] { offer.CompanyPostalCode, offer.CompanyCity }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (!string.IsNullOrWhiteSpace(cityLine))
                                c.Item().AlignRight().Text(cityLine).FontSize(9).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(offer.CompanyEmail))
                                c.Item().AlignRight().Text(offer.CompanyEmail).FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── Offer title & meta ──────────────────────────────────────────
                    col.Item().PaddingBottom(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("OFFER").FontSize(9).FontColor(Colors.Grey.Medium)
                                .Bold().LetterSpacing(0.08f);
                            c.Item().PaddingTop(4).Text(offer.Title ?? string.Empty)
                                .FontSize(22).Bold().FontColor(Colors.Grey.Darken3);
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight()
                                .Text($"Issued · {offer.CreatedAt:dd MMM yyyy}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            if (offer.ValidUntil.HasValue)
                                c.Item().AlignRight()
                                    .Text($"Valid until · {offer.ValidUntil:dd MMM yyyy}")
                                    .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // ── Bill To ─────────────────────────────────────────────────────
                    col.Item().PaddingBottom(20).Column(c =>
                    {
                        c.Item().PaddingBottom(6).Text("BILL TO").FontSize(8).Bold()
                            .FontColor(Colors.Grey.Medium).LetterSpacing(0.08f);
                        c.Item().Text(offer.ClientName).Bold().FontSize(11);
                        if (!string.IsNullOrWhiteSpace(offer.ClientContactPerson))
                            c.Item().Text(offer.ClientContactPerson).FontSize(9).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(offer.ClientStreet))
                            c.Item().Text(offer.ClientStreet).FontSize(9).FontColor(Colors.Grey.Medium);
                        var clientCity = string.Join(" ", new[] { offer.ClientPostalCode, offer.ClientCity }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (!string.IsNullOrWhiteSpace(clientCity))
                            c.Item().Text(clientCity).FontSize(9).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(offer.ClientCountry))
                            c.Item().Text(offer.ClientCountry).FontSize(9).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(offer.ClientVatNumber))
                            c.Item().PaddingTop(3).Text($"VAT: {offer.ClientVatNumber}")
                                .FontSize(8).FontColor(Colors.Grey.Lighten1);
                    });

                    // ── Items table ─────────────────────────────────────────────────
                    col.Item().PaddingBottom(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(6);
                            cols.ConstantColumn(48);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(80);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.BorderBottom(2).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell)
                                .Text("DESCRIPTION").FontSize(8).Bold()
                                .FontColor(Colors.Grey.Medium).LetterSpacing(0.06f);
                            h.Cell().Element(HeaderCell).AlignRight()
                                .Text("QTY").FontSize(8).Bold()
                                .FontColor(Colors.Grey.Medium).LetterSpacing(0.06f);
                            h.Cell().Element(HeaderCell).AlignRight()
                                .Text("UNIT").FontSize(8).Bold()
                                .FontColor(Colors.Grey.Medium).LetterSpacing(0.06f);
                            h.Cell().Element(HeaderCell).AlignRight()
                                .Text("TOTAL").FontSize(8).Bold()
                                .FontColor(Colors.Grey.Medium).LetterSpacing(0.06f);
                        });

                        foreach (var item in offer.Items)
                        {
                            static IContainer DataCell(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(10);

                            table.Cell().Element(DataCell).Text(item.Description);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(item.Quantity.ToString("G")).FontColor(Colors.Grey.Darken1);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(Fmt(item.UnitPrice, item.Currency)).FontColor(Colors.Grey.Darken1);
                            table.Cell().Element(DataCell).AlignRight()
                                .Text(Fmt(item.LineTotal, item.Currency)).Bold();
                        }
                    });

                    // ── Totals ──────────────────────────────────────────────────────
                    col.Item().PaddingTop(16).AlignRight().Column(totals =>
                    {
                        void SummaryRow(string label, string value, bool muted = true)
                        {
                            totals.Item().PaddingVertical(3).Row(r =>
                            {
                                r.ConstantItem(130).AlignRight().PaddingRight(24)
                                    .Text(label).FontSize(9)
                                    .FontColor(muted ? Colors.Grey.Medium : Colors.Grey.Darken3);
                                r.ConstantItem(100).AlignRight()
                                    .Text(value).FontSize(9)
                                    .FontColor(muted ? Colors.Grey.Medium : Colors.Grey.Darken3);
                            });
                        }

                        SummaryRow("Subtotal", Fmt(offer.Subtotal, offer.Currency));

                        if (offer.DiscountPercent is > 0)
                        {
                            SummaryRow($"Discount ({offer.DiscountPercent:F1}%)",
                                $"−{Fmt(offer.DiscountAmount, offer.Currency)}");
                        }

                        SummaryRow(offer.VatLabel, Fmt(offer.VatAmount, offer.Currency));

                        // Total divider
                        totals.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        totals.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(130).AlignRight().PaddingRight(24)
                                .Text("Total").Bold().FontSize(11);
                            r.ConstantItem(100).AlignRight()
                                .Text(Fmt(offer.Total, offer.Currency)).Bold().FontSize(11);
                        });
                    });

                    // ── VAT legal note ──────────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(offer.VatNote))
                    {
                        col.Item().PaddingTop(24).Column(c =>
                        {
                            c.Item().Text($"* {offer.VatNote}")
                                .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
                        });
                    }

                    // ── Offer notes ─────────────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(offer.Notes))
                    {
                        col.Item().PaddingTop(20).Column(c =>
                        {
                            c.Item().PaddingBottom(4).Text("Notes")
                                .FontSize(8).FontColor(Colors.Grey.Medium).Bold();
                            c.Item().Text(offer.Notes).FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });

                page.Footer().PaddingTop(12).BorderTop(1).BorderColor(Colors.Grey.Lighten3)
                    .AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }

    private static string Fmt(decimal amount, string currency)
    {
        var symbol = currency switch
        {
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            "CHF" => "CHF ",
            _ => currency + " "
        };
        return $"{symbol}{amount:N2}";
    }
}
