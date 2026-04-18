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
                        if (!string.IsNullOrWhiteSpace(offer.Notes))
                            c.Item().PaddingTop(4).Text(offer.Notes).FontColor(Colors.Grey.Darken1);
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

                        // Header row
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Description").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit Price").SemiBold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Line Total").SemiBold();
                        });

                        // Item rows
                        foreach (var (item, index) in offer.Items.Select((i, idx) => (i, idx)))
                        {
                            var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                            IContainer Cell(IContainer c) => c.Background(bg).Padding(6);

                            table.Cell().Element(Cell).Text(item.Description);
                            table.Cell().Element(Cell).AlignRight().Text(item.Quantity.ToString("G"));
                            table.Cell().Element(Cell).AlignRight().Text(FormatMoney(item.UnitPrice, item.Currency));
                            table.Cell().Element(Cell).AlignRight().Text(FormatMoney(item.LineTotal, item.Currency));
                        }
                    });

                    // Total
                    col.Item().PaddingTop(8).AlignRight().Row(row =>
                    {
                        row.AutoItem().PaddingRight(16).Text("Total").SemiBold().FontSize(12);
                        row.AutoItem().Text(FormatMoney(offer.Subtotal, offer.Currency)).Bold().FontSize(14);
                    });

                    if (!string.IsNullOrWhiteSpace(offer.Notes))
                    {
                        col.Item().PaddingTop(24).Column(c =>
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

    private static string FormatMoney(decimal amount, string currency) =>
        amount.ToString("N2") + " " + currency;
}
