using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proposly.Infrastructure.Services.Pdf;

public sealed class OfferPdfService : IPdfService
{
    public byte[] GenerateOfferPdf(OfferDetailResponse offer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("OFFER").FontSize(24).Bold().FontColor(Colors.Grey.Darken3);
                            c.Item().Text(offer.Title).FontSize(14).SemiBold();
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text($"Status: {offer.Status}").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().AlignRight().Text($"Date: {offer.CreatedAt:dd MMM yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                            if (offer.ValidUntil.HasValue)
                                c.Item().AlignRight().Text($"Valid until: {offer.ValidUntil:dd MMM yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Client
                    col.Item().PaddingBottom(16).Column(c =>
                    {
                        c.Item().Text("Bill To").FontSize(9).FontColor(Colors.Grey.Medium).Bold();
                        c.Item().Text(offer.ClientName).SemiBold();
                        if (!string.IsNullOrWhiteSpace(offer.ClientContactPerson))
                            c.Item().Text(offer.ClientContactPerson).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(offer.ClientStreet))
                            c.Item().Text(offer.ClientStreet).FontColor(Colors.Grey.Darken1);
                        var cityLine = string.Join(" ", new[] { offer.ClientPostalCode, offer.ClientCity }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (!string.IsNullOrWhiteSpace(cityLine))
                            c.Item().Text(cityLine).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(offer.ClientCountry))
                            c.Item().Text(offer.ClientCountry).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(offer.ClientVatNumber))
                            c.Item().PaddingTop(4).Text($"VAT: {offer.ClientVatNumber}").FontSize(9).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(offer.ClientEmail))
                            c.Item().Text(offer.ClientEmail).FontSize(9).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(offer.ClientPhone))
                            c.Item().Text(offer.ClientPhone).FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Description").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit Price").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Line Total").SemiBold();
                        });

                        foreach (var (item, index) in offer.Items.Select((i, idx) => (i, idx)))
                        {
                            var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                            IContainer Cell(IContainer c) => c.Background(bg).Padding(6);

                            table.Cell().Element(Cell).Text(item.Description);
                            table.Cell().Element(Cell).AlignRight().Text(item.Quantity.ToString("G"));
                            table.Cell().Element(Cell).AlignRight().Text(Fmt(item.UnitPrice, item.Currency));
                            table.Cell().Element(Cell).AlignRight().Text(Fmt(item.LineTotal, item.Currency));
                        }
                    });

                    // Totals breakdown
                    col.Item().PaddingTop(8).AlignRight().Column(totals =>
                    {
                        static IContainer TotalsRow(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(3);

                        void Row(string label, string value, bool bold = false, string? color = null)
                        {
                            totals.Item().Element(TotalsRow).Row(r =>
                            {
                                var labelText = r.RelativeItem().PaddingRight(24).AlignRight().Text(label);
                                var valueText = r.ConstantItem(130).AlignRight().Text(value);
                                if (bold) { labelText.Bold(); valueText.Bold(); }
                                if (color is not null)
                                {
                                    labelText.FontColor(color);
                                    valueText.FontColor(color);
                                }
                            });
                        }

                        Row("Subtotal:", Fmt(offer.Subtotal, offer.Currency));

                        if (offer.DiscountPercent is > 0)
                        {
                            Row($"Discount ({offer.DiscountPercent:F1}%):", $"−{Fmt(offer.DiscountAmount, offer.Currency)}", color: Colors.Green.Darken2);
                            Row("VAT base:", Fmt(offer.VatBase, offer.Currency));
                        }

                        if (!offer.IsVatExempt && offer.VatRate > 0)
                            Row($"{offer.VatLabel}:", Fmt(offer.VatAmount, offer.Currency));
                        else
                            Row(offer.VatLabel + ":", Fmt(0, offer.Currency), color: Colors.Grey.Medium);

                        totals.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().PaddingRight(24).AlignRight().Text("TOTAL:").Bold().FontSize(12);
                            r.ConstantItem(130).AlignRight().Text(Fmt(offer.Total, offer.Currency)).Bold().FontSize(14);
                        });
                    });

                    // VAT legal notes
                    var hasNote = !string.IsNullOrWhiteSpace(offer.VatNote);
                    if (hasNote)
                    {
                        col.Item().PaddingTop(20).Column(c =>
                        {
                            c.Item().Text("* " + offer.VatNote)
                                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        });
                    }

                    // Offer notes
                    if (!string.IsNullOrWhiteSpace(offer.Notes))
                    {
                        col.Item().PaddingTop(hasNote ? 8 : 24).Column(c =>
                        {
                            c.Item().Text("Notes").FontSize(9).FontColor(Colors.Grey.Medium).Bold();
                            c.Item().PaddingTop(4).Text(offer.Notes).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static string Fmt(decimal amount, string currency) =>
        amount.ToString("N2") + " " + currency;
}
