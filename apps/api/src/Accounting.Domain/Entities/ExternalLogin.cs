using Accounting.Domain.Common;
using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class ExternalLogin : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public AuthProvider Provider { get; set; }

    /// <summary>
    /// Unique identifier from the provider.
    /// Email provider: the user's email address.
    /// Google provider: the Google 'sub' (subject) claim.
    /// </summary>
    public string ProviderUserId { get; set; } = default!;

    /// <summary>
    /// Only populated for the Email provider. Null for OAuth providers.
    /// </summary>
    public string? PasswordHash { get; set; }

    // -------------------------------------------------------------------------
    // TODO: Notifications integration point
    // When a new ExternalLogin is created for an existing user (e.g. linking
    // Google to an existing email account), trigger a security notification:
    // await _notificationService.SendNewLoginMethodLinkedAsync(user);
    // -------------------------------------------------------------------------
}
