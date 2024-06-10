namespace GandiDynamicDns;

public record Configuration {

    public static readonly TimeSpan MINIMUM_TIME_TO_LIVE = TimeSpan.FromSeconds(300);

    public required string gandiApiKey { get; set; }
    public required string domain { get; set; }
    public required string subdomain { get; set; }
    public TimeSpan updateInterval { get; set; } = MINIMUM_TIME_TO_LIVE;
    public TimeSpan dnsRecordTimeToLive { get; set; } = MINIMUM_TIME_TO_LIVE;

}