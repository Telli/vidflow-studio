using System.Text;
using System.Text.Json;
using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace VidFlow.Api.Features.Notifications;

public sealed class WebhookOptions
{
    /// <summary>
    /// If set, only these hosts are allowed as webhook destinations.
    /// Use exact hostnames (no scheme/path), e.g. "hooks.slack.com".
    /// </summary>
    public string[] AllowedHosts { get; init; } = [];

    /// <summary>
    /// Whether to allow localhost destinations (useful for local testing only).
    /// </summary>
    public bool AllowLocalhost { get; init; } = false;

    /// <summary>
    /// Whether to allow private/loopback/link-local IP ranges (NOT recommended).
    /// </summary>
    public bool AllowPrivateIpRanges { get; init; } = false;

    /// <summary>
    /// Require HTTPS for webhook destinations.
    /// </summary>
    public bool RequireHttps { get; init; } = true;
}

public record WebhookSendResult(
    bool Success,
    HttpStatusCode? StatusCode = null,
    string? ErrorMessage = null);

public interface IWebhookService
{
    Task<WebhookSendResult> SendWebhookAsync(string url, object payload, CancellationToken ct);
    Task<WebhookSendResult> SendWebhookAsync(Uri url, object payload, CancellationToken ct);
}

public class WebhookService : IWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;
    private readonly WebhookOptions _options;

    public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger, IOptions<WebhookOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public Task<WebhookSendResult> SendWebhookAsync(string url, object payload, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(new WebhookSendResult(false, null, "Invalid webhook URL."));
        }

        return SendWebhookAsync(uri, payload, ct);
    }

    public async Task<WebhookSendResult> SendWebhookAsync(Uri url, object payload, CancellationToken ct)
    {
        try
        {
            var validationError = await ValidateDestinationAsync(url, ct);
            if (validationError != null)
            {
                _logger.LogWarning("Webhook destination rejected: {Reason} ({Url})", validationError, url);
                return new WebhookSendResult(false, null, validationError);
            }

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook to {Url} failed with status {StatusCode}", 
                    url, response.StatusCode);
                return new WebhookSendResult(false, response.StatusCode, $"Webhook failed with status {(int)response.StatusCode}.");
            }
            else
            {
                _logger.LogInformation("Webhook sent to {Url}", url);
                return new WebhookSendResult(true, response.StatusCode, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", url);
            return new WebhookSendResult(false, null, "Failed to send webhook.");
        }
    }

    private async Task<string?> ValidateDestinationAsync(Uri url, CancellationToken ct)
    {
        if (!url.IsAbsoluteUri)
            return "Webhook URL must be absolute.";

        if (_options.RequireHttps && !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return "Webhook URL must use HTTPS.";

        var host = url.Host;
        if (string.IsNullOrWhiteSpace(host))
            return "Webhook URL host is missing.";

        var isLocalhost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                          host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);

        if (isLocalhost && !_options.AllowLocalhost && !_options.AllowPrivateIpRanges)
            return "Localhost webhook destinations are not allowed.";

        if (_options.AllowedHosts.Length > 0 &&
            !_options.AllowedHosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase)))
        {
            return "Webhook host is not in allowlist.";
        }

        if (_options.AllowPrivateIpRanges)
            return null;

        // Block direct IP destinations and resolved private ranges.
        if (IPAddress.TryParse(host, out var ipLiteral))
        {
            return IsBlockedIp(ipLiteral) ? "Webhook destination IP is not allowed." : null;
        }

        IPAddress[] resolved;
        try
        {
            resolved = await Dns.GetHostAddressesAsync(host, ct);
        }
        catch
        {
            return "Unable to resolve webhook host.";
        }

        if (resolved.Length == 0)
            return "Unable to resolve webhook host.";

        return resolved.Any(IsBlockedIp) ? "Webhook destination resolves to a private/reserved IP range." : null;
    }

    private static bool IsBlockedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 127.0.0.0/8
            if (bytes[0] == 127) return true;
            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 0.0.0.0/8
            if (bytes[0] == 0) return true;
            // 100.64.0.0/10 (carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
            // 192.0.2.0/24, 198.51.100.0/24, 203.0.113.0/24 (TEST-NETs)
            if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) return true;
            if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) return true;
            if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                return true;

            var bytes = ip.GetAddressBytes();
            // fc00::/7 (unique local)
            if ((bytes[0] & 0xFE) == 0xFC) return true;
        }

        return false;
    }
}

public record WebhookPayload(
    string EventType,
    Guid ProjectId,
    Guid? EntityId,
    object Data,
    DateTime Timestamp);
