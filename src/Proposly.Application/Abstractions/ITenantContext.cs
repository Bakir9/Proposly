namespace Proposly.Application.Abstractions;

public interface ITenantContext
{
    Guid CompanyId { get; }
    string CompanyName { get; }
}
