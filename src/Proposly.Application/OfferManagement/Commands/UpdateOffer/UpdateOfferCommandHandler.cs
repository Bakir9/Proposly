using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.OfferManagement.Services;

namespace Proposly.Application.OfferManagement.Commands.UpdateOffer;

public sealed class UpdateOfferCommandHandler : ICommandHandler<UpdateOfferCommand>
{
    private readonly IOfferRepository _repository;
    private readonly IClientRepository _clientRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdateOfferCommandHandler(
        IOfferRepository repository,
        IClientRepository clientRepository,
        ICompanyRepository companyRepository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _clientRepository = clientRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(UpdateOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.UpdateDetails(command.Title, command.Notes, command.ValidUntil, command.DiscountPercent);

        if (command.VatRateOverride.HasValue)
        {
            offer.SetVat(new VatCalculationResult
            {
                Rate = command.VatRateOverride.Value,
                Type = VatType.Domestic,
                Label = command.VatRateOverride.Value == 0 ? "VAT 0%" : $"VAT {command.VatRateOverride.Value}%",
                IsExempt = command.VatRateOverride.Value == 0
            });
        }
        else
        {
            var company = await _companyRepository.GetByIdAsync(_currentUser.CompanyId, ct);
            var client = await _clientRepository.GetByIdAsync(offer.ClientId, ct);
            if (company is not null && client is not null)
            {
                var calc = new VatCalculator(company.CompanyCountry, company.IsVatRegistered,
                    company.IsVatExempt, company.DefaultVatRate, company.VatExemptReason);
                offer.SetVat(calc.Calculate(client));
            }
        }

        await _repository.UpdateAsync(offer, ct);
    }
}
