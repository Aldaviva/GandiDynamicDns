// ReSharper disable PropertyCanBeMadeInitOnly.Global - set by config
// ReSharper disable MemberCanBePrivate.Global - set by config

using GandiDynamicDns.Net.Dns;

namespace GandiDynamicDns;

public record Configuration {

    private const string DOMAIN_ROOT = "@";

    public required string gandiApiKey { get; init; }
    public required string domain { get; init; }
    public TimeSpan updateInterval { get; init; } = GandiDnsManager.MINIMUM_TIME_TO_LIVE;
    public TimeSpan dnsRecordTimeToLive { get; init; } = GandiDnsManager.MINIMUM_TIME_TO_LIVE;
    public bool dryRun { get; init; }
    public IList<string> stunServerBlacklist { get; } = [];
    public uint unanimity { get; init; } = 1;

    private readonly string _subdomain = DOMAIN_ROOT;
    public string subdomain {
        get => _subdomain;
        init => _subdomain = value.TrimEnd('.') is { Length: not 0 } s ? s : DOMAIN_ROOT;
    }

    public string fqdn => subdomain == DOMAIN_ROOT ? domain : $"{subdomain}.{domain}";

}