using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public interface ITokenService
{
    string GenerateToken(User user, Guid organizationId, string role);
}
