// ReSharper disable PropertyCanBeMadeInitOnly.Global - set by config
// ReSharper disable MemberCanBePrivate.Global - set by config

using Gandi.Dns;
using System.Diagnostics.CodeAnalysis;
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

    public IList<string> subdomains { get; } = [];

    [Obsolete($"Use {nameof(subdomains)} instead")]
    public string? subdomain {
        get => null;
        set {
            if (value is not null) {
                subdomains.Add(value);
            }
        }
    }

    public static bool isAuthTokenValid([NotNullWhen(true)] string? authToken) => authToken != null && validAuthTokenPattern().IsMatch(authToken);

    public void sanitize() {
        for (int i = 0; i < subdomains.Count; i++) {
            subdomains[i] = subdomains[i].TrimEnd('.') is { Length: not 0 } s ? s : DOMAIN_ROOT;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9]{24}|[a-f0-9]{40}$")]
    private static partial Regex validAuthTokenPattern();

}