using Proposly.Domain.CompanyManagement.Entities;

namespace Proposly.Application.Abstractions;

public interface IJwtService
{
    string GenerateToken(User user);
}
