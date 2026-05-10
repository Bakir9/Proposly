using Proposly.Domain.OfferManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.OfferManagement.Entities;

public sealed class Client : Entity<Guid>, ITenantEntity, IAuditableEntity
{
    private Client() { } // For EF Core

    private Client(Guid id, Guid companyId, string name, string? contactPerson, string? email, string? phone, string? website, Address? address, string? currency, string? vatNumber, ClientStatus status)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Website = website;
        Address = address;
        Currency = currency;
        VatNumber = vatNumber;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Client Create(Guid companyId, string name, string? contactPerson, string? email, string? phone, string? website = null, Address? address = null, string? currency = null, string? vatNumber = null, ClientStatus status = ClientStatus.Active)
        => new(Guid.NewGuid(), companyId, name, contactPerson, email, phone, website, address, currency, vatNumber, status);

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public Address? Address { get; private set; }
    public string? Currency { get; private set; }
    public string? VatNumber { get; private set; }
    public ClientStatus Status { get; private set; } = ClientStatus.Active;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ClientNote> _notes = new();
    public IReadOnlyList<ClientNote> Notes => _notes.AsReadOnly();

    public void Update(string name, string? contactPerson, string? email, string? phone, string? website, Address? address, string? currency, string? vatNumber, ClientStatus status)
    {
        Name = name;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Website = website;
        Address = address;
        Currency = currency;
        VatNumber = vatNumber;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public ClientNote AddNote(string content, Guid authorId, string authorName)
    {
        var note = ClientNote.Create(Id, content, authorId, authorName);
        _notes.Add(note);
        return note;
    }
}
