namespace GandiDynamicDns;

public record Configuration {

    public required string gandiApiKey { get; set; }
    public required string domain { get; set; }
    public required string subdomain { get; set; }
    public TimeSpan updateInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan? dnsRecordTimeToLive { get; set; } = TimeSpan.FromSeconds(300);

}