using System.Security.Cryptography;
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

    public static User CreateSuperAdmin(string email, string passwordHash, string firstName, string lastName)
    {
        var user = new User(Guid.NewGuid(), Guid.Empty, email, passwordHash, firstName, lastName, UserRole.SuperAdmin);
        user.IsEmailVerified = true;
        return user;
    }

    /// <summary>
    /// A worker who exists for planning (projects, HR, time) but cannot log in: no usable
    /// password, a generated placeholder email, and <see cref="IsProfileOnly"/> set. Login access
    /// is granted later via <see cref="GrantLoginAccess"/>.
    /// </summary>
    public static User CreateProfileOnly(Guid companyId, string firstName, string lastName)
    {
        var placeholderEmail = $"worker-{Guid.NewGuid():N}@profile.invalid";
        var placeholderHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var user = new User(Guid.NewGuid(), companyId, placeholderEmail, placeholderHash, firstName, lastName, UserRole.Member);
        user.IsProfileOnly = true;
        return user;
    }

    public static User CreateInvited(Guid companyId, string email, string firstName, string lastName, UserRole role, string inviteToken, DateTime inviteTokenExpiry)
    {
        // Placeholder hash — cannot be used to log in; replaced when invite is accepted
        var placeholder = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var user = new User(Guid.NewGuid(), companyId, email, placeholder, firstName, lastName, role);
        user.InviteToken = inviteToken;
        user.InviteTokenExpiry = inviteTokenExpiry;
        user.IsEmailVerified = true; // admin-created account; email ownership is assumed
        return user;
    }

    public Guid CompanyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsDisabled { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }
    public string? InviteToken { get; private set; }
    public DateTime? InviteTokenExpiry { get; private set; }
    public bool IsPendingInvite => InviteToken is not null;

    /// <summary>True for workers created without login access — see <see cref="CreateProfileOnly"/>.</summary>
    public bool IsProfileOnly { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public void SetEmailVerificationToken(string token, DateTime expiry)
    {
        EmailVerificationToken = token;
        EmailVerificationTokenExpiry = expiry;
    }

    public void MarkEmailVerified()
    {
        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Upgrades a profile-only worker to an invitable account: real email, chosen role, and an
    /// invite token that feeds the normal accept-invite flow.
    /// </summary>
    public void GrantLoginAccess(string email, UserRole role, string inviteToken, DateTime inviteTokenExpiry)
    {
        if (!IsProfileOnly)
            throw new InvalidOperationException("This worker already has login access.");
        if (role == UserRole.Owner || role == UserRole.SuperAdmin)
            throw new InvalidOperationException("A worker can only be invited as Admin or Member.");

        Email = email;
        Role = role;
        InviteToken = inviteToken;
        InviteTokenExpiry = inviteTokenExpiry;
        IsEmailVerified = true; // admin-provided address; ownership is assumed, as with CreateInvited
        IsProfileOnly = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AcceptInvite(string passwordHash)
    {
        if (InviteToken is null)
            throw new InvalidOperationException("This account is already active.");
        PasswordHash = passwordHash;
        InviteToken = null;
        InviteTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string token, DateTime expiry)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiry = expiry;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void UpdateProfile(string firstName, string lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(UserRole role) => Role = role;

    public void Disable()
    {
        if (Role == UserRole.Owner)
            throw new InvalidOperationException("Cannot disable the company Owner.");
        IsDisabled = true;
    }

    public void Enable() => IsDisabled = false;
}
