namespace Proposly.Application.Abstractions;

public interface ICurrentUserService
{
    Guid CompanyId { get; }
    Guid UserId { get; }
}
