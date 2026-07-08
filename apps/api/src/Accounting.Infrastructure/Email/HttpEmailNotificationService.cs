using System.Net.Http.Json;
using Accounting.Application.Services;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Email;

public class HttpEmailNotificationService : IEmailNotificationService
{
    private readonly HttpClient            _http;
    private readonly EmailServiceSettings  _settings;
    private readonly ILogger<HttpEmailNotificationService> _logger;

    public HttpEmailNotificationService(
        HttpClient http,
        EmailServiceSettings settings,
        ILogger<HttpEmailNotificationService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    public Task SendWelcomeAsync(string to, string firstName, string orgName, CancellationToken ct = default) =>
        PostAsync("welcome", to, new { firstName, orgName }, ct);

    public Task SendInviteAsync(string to, string firstName, string orgName, string inviterName, string role, string rawToken, CancellationToken ct = default) =>
        PostAsync("invite", to, new {
            firstName, orgName, inviterName, role,
            acceptUrl  = $"{_settings.AppUrl}/invite?token={Uri.EscapeDataString(rawToken)}",
            declineUrl = $"{_settings.AppUrl}/invite/decline?token={Uri.EscapeDataString(rawToken)}",
        }, ct);

    public Task SendVerificationEmailAsync(string to, string firstName, string verificationUrl, CancellationToken ct = default) =>
        PostAsync("verify-email", to, new { firstName, verificationUrl }, ct);

    public Task SendPasswordResetAsync(string to, string firstName, string resetUrl, CancellationToken ct = default) =>
        PostAsync("reset-password", to, new { firstName, resetUrl }, ct);

    public Task SendPasswordChangedAsync(string to, string firstName, CancellationToken ct = default) =>
        PostAsync("password-changed", to, new { firstName }, ct);

    public Task SendInvitationAcceptedAsync(string to, string firstName, string orgName, string role, CancellationToken ct = default) =>
        PostAsync("invitation-accepted", to, new { firstName, orgName, role, appUrl = _settings.AppUrl }, ct);

    private async Task PostAsync(string type, string to, object payload, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _logger.LogDebug("Email service not configured — skipping {Type} to {To}", type, to);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/send")
            {
                Content = JsonContent.Create(new { type, to, payload })
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.Secret);

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Email service returned {Status} for {Type} to {To}",
                    (int)response.StatusCode, type, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Type} email to {To} — email service unreachable", type, to);
        }
    }
}
