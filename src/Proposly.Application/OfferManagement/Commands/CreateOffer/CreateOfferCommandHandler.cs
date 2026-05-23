using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.OfferManagement.Services;

namespace Proposly.Application.OfferManagement.Commands.CreateOffer;

public sealed class CreateOfferCommandHandler : ICommandHandler<CreateOfferCommand, Guid>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateOfferCommandHandler(
        IOfferRepository offerRepository,
        IClientRepository clientRepository,
        ICompanyRepository companyRepository,
        ICurrentUserService currentUser)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateOfferCommand command, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(command.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

        var company = await _companyRepository.GetByIdAsync(_currentUser.CompanyId, ct)
            ?? throw new InvalidOperationException("Company not found.");

        var offer = Offer.Create(
            _currentUser.CompanyId,
            client.Id,
            command.Title,
            command.Notes,
            command.Currency,
            command.ValidUntil,
            command.DiscountPercent);

        var vatCalc = new VatCalculator(
            company.CompanyCountry,
            company.IsVatRegistered,
            company.IsVatExempt,
            company.DefaultVatRate,
            company.VatExemptReason);

        var vatResult = command.VatRateOverride.HasValue
            ? new VatCalculationResult
            {
                Rate = command.VatRateOverride.Value,
                Type = Domain.OfferManagement.Enums.VatType.Domestic,
                Label = $"VAT {command.VatRateOverride.Value}%"
            }
            : vatCalc.Calculate(client);

        offer.SetVat(vatResult);

        await _offerRepository.AddAsync(offer, ct);
        return offer.Id;
    }
}
