using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CompanyManagement.Entities;

public sealed class User : Entity<Guid>, ITenantEntity, IAuditableEntity
{
    private User() { } // For EF Core

    private User(Guid id, Guid companyId, string email, string passwordHash, string firstName, string lastName, UserRole role)
        : base(id)
    {
        CompanyId = companyId;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static User Create(Guid companyId, string email, string passwordHash, string firstName, string lastName, UserRole role = UserRole.Member)
        => new(Guid.NewGuid(), companyId, email, passwordHash, firstName, lastName, role);

    public Guid CompanyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateRole(UserRole role) => Role = role;
}
