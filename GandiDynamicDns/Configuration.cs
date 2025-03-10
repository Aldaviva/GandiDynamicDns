// ReSharper disable PropertyCanBeMadeInitOnly.Global - set by config
// ReSharper disable MemberCanBePrivate.Global - set by config

using Gandi.Dns;
using System.Text.RegularExpressions;

namespace GandiDynamicDns;

public partial record Configuration {

    private const string DOMAIN_ROOT = "@";

    public string? gandiAuthToken { get; init; }

    [Obsolete($"Use {nameof(gandiAuthToken)} instead")]
    public string? gandiApiKey {
        get => gandiAuthToken;
        init => gandiAuthToken = isAuthTokenValid(value) && !isAuthTokenValid(gandiAuthToken) ? value : gandiAuthToken;
    }

    public required string domain { get; init; }
    public TimeSpan updateInterval { get; init; } = DnsRecord.MinTimeToLive;
    public TimeSpan dnsRecordTimeToLive { get; init; } = DnsRecord.MinTimeToLive;
    public bool dryRun { get; init; }

    public IList<string> stunServerBlacklist { get; } = [
        "stun.bergophor.de",
        "stun.usfamily.net",
        "stun.finsterwalder.com"
    ];

    public uint unanimity { get; init; } = 1;

    private readonly string _subdomain = DOMAIN_ROOT;
    public string subdomain {
        get => _subdomain;
        init => _subdomain = value.TrimEnd('.') is { Length: not 0 } s ? s : DOMAIN_ROOT;
    }

    public string fqdn => subdomain == DOMAIN_ROOT ? domain : $"{subdomain}.{domain}";

    public static bool isAuthTokenValid(string? authToken) => authToken != null && validAuthTokenPattern().IsMatch(authToken);

    [GeneratedRegex("^[A-Za-z0-9]{24}|[a-f0-9]{40}$")]
    private static partial Regex validAuthTokenPattern();

}