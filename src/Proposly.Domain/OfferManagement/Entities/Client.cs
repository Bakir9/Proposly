using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.OfferManagement.Entities;

public sealed class Client : Entity<Guid>, ITenantEntity, IAuditableEntity
{
    private Client() { } // For EF Core

    private Client(Guid id, Guid companyId, string name, string? contactPerson, string? email, string? phone, Address? address)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Address = address;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Client Create(Guid companyId, string name, string? contactPerson, string? email, string? phone, Address? address = null)
        => new(Guid.NewGuid(), companyId, name, contactPerson, email, phone, address);

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public Address? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Update(string name, string? contactPerson, string? email, string? phone, Address? address)
    {
        Name = name;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }
}
