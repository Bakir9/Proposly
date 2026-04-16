using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.OfferManagement.Entities;

public sealed class OfferItem : Entity<Guid>
{
    private OfferItem() { } // For EF Core

    private OfferItem(Guid id, Guid offerId, string description, decimal quantity, Money unitPrice)
        : base(id)
    {
        OfferId = offerId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OfferItem Create(Guid offerId, string description, decimal quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice.Amount < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new(Guid.NewGuid(), offerId, description, quantity, unitPrice);
    }

    public Guid OfferId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }

    /// <summary>Snapshot of the unit price at the time the item was added.</summary>
    public Money UnitPrice { get; private set; } = null!;

    public Money LineTotal => UnitPrice * Quantity;

    public void Update(string description, decimal quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice.Amount < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
