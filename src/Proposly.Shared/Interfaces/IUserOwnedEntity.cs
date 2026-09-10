namespace Proposly.Shared.Interfaces;

/// <summary>
/// Marks an entity whose rows belong to a single employee rather than to the whole company.
/// <para>
/// <see cref="ITenantEntity"/> separates companies; this separates people inside a company.
/// AppDbContext applies an additional query filter to entities implementing this interface, so a
/// member sees only their own rows while an approver (Owner, Admin, SuperAdmin) sees all of them.
/// The company filter is never dropped — do not use IgnoreQueryFilters() to reach another
/// employee's rows, because that removes tenant isolation as well.
/// </para>
/// </summary>
public interface IUserOwnedEntity
{
    Guid UserId { get; }
}
