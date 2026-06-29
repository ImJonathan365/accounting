using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IExternalLoginRepository
{
    /// <summary>
    /// Finds an external login by provider and the provider's own user ID.
    /// Returns the record with the User already loaded.
    /// </summary>
    Task<ExternalLogin?> GetByProviderAsync(AuthProvider provider, string providerUserId, CancellationToken ct = default);

    Task AddAsync(ExternalLogin login, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // -------------------------------------------------------------------------
    // TODO: Google OAuth — add when implementing Google sign-in:
    // Task<ExternalLogin?> GetGoogleLoginByUserIdAsync(Guid userId, CancellationToken ct = default);
    // -------------------------------------------------------------------------
}
