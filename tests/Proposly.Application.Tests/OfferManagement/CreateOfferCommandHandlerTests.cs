using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Commands.CreateOffer;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.Tests.OfferManagement;

public sealed class CreateOfferCommandHandlerTests
{
    private readonly IOfferRepository _offers = Substitute.For<IOfferRepository>();
    private readonly IClientRepository _clients = Substitute.For<IClientRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreateOfferCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid();

    public CreateOfferCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _sut = new CreateOfferCommandHandler(_offers, _clients, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsNewOfferId()
    {
        var client = Client.Create(_companyId, "Acme Corp", null, null, null);
        _clients.GetByIdAsync(_clientId).Returns(client);

        var cmd = new CreateOfferCommand(_clientId, "Proposal A", null, "EUR", null);
        var id = await _sut.HandleAsync(cmd);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsOffer()
    {
        var client = Client.Create(_companyId, "Acme Corp", null, null, null);
        _clients.GetByIdAsync(_clientId).Returns(client);

        var cmd = new CreateOfferCommand(_clientId, "Proposal A", null, "EUR", null);
        await _sut.HandleAsync(cmd);

        await _offers.Received(1).AddAsync(Arg.Is<Offer>(o =>
            o.Title == "Proposal A" &&
            o.CompanyId == _companyId &&
            o.ClientId == client.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClientNotFound_Throws()
    {
        _clients.GetByIdAsync(_clientId).Returns((Client?)null);

        var cmd = new CreateOfferCommand(_clientId, "Proposal A", null, "EUR", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(cmd));
    }

    [Fact]
    public async Task HandleAsync_SetsOfferCurrencyFromCommand()
    {
        var client = Client.Create(_companyId, "Acme", null, null, null);
        _clients.GetByIdAsync(_clientId).Returns(client);

        var cmd = new CreateOfferCommand(_clientId, "Q", null, "USD", null);
        await _sut.HandleAsync(cmd);

        await _offers.Received(1).AddAsync(Arg.Is<Offer>(o => o.Currency == "USD"), Arg.Any<CancellationToken>());
    }
}
