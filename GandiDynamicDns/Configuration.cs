// ReSharper disable PropertyCanBeMadeInitOnly.Global - set by config
// ReSharper disable MemberCanBePrivate.Global - set by config

using GandiDynamicDns.Net.Dns;

namespace GandiDynamicDns;

public record Configuration {

    public required string gandiApiKey { get; init; }
    public required string domain { get; init; }
    public string subdomain { get; set; } = string.Empty;
    public TimeSpan updateInterval { get; init; } = GandiDnsManager.MINIMUM_TIME_TO_LIVE;
    public TimeSpan dnsRecordTimeToLive { get; set; } = GandiDnsManager.MINIMUM_TIME_TO_LIVE;

    public void fix() {
        subdomain = subdomain.TrimEnd('.');
        if (subdomain.Length == 0) {
            subdomain = "@";
        }
    }

}