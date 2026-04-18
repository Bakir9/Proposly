using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Queries.GetOfferById;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.SendOfferEmail;

public sealed class SendOfferEmailCommandHandler : ICommandHandler<SendOfferEmailCommand>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;

    public SendOfferEmailCommandHandler(
        IOfferRepository offerRepository,
        IClientRepository clientRepository,
        IPdfService pdfService,
        IEmailService emailService)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    public async Task HandleAsync(SendOfferEmailCommand command, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        var client = await _clientRepository.GetByIdAsync(offer.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {offer.ClientId} not found.");

        if (string.IsNullOrWhiteSpace(client.Email))
            throw new InvalidOperationException("Client has no email address on file.");

        var offerDetail = new OfferDetailResponse(
            offer.Id, offer.ClientId, client.Name,
            offer.Title, offer.Notes, offer.Status,
            offer.CalculateSubtotal().Amount, offer.Currency,
            offer.ValidUntil, offer.SentAt, offer.CreatedAt,
            offer.Items.Select(i => new OfferItemResponse(
                i.Id, i.Description, i.Quantity,
                i.UnitPrice.Amount, i.LineTotal.Amount, i.UnitPrice.Currency)).ToList());

        var pdfBytes = _pdfService.GenerateOfferPdf(offerDetail);

        var fileName = $"Offer-{offer.Title.Replace(" ", "_")}.pdf";
        var subject = $"Offer: {offer.Title}";
        var body = $"""
            <p>Dear {client.ContactPerson ?? client.Name},</p>
            <p>Please find attached our offer <strong>{offer.Title}</strong>.</p>
            <p>Total: <strong>{offer.CalculateSubtotal().Amount:N2} {offer.Currency}</strong></p>
            {(offer.ValidUntil.HasValue ? $"<p>This offer is valid until <strong>{offer.ValidUntil:dd MMM yyyy}</strong>.</p>" : "")}
            {(!string.IsNullOrWhiteSpace(offer.Notes) ? $"<p>{offer.Notes}</p>" : "")}
            <p>Please don't hesitate to reach out if you have any questions.</p>
            <p>Best regards,<br/>Proposly</p>
            """;

        await _emailService.SendAsync(client.Email, subject, body, pdfBytes, fileName, ct);
    }
}
