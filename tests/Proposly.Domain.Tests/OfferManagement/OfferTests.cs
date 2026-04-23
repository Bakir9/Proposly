using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.OfferManagement;

public class OfferTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();

    private static Offer CreateDraftOffer() =>
        Offer.Create(CompanyId, ClientId, "Test Offer", null, "EUR", null);

    private static Offer CreateSentOffer()
    {
        var offer = CreateDraftOffer();
        offer.AddItem("Widget", 2, new Money(100m, "EUR"));
        offer.Send();
        return offer;
    }

    // --- Send ---

    [Fact]
    public void Send_WithItems_TransitionsToSent()
    {
        var offer = CreateDraftOffer();
        offer.AddItem("Widget", 1, new Money(50m, "EUR"));

        offer.Send();

        Assert.Equal(OfferStatus.Sent, offer.Status);
        Assert.NotNull(offer.SentAt);
    }

    [Fact]
    public void Send_WithNoItems_Throws()
    {
        var offer = CreateDraftOffer();

        Assert.Throws<InvalidOperationException>(() => offer.Send());
    }

    [Fact]
    public void Send_WhenNotDraft_Throws()
    {
        var offer = CreateSentOffer();

        Assert.Throws<InvalidOperationException>(() => offer.Send());
    }

    // --- Accept ---

    [Fact]
    public void Accept_WhenSent_TransitionsToAccepted()
    {
        var offer = CreateSentOffer();

        offer.Accept();

        Assert.Equal(OfferStatus.Accepted, offer.Status);
    }

    [Fact]
    public void Accept_WhenNotSent_Throws()
    {
        var offer = CreateDraftOffer();

        Assert.Throws<InvalidOperationException>(() => offer.Accept());
    }

    // --- Reject ---

    [Fact]
    public void Reject_WhenSent_TransitionsToRejected()
    {
        var offer = CreateSentOffer();

        offer.Reject();

        Assert.Equal(OfferStatus.Rejected, offer.Status);
    }

    // --- Expire ---

    [Fact]
    public void Expire_WhenSent_TransitionsToExpired()
    {
        var offer = CreateSentOffer();

        offer.Expire();

        Assert.Equal(OfferStatus.Expired, offer.Status);
    }

    [Fact]
    public void Expire_WhenNotSent_Throws()
    {
        var offer = CreateDraftOffer();

        Assert.Throws<InvalidOperationException>(() => offer.Expire());
    }

    // --- AddItem ---

    [Fact]
    public void AddItem_WhenDraft_AddsItem()
    {
        var offer = CreateDraftOffer();

        offer.AddItem("Service", 3, new Money(200m, "EUR"));

        Assert.Single(offer.Items);
    }

    [Fact]
    public void AddItem_WhenSent_Throws()
    {
        var offer = CreateSentOffer();

        Assert.Throws<InvalidOperationException>(() =>
            offer.AddItem("Extra", 1, new Money(10m, "EUR")));
    }

    // --- RemoveItem ---

    [Fact]
    public void RemoveItem_WhenDraft_RemovesItem()
    {
        var offer = CreateDraftOffer();
        var item = offer.AddItem("Widget", 1, new Money(50m, "EUR"));

        offer.RemoveItem(item.Id);

        Assert.Empty(offer.Items);
    }

    // --- CalculateSubtotal ---

    [Fact]
    public void CalculateSubtotal_SumsLineItems()
    {
        var offer = CreateDraftOffer();
        offer.AddItem("Item A", 2, new Money(100m, "EUR")); // 200
        offer.AddItem("Item B", 3, new Money(50m, "EUR"));  // 150

        var subtotal = offer.CalculateSubtotal();

        Assert.Equal(350m, subtotal.Amount);
        Assert.Equal("EUR", subtotal.Currency);
    }
}
