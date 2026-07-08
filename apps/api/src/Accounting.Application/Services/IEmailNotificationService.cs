namespace Accounting.Application.Services;

public class EmailServiceSettings
{
    public string BaseUrl { get; init; } = "";
    public string Secret  { get; init; } = "";
    public string AppUrl  { get; init; } = "http://localhost:3000";
}

public interface IEmailNotificationService
{
    Task SendWelcomeAsync(string to, string firstName, string orgName, CancellationToken ct = default);
    Task SendInviteAsync(string to, string firstName, string orgName, string inviterName, string role, string rawToken, CancellationToken ct = default);
    Task SendVerificationEmailAsync(string to, string firstName, string verificationUrl, CancellationToken ct = default);
    Task SendPasswordResetAsync(string to, string firstName, string resetUrl, CancellationToken ct = default);
    Task SendPasswordChangedAsync(string to, string firstName, CancellationToken ct = default);
    Task SendInvitationAcceptedAsync(string to, string firstName, string orgName, string role, CancellationToken ct = default);
}
