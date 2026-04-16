using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Events;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.OfferManagement.Entities;

public sealed class Offer : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<OfferItem> _items = [];

    private Offer() { } // For EF Core

    private Offer(
        Guid id,
        Guid companyId,
        Guid clientId,
        string title,
        string? notes,
        string currency,
        DateOnly? validUntil)
        : base(id)
    {
        CompanyId = companyId;
        ClientId = clientId;
        Title = title;
        Notes = notes;
        Currency = currency;
        ValidUntil = validUntil;
        Status = OfferStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Offer Create(
        Guid companyId,
        Guid clientId,
        string title,
        string? notes,
        string currency,
        DateOnly? validUntil)
    {
        var offer = new Offer(Guid.NewGuid(), companyId, clientId, title, notes, currency, validUntil);
        offer.RaiseDomainEvent(new OfferCreatedDomainEvent(offer.Id, companyId));
        return offer;
    }

    public Guid CompanyId { get; private set; }
    public Guid ClientId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateOnly? ValidUntil { get; private set; }
    public OfferStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<OfferItem> Items => _items.AsReadOnly();

    // --- Status transitions ---

    public void Send()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot send an offer in '{Status}' status.");
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot send an offer with no items.");

        Status = OfferStatus.Sent;
        SentAt = DateTime.UtcNow;
        Touch();
        RaiseDomainEvent(new OfferSentDomainEvent(Id, CompanyId, ClientId));
    }

    public void Accept()
    {
        if (Status != OfferStatus.Sent)
            throw new InvalidOperationException($"Cannot accept an offer in '{Status}' status.");

        Status = OfferStatus.Accepted;
        Touch();
        RaiseDomainEvent(new OfferAcceptedDomainEvent(Id, CompanyId, ClientId));
    }

    public void Reject()
    {
        if (Status != OfferStatus.Sent)
            throw new InvalidOperationException($"Cannot reject an offer in '{Status}' status.");

        Status = OfferStatus.Rejected;
        Touch();
        RaiseDomainEvent(new OfferRejectedDomainEvent(Id, CompanyId, ClientId));
    }

    public void Expire()
    {
        if (Status != OfferStatus.Sent)
            throw new InvalidOperationException($"Cannot expire an offer in '{Status}' status.");

        Status = OfferStatus.Expired;
        Touch();
    }

    // --- Item management (only while Draft) ---

    public OfferItem AddItem(string description, decimal quantity, Money unitPrice)
    {
        EnsureDraft();
        var item = OfferItem.Create(Id, description, quantity, unitPrice);
        _items.Add(item);
        Touch();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found in this offer.");
        _items.Remove(item);
        Touch();
    }

    public void UpdateItem(Guid itemId, string description, decimal quantity, Money unitPrice)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found in this offer.");
        item.Update(description, quantity, unitPrice);
        Touch();
    }

    public void UpdateDetails(string title, string? notes, DateOnly? validUntil)
    {
        EnsureDraft();
        Title = title;
        Notes = notes;
        ValidUntil = validUntil;
        Touch();
    }

    // --- Financial calculations ---

    public Money CalculateSubtotal()
    {
        var zero = Money.Zero(Currency);
        return _items.Aggregate(zero, (total, item) => total + item.LineTotal);
    }

    // --- Helpers ---

    private void EnsureDraft()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Offer cannot be modified in '{Status}' status.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
